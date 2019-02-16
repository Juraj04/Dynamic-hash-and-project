using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicHash
{
    public interface IConverter
    {
        byte[] ToByteArray();
        void FromByteArray(byte[] byteArray);
        int GetSize();
    }
}
