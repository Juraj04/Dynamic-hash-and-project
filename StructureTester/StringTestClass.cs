using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DynamicHash;

namespace StructureTester
{
    public static class Extension
    {
        public static T[] SubArray<T>(this T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }
    }
    class StringTestClass: IRecord<StringTestClass>
    {
        public string Name { get; set; }
        public const int NameSize = 15;

        public StringTestClass()
        {
            Name = "";
        }

        public StringTestClass(string name)
        {
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }

        public byte[] ToByteArray()
        {
            var result = new List<byte>();
            result.AddRange(BitConverter.GetBytes(Encoding.ASCII.GetByteCount(Name)));    //5. byteCount name
            result.AddRange(Encoding.ASCII.GetBytes(Name));                               //6. name
            if (Name.Length - NameSize != 0)
                result.AddRange(Encoding.ASCII.GetBytes(GetFakeStringOfSize(NameSize - Name.Length)));
            return result.ToArray();
        }

        public static string GetFakeStringOfSize(int size)
        {
            return new string('a', size);
        }

        public void FromByteArray(byte[] byteArray)
        {
            var index = 0;
            var nameCount = BitConverter.ToInt32(byteArray, index);
            index += sizeof(int);

            Name = Encoding.ASCII.GetString(byteArray.SubArray(index, nameCount));
        }

        public int GetSize()
        {
            return Encoding.ASCII.GetByteCount(GetFakeStringOfSize(NameSize)) + sizeof(int);
        }

        public bool Equals(StringTestClass other)
        {
            return Name == other.Name;
        }

        public override int GetHashCode()
        {
            return 539060726 + EqualityComparer<string>.Default.GetHashCode(Name);
        }
    }
}
