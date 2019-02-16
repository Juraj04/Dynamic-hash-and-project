using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicHash
{
    public class BinaryFileManager
    {

        public readonly string FilePath;
        public int DataSize { get; set; }
        private FileStream _fileStream;
        public int MaxFileIndex => (int) (_fileStream.Length / DataSize) -1;

        public BinaryFileManager(string filePath, int dataSize)
        {
            FilePath = filePath;
            DataSize = dataSize;
            _fileStream = new FileStream(FilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        }

        ~BinaryFileManager()
        {
            _fileStream.Dispose();
        }

        public void CutOffFile(int index)
        {
            int currentFileIndex = (int)(_fileStream.Length / DataSize);
            if (index <= (currentFileIndex - 1))
            {
                _fileStream.SetLength((index) * DataSize);
            }
        }

        public void AdjustFileSize(List<int> freeIndexes)
        {
            var currentIndex = MaxFileIndex;
            var size = freeIndexes.Count;

            while (freeIndexes.Contains(currentIndex))
            {
                freeIndexes.Remove(currentIndex);
                currentIndex--;
            }

            if (size != freeIndexes.Count)
            {
                currentIndex++;
                CutOffFile(currentIndex);
            }
        }

        public void ResetFile()
        {
            _fileStream.SetLength(0);
        }

        public IEnumerable<byte[]> BlocksSequence()
        {
            for (int i = 0; i <= MaxFileIndex; i++)
            {
                yield return ReadData(i);
            }
        }

        public byte[] ReadData(int index)
        {
            var result = new byte[DataSize];
            _fileStream.Seek(index * DataSize, SeekOrigin.Begin);
            _fileStream.Read(result, 0, DataSize);

            return result;
        }

        public void WriteData(int index, byte[] data)
        {
            _fileStream.Seek(index * data.Length, SeekOrigin.Begin);
            _fileStream.Write(data, 0, data.Length);
            _fileStream.Flush();
            
        }

        public int WriteData(byte[] data)
        {
            var l = _fileStream.Length;
            _fileStream.Seek(_fileStream.Length, SeekOrigin.Begin);
            _fileStream.Write(data, 0, data.Length);
            _fileStream.Flush();
            var lnow = _fileStream.Length;

            return MaxFileIndex;
        }
    }
}
