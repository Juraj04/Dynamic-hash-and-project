using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicHash
{
    public class DynHash<T> where T : IRecord<T>, new()
    {
        private TrieNode Root { get; set; } 
        private int _currentDepth;
        private FilePaths _files;
        public int MaxRecords { get; set; }
        public int MaxRecordsInOverflow { get; set; }

        private List<int> _freeBlocks;
        private List<int> _freeBlocksOverflow;

        private Block<T> _workingBlock1;
        private Block<T> _leftWorkingBlock;
        private Block<T> _rightWorkingBlock;
        private Block<T> _overflowBlock;

        private BinaryFileManager _blockFile; 
        private BinaryFileManager _overflowFile;

        public int HashBitSize => sizeof(int) * 8;

        public DynHash(FilePaths files, int maxRecords, int maxRecordsInOverflow)
        {
            Root = new ExternalTrieNode();
            _files = files;
            MaxRecords = maxRecords;
            MaxRecordsInOverflow = maxRecordsInOverflow;
            _overflowBlock = new Block<T>(maxRecordsInOverflow);
            _workingBlock1 = new Block<T>(maxRecords);
            _freeBlocks = new List<int>();
            _freeBlocksOverflow = new List<int>();
            _blockFile = new BinaryFileManager(_files.FileBlockData, _workingBlock1.GetSize());
            _overflowFile = new BinaryFileManager(_files.FileOverflowFile,_overflowBlock.GetSize());
        }

        public void Clear()
        {
            Root = new ExternalTrieNode();
            _workingBlock1 = new Block<T>(MaxRecords);
            _overflowBlock = new Block<T>(MaxRecordsInOverflow);
            _freeBlocks.Clear();
            _freeBlocksOverflow.Clear();
            _blockFile.ResetFile();
            _overflowFile.ResetFile();
            ResetFileDataSize();
        }

        public void ResetFileDataSize()
        {
            _blockFile.DataSize = _workingBlock1.GetSize();
            _overflowFile.DataSize = _overflowBlock.GetSize();
        }

        public bool TryFind(T record, out T resultRecord)
        {
            _currentDepth = 0;
            resultRecord = default(T);
            var node = FindExternalNode(record.GetHashCode());
            if (node.IndexOfBlock == -1)
                return false;
            if (node.RecordCount == 0)
                return false;
            _workingBlock1 = new Block<T>(MaxRecords);
            _workingBlock1.FromByteArray(_blockFile.ReadData(node.IndexOfBlock));
            if (node.RecordCount <= MaxRecords)
            {
                return _workingBlock1.TryFindRecord(record, out resultRecord);
            }
            else
            {
                return _workingBlock1.TryFindRecord(record, out resultRecord) || TryFindOverflow(_workingBlock1, record, out resultRecord);
            }
            

        }

        private bool TryFindOverflow(Block<T> block, T record, out T resultRecord)
        {
            resultRecord = default(T);
            var index = block.NextOverflowBlockIndex;
            if(index == -1)
                throw new Exception($"This should not happened, index of block: {index} and should not be -1");
            while (index != -1)
            {
                _overflowBlock = new Block<T>(MaxRecordsInOverflow);
                _overflowBlock.FromByteArray(_overflowFile.ReadData(index));

                if (_overflowBlock.TryFindRecord(record, out resultRecord))
                {
                    return true;
                }
                
                index = _overflowBlock.NextOverflowBlockIndex;
            }

            return false;
        }

        

        private ExternalTrieNode FindExternalNode(int hash)
        {
            if (Root is ExternalTrieNode node)
            {
                return node;
            }
            else
            {
                var bArray = new BitArray(new int[1] { hash });
                var workingNode = Root;
                while (!(workingNode is ExternalTrieNode))
                {
                    if (bArray[_currentDepth])
                        workingNode = (workingNode as InternalTrieNode)?.RightChild;
                    else
                        workingNode = (workingNode as InternalTrieNode)?.LeftChild;

                    _currentDepth++;
                }
                return workingNode as ExternalTrieNode;
            }
        }

        public bool Add(T record)
        {
            _currentDepth = 0;
            bool result = false;
            var node = FindExternalNode(record.GetHashCode());

            if (node.IndexOfBlock == -1)
            {
                _workingBlock1 = new Block<T>(MaxRecords);
                result = _workingBlock1.AddRecord(record);
                if (result)
                {
                    node.RecordCount++;
                    node.IndexOfBlock = WriteNewBlockAndGetIndex(_workingBlock1);
                }
            }
            else if (node.RecordCount < MaxRecords)
            {
                _workingBlock1 = new Block<T>(MaxRecords);
                _workingBlock1.FromByteArray(_blockFile.ReadData(node.IndexOfBlock));

                result = _workingBlock1.AddRecord(record);
                if (result)
                {
                    node.RecordCount++;
                    _blockFile.WriteData(node.IndexOfBlock, _workingBlock1.ToByteArray());
                }

            }
            else if (node.RecordCount > MaxRecords)
            {
                return AddOverflow(node, record);
            }
            else if (node.RecordCount == MaxRecords)
            {
                if (_currentDepth == HashBitSize) //ak sme na konci
                {
                    return AddOverflow(node, record);
                }
                else
                {
                    var replaceNode = node;
                    _workingBlock1 = new Block<T>(MaxRecords);
                    _workingBlock1.FromByteArray(_blockFile.ReadData(replaceNode.IndexOfBlock));
                    _freeBlocks.Add(replaceNode.IndexOfBlock); //blok sa rozdeli, ale ulozim jeho index
                    var workingRecord = record;
                    while (replaceNode?.RecordCount == MaxRecords)
                    {
                        if (_currentDepth == HashBitSize) //ak uz dalej nemozme delit
                        {
                            return AddOverflow(replaceNode, workingRecord, _workingBlock1);
                        }

                        var newInternal = TransformExternal(replaceNode);
                        _leftWorkingBlock = new Block<T>(MaxRecords);
                        _rightWorkingBlock = new Block<T>(MaxRecords);
                        var leftExternal = newInternal.LeftChild as ExternalTrieNode;
                        var rightExternal = newInternal.RightChild as ExternalTrieNode;

                        result = PutRecordInCorrectBlock(leftExternal, rightExternal, workingRecord); //najskor pridat

                        foreach (var rec in _workingBlock1.GetAllRecords()) //najskor prerozdelit
                        {
                            workingRecord = rec;
                            PutRecordInCorrectBlock(leftExternal, rightExternal, rec); //uz raz boli pridane, netreba overovat
                        }

                        if (leftExternal?.RecordCount == 0)
                        {
                            replaceNode = rightExternal;
                            _workingBlock1 = _rightWorkingBlock;
                        }
                        else if (rightExternal?.RecordCount == 0)
                        {
                            replaceNode = leftExternal;
                            _workingBlock1 = _leftWorkingBlock;
                        }
                        else
                        {
                            if (leftExternal != null)
                                leftExternal.IndexOfBlock = WriteNewBlockAndGetIndex(_leftWorkingBlock);
                            if (rightExternal != null)
                                rightExternal.IndexOfBlock = WriteNewBlockAndGetIndex(_rightWorkingBlock);
                            break;
                        }
                        _currentDepth++;

                    }
                    AdjustFile();
                }

            }

            return result;
        }

        private bool AddOverflow(ExternalTrieNode node, T record, Block<T> workingBlock = null)
        {
            if (workingBlock == null)
            {
                if(node.IndexOfBlock == -1)
                    throw new Exception($"This should not happened, index of block: {node.IndexOfBlock} and should not be");
                if(node.RecordCount<MaxRecords)
                    throw new Exception($"This should not happened nodeRecords vs maxRecords {node.RecordCount} < {MaxRecords}");

                //nacitaj podla indexu blok,
                //skus vlozit do bloku
                //ak nie
                //kym ma blok adresu na dalsieho, nacitavaj do listu
                //postupne nacitavaj blok kym sa nenajde taky kde sa zaznam zmesti
                //ak sa nikde nezmesti, vytvor novy pozri free bloky inak na koniec
                //record count ++
                _workingBlock1 = new Block<T>(MaxRecords);
                _workingBlock1.FromByteArray(_blockFile.ReadData(node.IndexOfBlock));
                if (_workingBlock1.TryFindRecord(record, out var data)) //ak tam uz existuje
                {
                    return false;
                }
                
                if (_workingBlock1.NextOverflowBlockIndex == -1) //ak zatial nebol preplneny
                {
                    if (_workingBlock1.AddRecord(record)) //ak ho pridame do povodneho tak len zapiseme a koniec, netreba kontrolu lebo nema preplnovacie, i ked myslim ze to ani nenastane
                    {
                        node.RecordCount++;
                        _blockFile.WriteData(node.IndexOfBlock, _workingBlock1.ToByteArray());
                        return true;
                    }

                    _overflowBlock = new Block<T>(MaxRecordsInOverflow);
                    _overflowBlock.AddRecord(record);
                    node.RecordCount++;

                    if (_freeBlocksOverflow.Count == 0)
                    {
                        var index = _freeBlocksOverflow.Min(i => i);
                        _freeBlocksOverflow.Remove(index);
                         _overflowFile.WriteData(index,_overflowBlock.ToByteArray()); //zapisem preplneny a nastavim index
                        _workingBlock1.NextOverflowBlockIndex = index;
                    }
                    else
                    {
                        _workingBlock1.NextOverflowBlockIndex = _overflowFile.WriteData(_overflowBlock.ToByteArray());
                    }
                    
                }
                else //ak uz bol preplneny
                {
                    var listOverflowBlocks = new List<Block<T>>();
                    var index = _workingBlock1.NextOverflowBlockIndex;
                    while (index != -1)
                    {
                        listOverflowBlocks.Add(new Block<T>(MaxRecordsInOverflow));
                        listOverflowBlocks[listOverflowBlocks.Count -1].FromByteArray(_overflowFile.ReadData(index));
                        index = listOverflowBlocks[listOverflowBlocks.Count - 1].NextOverflowBlockIndex;
                    }

                    //to ci uz existuje v normalnom bloku sa overilo hore
                    foreach (var ofBlock in listOverflowBlocks) //overit ci niekde uz nie je v preplnovacke
                    {
                        if (ofBlock.TryFindRecord(record, out var dataRec))
                        {
                            return false; //v nejakom z nich uz existuje
                        }
                    }

                    if (node.RecordCount - MaxRecords < listOverflowBlocks.Count * MaxRecordsInOverflow) //niekde sa zmestia
                    {
                        if (!_workingBlock1.AddRecord(record)) //ak sa nezmesti do normalneho skus do ostatnych
                        {
                            foreach (var ofBlock in listOverflowBlocks) //prechadzat a naplnat
                            {
                                if (ofBlock.AddRecord(record))
                                {
                                    break;
                                }
                            }

                        }
                        
                    }
                    else //nikde sa nezmestia - vytvorit novy
                    {
                        _overflowBlock = new Block<T>(MaxRecordsInOverflow);
                        _overflowBlock.AddRecord(record);
                        if (_freeBlocksOverflow.Count > 0)
                        {
                            index = _freeBlocksOverflow.Min(i => i);
                            _freeBlocksOverflow.Remove(index);
                            _overflowFile.WriteData(index, _overflowBlock.ToByteArray());
                        }
                        else
                        {
                            index = _overflowFile.WriteData(_overflowBlock.ToByteArray());
                        }
                        if(listOverflowBlocks[listOverflowBlocks.Count - 1].NextOverflowBlockIndex != -1)
                            throw new Exception($"This should not happened, index of block: {node.IndexOfBlock} and should be -1");
                        listOverflowBlocks[listOverflowBlocks.Count - 1].NextOverflowBlockIndex = index;
                    }
                    node.RecordCount++;

                    for (int i = listOverflowBlocks.Count - 1; i > 0 ; i--)
                    {
                        _overflowFile.WriteData(listOverflowBlocks[i - 1].NextOverflowBlockIndex,
                            listOverflowBlocks[i].ToByteArray()); //kazdeho zapisem podla indexu predchadzajuceho (az na prveho)
                    }

                    _overflowFile.WriteData(_workingBlock1.NextOverflowBlockIndex, listOverflowBlocks[0].ToByteArray());
                }
                _blockFile.WriteData(node.IndexOfBlock, _workingBlock1.ToByteArray()); //zapisem povodny blok
            }
            else
            {
                if(node.IndexOfBlock != -1)
                    throw new Exception($"This should not happened, index of block: {node.IndexOfBlock} and should be -1");
                //zapis nodu novy working blok a daj mu adresu na prvy preplnovak kde pojde record
                //record count ++;

                if (workingBlock.TryFindRecord(record, out var dataRec))
                {
                    throw new Exception($"This should not happened, block already contains record");
                }

                _overflowBlock = new Block<T>(MaxRecordsInOverflow);
                _overflowBlock.AddRecord(record);
                var index = -1;
                if (_freeBlocksOverflow.Count > 0)
                {
                    index = _freeBlocksOverflow.Min(i => i);
                    _freeBlocksOverflow.Remove(index);
                    _overflowFile.WriteData(index, _overflowBlock.ToByteArray());
                }
                else
                {
                    index = _overflowFile.WriteData(_overflowBlock.ToByteArray());
                }
                if(index == -1)
                    throw new Exception($"This should not happened, index of block: {node.IndexOfBlock} and should NOT be -1");
                workingBlock.NextOverflowBlockIndex = index;
                node.IndexOfBlock = WriteNewBlockAndGetIndex(workingBlock);
                node.RecordCount++;
            }

            return true;
        }

        private int WriteNewBlockAndGetIndex(Block<T> block)
        {
            if (_freeBlocks.Count > 0)
            {
                var minIndex = _freeBlocks.Min(i => i);
                _freeBlocks.Remove(minIndex);
                _blockFile.WriteData(minIndex, block.ToByteArray());
                return minIndex;
            }
            return _blockFile.WriteData(block.ToByteArray());
        }

        private bool PutRecordInCorrectBlock(ExternalTrieNode leftExternal, ExternalTrieNode rightExternal, T rec)
        {
            var bArray = new BitArray(new int[1] { rec.GetHashCode() });
            bool result;
            if (_currentDepth == bArray.Length)
                return false;
            if (bArray[_currentDepth])
            {
                result = _rightWorkingBlock.AddRecord(rec);
                if (result && rightExternal != null)
                    rightExternal.RecordCount++;
            }
            else
            {
                result = _leftWorkingBlock.AddRecord(rec);
                if (result && leftExternal != null)
                    leftExternal.RecordCount++;
            }

            return result;
        }

        private InternalTrieNode TransformExternal(ExternalTrieNode node)
        {
            var inter = new InternalTrieNode { Parent = node.Parent };
            if (node.InternalParent?.LeftChild == node)
            {
                node.InternalParent.LeftChild = inter;
            }
            else
            {
                if (node.InternalParent != null)
                    node.InternalParent.RightChild = inter;
            }

            if (node == Root)
            {
                Root = inter;
            }

            inter.LeftChild = new ExternalTrieNode();
            inter.RightChild = new ExternalTrieNode();
            return inter;
        }

        public bool TryRemove(T record, out T removedRecord)
        {

            _currentDepth = 0;
            removedRecord = default(T);
            var node = FindExternalNode(record.GetHashCode());
            if (node.RecordCount == 0)
                return false;

            if (node.RecordCount > MaxRecords)
            {
                if (!TryRemoveOverflow(node, record, out removedRecord, out var canDoJoin))
                {
                    return false;
                }
                if(canDoJoin)
                    DoJoinNodes(node);
                return true;
            }

            _workingBlock1 = new Block<T>(MaxRecords);
            _workingBlock1.FromByteArray(_blockFile.ReadData(node.IndexOfBlock));
            if (_workingBlock1.TryRemoveRecord(record, out removedRecord))
            {
                node.RecordCount--;
                DoJoinNodes(node);
                return true;
            }

            removedRecord = default(T);
            return false;
        }

        private void DoJoinNodes(ExternalTrieNode node)
        {
            while (CanJoinNodes(node, _workingBlock1.ValidCount))
            {
                if (node.IndexOfBlock > -1)
                {
                    _freeBlocks.Add(node.IndexOfBlock);
                }

                if (node.ExternalBrother.IndexOfBlock > -1)
                {
                    _freeBlocks.Add(node.ExternalBrother.IndexOfBlock);
                }


                if (node.ExternalBrother.RecordCount > 0) //len ak ma load zmysel
                {
                    _leftWorkingBlock = new Block<T>(MaxRecords);
                    _leftWorkingBlock.FromByteArray(_blockFile.ReadData(node.ExternalBrother.IndexOfBlock));
                    AddBlockData(_leftWorkingBlock, _workingBlock1);
                }

                node = TransformInternal(node.InternalParent);
            }

            if (_workingBlock1.ValidCount > 0)
            {

                if (node.IndexOfBlock == -1)
                {
                    node.IndexOfBlock = WriteNewBlockAndGetIndex(_workingBlock1);
                }
                else
                {
                    _blockFile.WriteData(node.IndexOfBlock, _workingBlock1.ToByteArray());
                }
            }
            else
            {
                _freeBlocks.Add(node.IndexOfBlock);
                node.IndexOfBlock = -1;
            }
            node.RecordCount = _workingBlock1.ValidCount;
            AdjustFile();
        }

        private bool TryRemoveOverflow(ExternalTrieNode node, T record, out T removedRecord, out bool canDoJoin)
        {
            //najskor pozriet do seba ci nemam node
            //ak nie
            //nacitat vsetky ostatne (ak mam) 
            //najst a vymazat
            //skusit utriast
            removedRecord = default(T);
            canDoJoin = false;
            _workingBlock1 = new Block<T>(MaxRecords);
            _workingBlock1.FromByteArray(_blockFile.ReadData(node.IndexOfBlock));

            var listOverflowBlocks = new List<Block<T>>();
            var index = _workingBlock1.NextOverflowBlockIndex;
            if(index == -1)
                throw new Exception($"This should not happened, index of block: {node.IndexOfBlock} and should NOT be -1");
            
            while (index != -1) //nacitam vsetky preplnene
            {
                listOverflowBlocks.Add(new Block<T>(MaxRecordsInOverflow));
                listOverflowBlocks[listOverflowBlocks.Count - 1].FromByteArray(_overflowFile.ReadData(index));
                index = listOverflowBlocks[listOverflowBlocks.Count - 1].NextOverflowBlockIndex;
            }

            var currentCount = node.RecordCount;
            if (_workingBlock1.TryRemoveRecord(record, out removedRecord))
            {
                node.RecordCount--;
            }
            else
            {
                foreach (var block in listOverflowBlocks)
                {
                    if (block.TryRemoveRecord(record, out removedRecord))
                    {
                        node.RecordCount--;
                        break;
                    }
                }
            }
            

            if (currentCount == node.RecordCount)
                return false; //nepodarilo sa vymazat

            if ((MaxRecords + listOverflowBlocks.Count * MaxRecordsInOverflow) - node.RecordCount ==
                MaxRecordsInOverflow)
            {
                //ideme utriasat
                var lastBlock = listOverflowBlocks[listOverflowBlocks.Count - 1];
                listOverflowBlocks.RemoveAt(listOverflowBlocks.Count-1);
                if (listOverflowBlocks.Count > 0)
                {
                    _freeBlocksOverflow.Add(listOverflowBlocks[listOverflowBlocks.Count - 1].NextOverflowBlockIndex);
                    listOverflowBlocks[listOverflowBlocks.Count - 1].NextOverflowBlockIndex = -1;
                }
                else
                {
                    _freeBlocksOverflow.Add(_workingBlock1.NextOverflowBlockIndex);
                    _workingBlock1.NextOverflowBlockIndex = -1;
                }
                _overflowFile.AdjustFileSize(_freeBlocksOverflow);

                var helpCounter = 0;
                foreach (var rec in lastBlock.GetAllRecords())
                {
                    if (_workingBlock1.AddRecord(rec))
                    {
                        helpCounter++;
                        continue;
                    }

                    foreach (var blockInList in listOverflowBlocks)
                    {
                        if (blockInList.AddRecord(rec))
                        {
                            helpCounter++;
                            break;
                        }
                           
                    }
                }

                if(helpCounter != lastBlock.ValidCount)
                    throw new Exception($"This should not happened, not all blocks were added");

                //mame utrasene mozme zapisat
            }

            for (int i = listOverflowBlocks.Count - 1; i > 0; i--)
            {
                _overflowFile.WriteData(listOverflowBlocks[i - 1].NextOverflowBlockIndex,
                    listOverflowBlocks[i].ToByteArray()); //kazdeho zapisem podla indexu predchadzajuceho (az na prveho)
            }

            if(listOverflowBlocks.Count > 0)
                _overflowFile.WriteData(_workingBlock1.NextOverflowBlockIndex, listOverflowBlocks[0].ToByteArray());
            canDoJoin = CanJoinNodes(node, node.RecordCount);
            if (!canDoJoin) //zapisem povodny blok len ak uz sa nejde s nim hybat
                _blockFile.WriteData(node.IndexOfBlock, _workingBlock1.ToByteArray()); 

            if(node.RecordCount < MaxRecords)
                throw new Exception($"This should not happened, record count < _maxrecords during overflow deleting");

            return true;

        }

        private bool CanJoinNodes(ExternalTrieNode node, int recordsInBlock)
        {
            return node.HasExternalBrother && (recordsInBlock + node.ExternalBrother.RecordCount) <= MaxRecords;
        }

        private void AdjustFile()
        {
            _blockFile.AdjustFileSize(_freeBlocks);
        }

        private void AddBlockData(Block<T> oldBlock, Block<T> destinationBlock)
        {
            if (oldBlock.GetAllRecords().Any(record => !destinationBlock.AddRecord(record)))
            {
                throw new Exception("Adding record during joining, should not happen");
            }
        }

        private ExternalTrieNode TransformInternal(InternalTrieNode internalTrieNode)
        {
            var ext = new ExternalTrieNode();
            if (internalTrieNode == Root)
            {
                Root = ext;
            }
            else
            {
                if (internalTrieNode.InternalParent.LeftChild == internalTrieNode)
                {
                    internalTrieNode.InternalParent.LeftChild = ext;
                }
                else
                {
                    internalTrieNode.InternalParent.RightChild = ext;
                }
                ext.Parent = internalTrieNode.Parent;
            }

            return ext;

        }

        public StringBuilder WriteAllBlocks()
        {
            var builder = new StringBuilder(_blockFile.MaxFileIndex + 3);
            builder.AppendLine($"Max file block index: {_blockFile.MaxFileIndex}");
            builder.AppendLine($"Max records in one block: {MaxRecords}");
            builder.AppendLine("Free blocks: " + _freeBlocks.ListToString(';'));
            builder.AppendLine();
            
            _workingBlock1 = new Block<T>(MaxRecords); //just to clear it
            var validIndex = 0;
            foreach (var byteData in _blockFile.BlocksSequence())
            {
                _workingBlock1.FromByteArray(byteData);
                if (_workingBlock1.ValidCount > 0 && !_freeBlocks.Contains(validIndex))
                {
                    builder.AppendLine($"Current block index: {validIndex}, address: {validIndex * _workingBlock1.GetSize()}");
                    builder.AppendLine(_workingBlock1.ToString());
                }
                else
                {
                    builder.AppendLine($"Current block index: {validIndex}, address: {validIndex * _workingBlock1.GetSize()} IS NOT VALID!!");
                }

                validIndex++;
            }

            return builder;
        }

        public StringBuilder WriteAllOverflowBlocks()
        {
            var builder = new StringBuilder(_overflowFile.MaxFileIndex + 3);
            builder.AppendLine($"Max overflow file block index: {_overflowFile.MaxFileIndex}");
            builder.AppendLine($"Max records in one overflow block: {MaxRecordsInOverflow}");
            builder.AppendLine("Free overflow blocks: " + _freeBlocksOverflow.ListToString(';'));
            builder.AppendLine();

            _workingBlock1 = new Block<T>(MaxRecordsInOverflow); //just to clear it
            var validIndex = 0;
            foreach (var byteData in _overflowFile.BlocksSequence())
            {
                _workingBlock1.FromByteArray(byteData);
                if (_workingBlock1.ValidCount > 0 && !_freeBlocksOverflow.Contains(validIndex))
                {
                    builder.AppendLine($"Current block index: {validIndex}, address: {validIndex * _workingBlock1.GetSize()}");
                    builder.AppendLine(_workingBlock1.ToString());
                }
                else
                {
                    builder.AppendLine($"Current block index: {validIndex}, address: {validIndex * _workingBlock1.GetSize()} IS NOT VALID!!");
                }

                validIndex++;
            }

            return builder;
        }


        public void SaveTreeData()
        {
            var builder = new StringBuilder();

            builder.AppendLine(MaxRecords.ToString());
            builder.AppendLine(MaxRecordsInOverflow.ToString());
            builder.AppendLine(_freeBlocks.ListToString(';'));
            builder.AppendLine(_freeBlocksOverflow.ListToString(';'));

            foreach (var trieNode in PreOrderTraversal())
            {
                builder.AppendLine(trieNode.GetStringRepresentation());
            }

            using (StreamWriter sw = new StreamWriter(_files.FileTreeData, false))
            {
                sw.Write(builder.ToString());
            }

        }

        public void LoadTreeData()
        {
            var list = new List<string>();
            try
            {
                using (StreamReader sr = new StreamReader(_files.FileTreeData))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        list.Add(line);
                    }

                }
            }
            catch (Exception)
            {
                return;
            }

            if (list.Count == 0) //nic v subore nie je
            {
                return;
            }
            Root = null;
            MaxRecords = Int32.Parse(list[0]);
            MaxRecordsInOverflow = Int32.Parse(list[1]);
            _workingBlock1 = new Block<T>(MaxRecords);
            _overflowBlock = new Block<T>(MaxRecordsInOverflow);
            if (list[2] != "")
            {
                _freeBlocks.StringToList(';',list[2]);
            }
            if (list[3] != "")
            {
                _freeBlocksOverflow.StringToList(';',list[3]);
            }

            var stack = new Stack<InternalTrieNode>();

            for (int i = 4; i < list.Count; i++)
            {
                if (list[i][0] == 'I')
                {
                    var iNode = new InternalTrieNode();
                    if (Root == null)
                    {
                        Root = iNode;
                    }
                    else
                    {
                        CorrectInsertToTree(stack, iNode);
                    }
                    stack.Push(iNode);
                }
                else
                {
                    var eNode = new ExternalTrieNode();
                    eNode.LoadDataFromString(list[i]);
                    if (Root == null)
                    {
                        Root = eNode;
                    }
                    else
                    {
                        CorrectInsertToTree(stack, eNode);
                    }
                }
            }
        }

        private void CorrectInsertToTree(Stack<InternalTrieNode> freeParents, TrieNode newNode)
        {
            var parent = freeParents.Pop();
            newNode.Parent = parent;
            if (parent.LeftChild == null)
            {
                parent.LeftChild = newNode;
                freeParents.Push(parent);
            }
            else
            {
                parent.RightChild = newNode;
            }
        }

        public IEnumerable<TrieNode> PreOrderTraversal()
        {
            if (Root == null)
                yield break;

            var stack = new Stack<TrieNode>();
            stack.Push(Root);

            while (stack.Count > 0)
            {
                var node = stack.Pop();
                yield return node;

                if ((node as InternalTrieNode)?.RightChild != null)
                    stack.Push((node as InternalTrieNode).RightChild);
                if ((node as InternalTrieNode)?.LeftChild != null)
                    stack.Push((node as InternalTrieNode).LeftChild);
            }

        }

    }
}
