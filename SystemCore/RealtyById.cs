using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DynamicHash;

namespace SystemCore
{
    class RealtyById: IRecord<RealtyById>
    {
        public int Id { get; set; }
        public int AddressIndex { get; set; }

        public RealtyById()
        {
            Id = -1;
            AddressIndex = -1;
        }

        public RealtyById(Realty r)
        {
            CreateFromRealty(r);
        }

        public byte[] ToByteArray()
        {
            var result = new List<byte>();
            result.AddRange(BitConverter.GetBytes(Id));
            result.AddRange(BitConverter.GetBytes(AddressIndex));
            return result.ToArray();
        }

        public void FromByteArray(byte[] byteArray)
        {
            var index = 0;
            Id = BitConverter.ToInt32(byteArray, index);
            index += sizeof(int);
            AddressIndex = BitConverter.ToInt32(byteArray, index);
        }

        public int GetSize()
        {
            return 2 * sizeof(int);
        }

        public bool Equals(RealtyById other)
        {
            return other != null && Id == other.Id;
        }

        public void CreateFromRealty(Realty r)
        {
            Id = r.Id;
        }

        public override string ToString()
        {
            var text = "";
            text += $"Id: {Id} \r\n";
            text += $"AddressIndex: {AddressIndex} \r\n";
            text += $"Byte size: {GetSize()} \r\n";
            return text;
        }

        public override int GetHashCode()
        {
            //return 1;
            return Id % 2;
        }
    }
}
