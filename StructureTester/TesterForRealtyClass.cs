using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DynamicHash;
using SystemCore;

namespace StructureTester
{
    class TesterForRealtyClass
    {
        public DynHash<RealtyByCadAndRegNumber> TestDynHash { get; set; }
        public List<RealtyByCadAndRegNumber> InsertedRecords { get; set; }
        public int MaxItems { get; set; }

        public TesterForRealtyClass()
        {
            var filePaths = new FilePaths()
            {
                FileBlockData = "C:/Users/janik/source/repos/US_SemestralnaPraca2/StructureTester/testData/bloky.txt",
                FileTreeData = "C:/Users/janik/source/repos/US_SemestralnaPraca2/StructureTester/testData/conf.txt",
                FileOverflowFile = "C:/Users/janik/source/repos/US_SemestralnaPraca2/StructureTester/testData/preplnovaci.txt"
            };
            TestDynHash = new DynHash<RealtyByCadAndRegNumber>(filePaths, 10, 30);
            InsertedRecords = new List<RealtyByCadAndRegNumber>();
        }

        public bool RandomRemoveInsertTest(int rounds)
        {
            Console.WriteLine("Starting random operations test");
            var current = 0;
            var randSeed = new Random();
            var randomCount = new Random(randSeed.Next());
            var randomInsOrDel = new Random(randSeed.Next());
            var randomInsertNumber = new Random(randSeed.Next());
            var randomDeleteNumber = new Random(randSeed.Next());
            var randomStringLength = new Random(randSeed.Next());
            InsertedRecords.Clear();

            while (current < rounds)
            {
                var count = randomCount.Next(50, 200);
                var insOrDel = randomInsOrDel.NextDouble();
                for (int i = 0; i < count; i++)
                {
                    if (InsertedRecords.Count == 0)
                        insOrDel = 0.1;

                    if (insOrDel < 0.5)
                    {
                        var length = randomStringLength.Next(0, 15);
                        var guid = Guid.NewGuid().ToString().Substring(0, length);
                        var num = randomInsertNumber.Next();
                        var record = new RealtyByCadAndRegNumber
                        {
                            AddressIndex = 0,
                            UniqueName = guid,
                            RegisterNumber = num
                        };
                        if (TestDynHash.Add(record))
                        {
                            var iRecord = new RealtyByCadAndRegNumber
                            {
                                AddressIndex = 0,
                                UniqueName = guid,
                                RegisterNumber = num
                            };
                            InsertedRecords.Add(iRecord);
                        }

                    }
                    else
                    {
                        var index = randomDeleteNumber.Next(0, InsertedRecords.Count - 1);
                        var value = InsertedRecords[index];
                        InsertedRecords.Remove(value);
                        var record = new RealtyByCadAndRegNumber
                        {
                            AddressIndex = value.AddressIndex,
                            UniqueName = value.UniqueName,
                            RegisterNumber = value.RegisterNumber
                        };

                        if (!TestDynHash.TryRemove(record, out var result))
                        {
                            Console.WriteLine("Did not remove value {0}", record.RegisterNumber);

                            if (TestDynHash.TryFind(record, out var value2))
                            {
                                Console.WriteLine("Still contains value after deletion");
                                return false;
                            }
                            else
                            {
                                Console.WriteLine("nie je tam ani");
                            }
                            return false;
                        }
                        if (TestDynHash.TryFind(value, out var value3))
                        {
                            Console.WriteLine("Still contains value after deletion");
                            return false;
                        }
                    }
                }

                current++;
            }

            Console.WriteLine("Random operation test finished successfully");
            return true;
        }

        public void AddData(int count)
        {
            Console.WriteLine("Starting insertion");
            var randomStringLength = new Random();
            InsertedRecords.Capacity = count;
            for (int i = 0; i < count; i++)
            {
                var length = randomStringLength.Next(0, 15);
                var guid = Guid.NewGuid().ToString().Substring(0, length);
                var record = new RealtyByCadAndRegNumber
                {
                    AddressIndex = 0,
                    UniqueName = guid,
                    RegisterNumber = i
                };
                if (TestDynHash.Add(record))
                {
                    var iRecord = new RealtyByCadAndRegNumber
                    {
                        AddressIndex = 0,
                        UniqueName = guid,
                        RegisterNumber = i
                    };
                    InsertedRecords.Add(iRecord);
                }
            }

            Console.WriteLine("Insertion test finished");
        }

        public bool TestDeleteAll(int count)
        {
            Console.WriteLine("Starting delete test");
            var rand = new Random();
            var data = InsertedRecords.OrderBy(record => rand.Next());
            var queue = new Queue<RealtyByCadAndRegNumber>();
            foreach (var testRecord in data)
            {
                queue.Enqueue(testRecord);
            }

            var help = 0;
            while (help < count)
            {
                if (!TestDynHash.TryRemove(queue.Dequeue(), out var dataRecord))
                {
                    Console.WriteLine("{0} data record not removed");
                    return false;
                }

                help++;
            }

            InsertedRecords = queue.ToList();

            Console.WriteLine("Delete test finished successfully");
            return true;

        }

        public bool TestFind()
        {
            Console.WriteLine("Starting find test");
            foreach (var insertedRecord in InsertedRecords)
            {
                if (!TestDynHash.TryFind(insertedRecord, out var dataRecord))
                {
                    Console.WriteLine("{0} data record not found", insertedRecord.RegisterNumber);
                    return false;
                }
            }

            Console.WriteLine("Find test finished successfully");
            return true;
        }

        public void DoDhTesting()
        {
            //var n = 3000;
            //AddData(n);
            var result = RandomRemoveInsertTest(500) && TestFind(); // TestFind() && TestDeleteAll(n) && TestFind() && 
            Console.WriteLine(result ? "tests were successful" : "tests failed");
            Console.ReadLine();
        }
    }
}
