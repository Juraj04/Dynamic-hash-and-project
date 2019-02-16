using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using DynamicHash;

namespace SystemCore
{
    class Program
    {
        static void Main(string[] args)
        {

            var r1 = new Realty
            {
                Description = " ",
                Id = 69,
                RegisterNumber = 85,
                UniqueName = " "
            };
            var s1 = r1.GetSize();
            var b1 = r1.ToByteArray();
            var r2 = new Realty();
            r2.FromByteArray(b1);
            var s2 = r2.GetSize();
            var l = s1 == s2;

            var help1 = new RealtyByCadAndRegNumber(r2);

            var bhelp1 = help1.ToByteArray();

            var help2 = new RealtyByCadAndRegNumber();
            help2.FromByteArray(bhelp1);

            var s = help1.Equals(help2);


        }
    }
}
