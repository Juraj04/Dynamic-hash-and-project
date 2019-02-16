using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DynamicHash;

namespace StructureTester
{
    class RandomOperationsTester
    {
        public DynHash<TestRecord> TestDynHash { get; set; }
        public List<TestRecord> InsertedRecords { get; set; }
        public int  MaxItems { get; set; }

        public RandomOperationsTester()
        {
            var filePaths = new FilePaths()
            {
                FileBlockData = "C:/Users/janik/source/repos/US_SemestralnaPraca2/StructureTester/testData/bloky.txt",
                FileTreeData = "C:/Users/janik/source/repos/US_SemestralnaPraca2/StructureTester/testData/conf.txt",
                FileOverflowFile = "C:/Users/janik/source/repos/US_SemestralnaPraca2/StructureTester/testData/preplnovaci.txt"
            };
            TestDynHash = new DynHash<TestRecord>(filePaths,10,30);
            InsertedRecords = new List<TestRecord>();
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
                        var num = randomInsertNumber.Next();
                        var record = new TestRecord(num);
                        if (TestDynHash.Add(record))
                        {
                            InsertedRecords.Add(new TestRecord(num));
                        }

                    }
                    else
                    {
                        var index = randomDeleteNumber.Next(0, InsertedRecords.Count - 1);
                        var value = InsertedRecords[index];
                        InsertedRecords.Remove(value);

                        if (!TestDynHash.TryRemove(new TestRecord(value.Number), out var result))
                        {
                            Console.WriteLine("Did not remove value {0}", value.Number);

                            if (TestDynHash.TryFind(new TestRecord(value.Number), out var value2))
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
            InsertedRecords.Capacity = count;
            for (int i = 0; i < count; i++)
            {
                var record = new TestRecord(i);
                if (TestDynHash.Add(record))
                {
                    InsertedRecords.Add(new TestRecord(i));
                }
            }

            Console.WriteLine("Insertion test finished");
        }

        public bool TestFind()
        {
            Console.WriteLine("Starting find test");
            foreach (var insertedRecord in InsertedRecords)
            {
                if (!TestDynHash.TryFind(insertedRecord, out var dataRecord))
                {
                    Console.WriteLine("{0} data record not found", insertedRecord.Number);
                    return false;
                }
            }

            Console.WriteLine("Find test finished successfully");
            return true;
        }

        public bool TestDeleteAll(int count)
        {
            Console.WriteLine("Starting delete test");
            var rand = new Random();
            var data = InsertedRecords.OrderBy(record => rand.Next());
            var queue = new Queue<TestRecord>();
            foreach (var testRecord in data)
            {
                queue.Enqueue(testRecord);
            }

            var help = 0;
            while (help < count)
            {
                if(!TestDynHash.TryRemove(queue.Dequeue(), out var dataRecord))
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

        public bool ReverseFindTest()
        {
            Console.WriteLine("Starting reverse find test");
            foreach (var insertedRecord in InsertedRecords)
            {
                if (TestDynHash.TryFind(insertedRecord, out var dataRecord))
                {
                    Console.WriteLine("{0} data record found", insertedRecord.Number);
                    return false;
                }
            }

            Console.WriteLine("Reverse Find test finished successfully");
            return true;
        }

        public void DoDhTesting()
        {
            var n = 5000;
            AddData(n);
            var result = TestFind() && TestDeleteAll(n) && TestFind() && ReverseFindTest();// && RandomRemoveInsertTest(2000) && TestFind();
            Console.WriteLine(result ? "tests were successful" : "tests failed");
            Console.ReadLine();
        }
    }
}
