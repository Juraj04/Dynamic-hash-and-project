using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DynamicHash;

namespace SystemCore
{
    public enum EqualsOptions
    {
        ById,
        ByNameAndRegNumber
    }
    public class Realty : IRecord<Realty>
    {
        public static EqualsOptions EqualsOptions { get; set; }
        public int RegisterNumber { get; set; }
        public int Id { get; set; }
        public int RealNameLength { get; private set; }
        public int RealDescLength { get; private set; }


        public const int UniqueNameSize = 15;
        private string _name;

        public string UniqueName
        {
            get => _name.Substring(0,RealNameLength);
            set
            {
                if (value.Length > UniqueNameSize)
                    throw new ArgumentOutOfRangeException($"Max length of name is {UniqueNameSize}");
                RealNameLength = value.Length;
                _name = value + Routines.GetFakeStringOfSize(UniqueNameSize - RealNameLength);
            }
        }

        public const int DescriptionSize = 20;
        private string _description;

        public string Description
        {
            get => _description.Substring(0, RealDescLength);
            set
            {
                if (value.Length > DescriptionSize)
                    throw new ArgumentOutOfRangeException($"Max length of description is {DescriptionSize}");
                RealDescLength = value.Length;
                _description = value + Routines.GetFakeStringOfSize(DescriptionSize - RealDescLength);
            }
        }
        
        public Realty(int id)
        {
            Id = id;
        }

        public Realty(int regNumber, string uniqueName)
        {
            RegisterNumber = regNumber;
            UniqueName = uniqueName;
        }

        public Realty(int registerNumber, string uniqueName, string description, int id)
        {
            RegisterNumber = registerNumber;
            UniqueName = uniqueName;
            Description = description;
            Id = id;
        }

        public Realty()
        {
            Description = "";
            UniqueName = "";
        }

        public byte[] ToByteArray()
        {
            var result = new List<byte>();
            result.AddRange(BitConverter.GetBytes(RegisterNumber));                             //1. register number
            result.AddRange(BitConverter.GetBytes(Id));                                         //2. id
            result.AddRange(BitConverter.GetBytes(RealNameLength));                            //3. dlzka mena
            result.AddRange(BitConverter.GetBytes(RealDescLength));                            //4. dlzka popisu
            result.AddRange(Encoding.ASCII.GetBytes(_name + _description));
            return result.ToArray();

        }

        public void FromByteArray(byte[] byteArray)
        {
            var index = 0;
            RegisterNumber = BitConverter.ToInt32(byteArray, index);
            index += sizeof(int);

            Id = BitConverter.ToInt32(byteArray, index);
            index += sizeof(int);

            RealNameLength = BitConverter.ToInt32(byteArray, index);
            index += sizeof(int);

            RealDescLength = BitConverter.ToInt32(byteArray, index);
            index += sizeof(int);

            var text = Encoding.ASCII.GetString(byteArray, index, byteArray.Length - index);
            _name = text.Substring(0, UniqueNameSize);
            _description = text.Substring(UniqueNameSize, DescriptionSize);

        }

        

        public int GetSize()
        {
            var size = 4 * sizeof(int);
            size += Encoding.ASCII.GetByteCount(_name + _description);
            return size;
        }

        public bool Equals(Realty other)
        {
            if (EqualsOptions == EqualsOptions.ById)
                return Id == other?.Id;
            if (EqualsOptions == EqualsOptions.ByNameAndRegNumber)
                return RegisterNumber == other?.RegisterNumber && UniqueName == other.UniqueName;
            return false;
        }

        public override int GetHashCode()
        {
            return Id;
        }

        public override string ToString()
        {
            var text = "";
            text += $"Id: {Id} \r\n";
            text += $"Register number: {RegisterNumber} \r\n";
            text += $"Unique name size: {UniqueNameSize} \r\n";
            text += $"Unique name: {UniqueName} \r\n";
            text += $"Description size: {DescriptionSize} \r\n";
            text += $"Description: {Description} \r\n";
            text += $"Byte size: {GetSize()} \r\n";
            return text;
        }


        public bool JustDifferentDescriptions(Realty other)
        {
            return UniqueName == other.UniqueName && RegisterNumber == other.RegisterNumber &&
                   Description != other.Description;
        }

        public bool CompleteEqual(Realty other)
        {
            return UniqueName == other.UniqueName && RegisterNumber == other.RegisterNumber &&
                   Description == other.Description;
        }

        public bool UniqueNameOrRegNumberNotEqual(Realty other)
        {
            return UniqueName != other.UniqueName || RegisterNumber != other.RegisterNumber;
        }
    }
}
