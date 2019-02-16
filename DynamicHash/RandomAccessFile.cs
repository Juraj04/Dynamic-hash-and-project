using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicHash
{
    public class RandomAccessFile<T> where T : IRecord<T>, new()
    {
        private List<int> _freeBlocks; 
        private List<int> _partiallyFullBlocks; 
        private BinaryFileManager _file; 
        private Block<T> _workingBlock;
        public int MaxRecords;
        private readonly string _fileTreeData;

        public RandomAccessFile(string fileName, string fileTreeData, int maxRecords)
        {
            _workingBlock = new Block<T>(maxRecords);
            MaxRecords = maxRecords;
            _file = new BinaryFileManager(fileName, _workingBlock.GetSize());
            _freeBlocks = new List<int>();
            _partiallyFullBlocks = new List<int>();
            _fileTreeData = fileTreeData;
        }

        
        public bool Add(T record, out int index)
        {
            _workingBlock = new Block<T>(MaxRecords);
            if (_partiallyFullBlocks.Count > 0)
            {
                index = _partiallyFullBlocks.Min(i => i);
                _workingBlock.FromByteArray((_file.ReadData(index)));
                if (_workingBlock.AddRecord(record))
                {
                    if (_workingBlock.ValidCount == MaxRecords)
                        _partiallyFullBlocks.Remove(index);
                    _file.WriteData(index, _workingBlock.ToByteArray());
                    return true;
                }
            }
            else if (_freeBlocks.Count > 0)
            {
                index = _freeBlocks.Min(i => i);
                _workingBlock.FromByteArray((_file.ReadData(index)));
                if (_workingBlock.AddRecord(record))
                {
                    _freeBlocks.Remove(index);
                    _partiallyFullBlocks.Add(index);
                    _file.WriteData(index, _workingBlock.ToByteArray());
                    return true;
                }
            }
            else
            {
                if (_workingBlock.AddRecord(record))
                {
                    index = _file.WriteData(_workingBlock.ToByteArray());
                    if(_workingBlock.ValidCount < MaxRecords)
                        _partiallyFullBlocks.Add(index);
                    return true;
                }
            }

            index = -1;
            return false;
        }

        public void Clear()
        {
            _workingBlock = new Block<T>(MaxRecords);
            _freeBlocks.Clear();
            _partiallyFullBlocks.Clear();
            _file.ResetFile();
            ResetFileDataSize();
        }

        public void ResetFileDataSize()
        {
            _file.DataSize = _workingBlock.GetSize();

        }

        public void SaveConfData()
        {
            var builder = new StringBuilder();
            builder.AppendLine(MaxRecords.ToString());
            builder.AppendLine(_partiallyFullBlocks.ListToString(';'));
            builder.AppendLine(_freeBlocks.ListToString(';'));

            using (StreamWriter sw = new StreamWriter(_fileTreeData, false))
            {
                sw.Write(builder.ToString());
            }
        }

        public void LoadConfData()
        {
            var list = new List<string>();
            try
            {
                using (StreamReader sr = new StreamReader(_fileTreeData))
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

            if (list.Count == 0)
                return;

            MaxRecords = Int32.Parse(list[0]);
            _workingBlock = new Block<T>(MaxRecords);
            if (list[1].Length > 0)
            {
                _partiallyFullBlocks.StringToList(';',list[1]);
            }
            if (list[2].Length > 0)
            {
                _freeBlocks.StringToList(';',list[2]);
            }

        }

        public bool TryFind(T record, int index, out T removedRecord)
        {
            _workingBlock = new Block<T>(MaxRecords);
            _workingBlock.FromByteArray(_file.ReadData(index));
            return _workingBlock.TryFindRecord(record, out removedRecord);

        }

        public bool TryRemove(T record, int index, out T removedRecord)
        {
            _workingBlock = new Block<T>(MaxRecords);
            _workingBlock.FromByteArray(_file.ReadData(index));
            removedRecord = default(T);

            if (_workingBlock.TryRemoveRecord(record, out removedRecord))
            {
                if (_workingBlock.ValidCount == 0)
                {
                    _partiallyFullBlocks.Remove(index);
                    _freeBlocks.Add(index);
                    _file.AdjustFileSize(_freeBlocks);
                }
                else
                {
                    if (!_partiallyFullBlocks.Contains(index))
                        _partiallyFullBlocks.Add(index);
                    _file.WriteData(index,_workingBlock.ToByteArray());
                }
                return true;
            }

            return false;
        }

        public bool BeginUpdate(T record, int index, out T data) //dvojicka begin a finish
        {
            _workingBlock = new Block<T>(MaxRecords);
            _workingBlock.FromByteArray(_file.ReadData(index));
            data = default(T);
            return _workingBlock.TryRemoveRecord(record, out data); //necham v pamati (v subore je stale original)
        }

        public bool FinishUpdate(T record, int index) //ak mi toto pride, prepisem blok v subore, ak nepride, ostane original
        {
            if (_workingBlock.AddRecord(record))
            {
                _file.WriteData(index, _workingBlock.ToByteArray());
                return true;
            }

            return false;
        }


        public StringBuilder WriteAllBlocks()
        {
            var builder = new StringBuilder(_file.MaxFileIndex + 4);
            builder.AppendLine($"Max block index: {_file.MaxFileIndex}");
            builder.AppendLine($"Max records in one block: {MaxRecords}");
            builder.AppendLine("Not full yet: " + _partiallyFullBlocks.ListToString(';'));
            builder.AppendLine("Free blocks: " + _freeBlocks.ListToString(';'));
            builder.AppendLine();

            _workingBlock = new Block<T>(MaxRecords); //just to clear it
            var validIndex = 0;
            foreach (var byteData in _file.BlocksSequence())
            {
                _workingBlock.FromByteArray(byteData);
                if (_workingBlock.ValidCount > 0 && !_freeBlocks.Contains(validIndex))
                {
                    builder.AppendLine($"Current block index: {validIndex}, address: {validIndex * _workingBlock.GetSize()}");
                    builder.AppendLine(_workingBlock.ToString());
                }
                else
                {
                    builder.AppendLine($"Current block index: {validIndex}, address: {validIndex * _workingBlock.GetSize()} IS NOT VALID!!");
                }

                validIndex++;
            }

            return builder;
        }
    }
}
