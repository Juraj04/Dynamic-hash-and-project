using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DynamicHash;

namespace SystemCore
{
    public class RealtyByCadAndRegNumber : IRecord<RealtyByCadAndRegNumber>
    {
        public int RealNameLength { get; private set; }
        public int RegisterNumber { get; set; }
        public int AddressIndex { get; set; }
        private const int UniqueNameSize = 15;
        private string _name;
        public string UniqueName
        {
            get => _name.Substring(0, RealNameLength);
            set
            {
                if (value.Length > UniqueNameSize)
                    throw new ArgumentOutOfRangeException($"Max length of name is {UniqueNameSize}");
                RealNameLength = value.Length;
                _name = value + Routines.GetFakeStringOfSize(UniqueNameSize - RealNameLength);
            }
        }

        public RealtyByCadAndRegNumber()
        {
            UniqueName = "";
        }

        public RealtyByCadAndRegNumber(Realty r)
        {
            CreateFromRealty(r);
        }

        public byte[] ToByteArray()
        {
            var result = new List<byte>();
            result.AddRange(BitConverter.GetBytes(RegisterNumber));
            result.AddRange(BitConverter.GetBytes(AddressIndex));
            result.AddRange(BitConverter.GetBytes(RealNameLength));
            result.AddRange(Encoding.ASCII.GetBytes(_name));
            return result.ToArray();
        }

        public void FromByteArray(byte[] byteArray)
        {
            var index = 0;
            RegisterNumber = BitConverter.ToInt32(byteArray, index);
            index += sizeof(int);
            AddressIndex = BitConverter.ToInt32(byteArray, index);
            index += sizeof(int);
            RealNameLength = BitConverter.ToInt32(byteArray, index);
            index += sizeof(int);
            _name = Encoding.ASCII.GetString(byteArray, index, byteArray.Length - index);
            

        }

        public int GetSize()
        {
            var result = 3 * sizeof(int);
            result += Encoding.ASCII.GetByteCount(_name);
            return result;
        }

        public bool Equals(RealtyByCadAndRegNumber other)
        {
            return RegisterNumber == other?.RegisterNumber && UniqueName == other.UniqueName;
        }

        public void CreateFromRealty(Realty r)
        {
            UniqueName = r.UniqueName;
            RegisterNumber = r.RegisterNumber;
        }

        public override string ToString()
        {
            var text = "";
            text += $"Unique name: {UniqueName} \r\n";
            text += $"Register number: {RegisterNumber} \r\n";
            text += $"AddressIndex: {AddressIndex} \r\n";
            text += $"Byte size: {GetSize()} \r\n";
            return text;
        }

        public override int GetHashCode()
        {
            var hashCode = 1103962590;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(UniqueName);
            hashCode = hashCode * -1521134295 + RegisterNumber.GetHashCode();
            return Math.Abs(hashCode) % 2;
            //return 1;
        }
    }
}
