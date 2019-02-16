using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicHash
{
    public class Block<T> : IConverter where T: IRecord<T>,new()
    {
        public List<T> Records { get; set; }
        public int ValidCount { get; private set; }
        public int NextOverflowBlockIndex { get; set; }

        public Block(int maxRecords)
        {
            ValidCount = 0;
            Records = new List<T>(maxRecords);
            NextOverflowBlockIndex = -1;
            InitDefaultRecords();
        }

        private void InitDefaultRecords()
        {
            Records.Clear();
            
            for (int i = 0; i < Records.Capacity; i++)
            {
                Records.Add(new T());
            }
        }

        public byte[] ToByteArray()
        {
            var result = new List<byte>();
            var byteValidCount = BitConverter.GetBytes(ValidCount);
            var byteNextIndex = BitConverter.GetBytes(NextOverflowBlockIndex);
            result.AddRange(byteValidCount);
            result.AddRange(byteNextIndex);

            foreach (var record in Records)
            {
                result.AddRange(record.ToByteArray());
            }

            return result.ToArray();
        }

        public void FromByteArray(byte[] byteArray)
        {
            ValidCount = BitConverter.ToInt32(byteArray, 0);
            var currIndex = sizeof(int);
            NextOverflowBlockIndex = BitConverter.ToInt32(byteArray, currIndex);
            currIndex += sizeof(int);


            foreach (var record in Records)
            {
                record.FromByteArray(byteArray.SubArray(currIndex, record.GetSize()));
                currIndex += record.GetSize();
            }
        }

        public int GetSize()
        {
            var size = 2*sizeof(int);
            foreach (var record in Records)
            {
                size += record.GetSize();
            }

            return size;
        }

        public bool AddRecord(T record) 
        {
            if (ValidCount == Records.Count) //ak je plno
                return false;
            
            for (int i = 0; i < ValidCount; i++)
            {
                if (record.Equals(Records[i]))
                    return false;
            }
            
            Records[ValidCount] = record;
            ValidCount++;
            return true;
        }

        public bool TryFindRecord(T record, out T outDataRecord)
        {
            outDataRecord = default(T);
            for (int i = 0; i < ValidCount; i++)
            {
                if (record.Equals(Records[i]))
                {
                    outDataRecord = Records[i];
                    return true;
                }
            }

            return false;
        }

        public bool TryRemoveRecord(T record, out T outDataRecord)
        {
            outDataRecord = default(T);
                
            for (int i = 0; i < ValidCount; i++)
            {
                if (record.Equals(Records[i]))
                {
                    outDataRecord = Records[i];
                    Records.Remove(Records[i]);
                    ValidCount--;
                    Records.Add(new T());
                    return true;
                }
            }

            return false;
        }

        public IEnumerable<T> GetAllRecords()
        {
            for (var i = 0; i < ValidCount; i++)
            {
                yield return Records[i];
            }
        }

        public override string ToString()
        {
            var text = "";
            text += $"Valid count: {ValidCount} \r\n";
            text += $"Records count: {Records.Count} \r\n";
            text += $"Next block index: {NextOverflowBlockIndex} \r\n";
            text += $"Max records: {Records.Capacity} \r\n\r\n";
            

            foreach (var record in Records)
            {
                text += record.ToString() + "\r\n";
            }

            return text + "\r\n";
        }
    }
}
