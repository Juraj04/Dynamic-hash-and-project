using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DynamicHash;

namespace StructureTester
{
    class Program
    {
        static void Main(string[] args)
        {

            var j = new FilePaths()
            {
                FileTreeData = "C:/Users/janik/source/repos/US_SemestralnaPraca2/DynamicHash/data/data.txt",
                FileBlockData = "C:/Users/janik/source/repos/US_SemestralnaPraca2/SystemCore/data/data.txt",
                FileOverflowFile = "C:/Users/janik/source/repos/US_SemestralnaPraca2/SystemCore/data/overflowFile.txt"
            };
            

            
            //var raf = new RafTester();
            //raf.DoRafTesting();
            //return;

            //var rTest = new TesterForRealtyClass();
            //rTest.DoDhTesting();
            //return;

            var t = new RandomOperationsTester();
            t.DoDhTesting();
            return;


            var dh = new DynHash<TestRecord>(j, 2, 4);

            while (true)
            {
                Console.WriteLine("__________________________________________");
                Console.WriteLine("Write number to insert new value");
                Console.WriteLine("I-insert / D-delete / F-find value, E-exit, S-save, L-load");
                var line = Console.ReadLine();
                
                    
                var operation = line[0];
                var sValue = line.Substring(1);
                
                switch (Char.ToLower(operation))
                {
                    case 'i':
                    {
                        dh.Add(new TestRecord(Int32.Parse(sValue)));
                        break;
                    }
                    case 'd':
                    {
                        if (dh.TryRemove(new TestRecord(Int32.Parse(sValue)), out var removed))
                            Console.WriteLine($"Removed: {removed.Number}");

                       break;
                    }
                    case 'e':
                    {
                        return;
                    }
                    case 'l':
                    {
                        dh.LoadTreeData();
                        Console.WriteLine("Loaded");
                        break;
                    }
                    case 's':
                    {
                        dh.SaveTreeData();
                        Console.WriteLine("Saved");
                        break;
                    }
                    case 'f':
                    {
                        Console.WriteLine(dh.TryFind(new TestRecord(Int32.Parse(sValue)),out var r ));
                        break;
                    }
                    default:
                    {
                        break;
                    }
                }
                //dh.Root.Print();
                var free = "";
                //foreach (var dhFreeBlock in dh._freeBlocks)
                //{
                 //   free += dhFreeBlock + ", ";
                //}

                //Console.WriteLine("Free blocks: " + free);
                //Console.WriteLine("Max file index: " + dh._blockFile.MaxFileIndex);

            }
        }
    }
}
