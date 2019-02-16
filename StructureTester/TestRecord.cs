using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DynamicHash;

namespace StructureTester
{
    class TestRecord: IRecord<TestRecord>
    {
        public int Number { get; set; }
        public TestRecord(int number)
        {
            Number = number;
        }

        public TestRecord()
        {
            Number = -1;
        }

        public byte[] ToByteArray()
        {
            return BitConverter.GetBytes(Number);
        }

        public void FromByteArray(byte[] byteArray)
        {
            Number = BitConverter.ToInt32(byteArray, 0);
        }

        public int GetSize()
        {
            return sizeof(int);
        }

        public bool Equals(TestRecord other)
        {
            return Number == other?.Number;
        }

        public override int GetHashCode()
        {
            //return Number;
            return Number % 4;
        }

        public override string ToString()
        {
            return Number.ToString();
        }
    }
}
