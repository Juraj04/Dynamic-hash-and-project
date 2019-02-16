using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DynamicHash;

namespace StructureTester
{
    class RafTestBock
    {
        public int TestRecordValue { get; set; }
        public int IndexAddress { get; set; }

        public RafTestBock(int testRecordValue, int indexAddress)
        {
            TestRecordValue = testRecordValue;
            IndexAddress = indexAddress;
        }
    }
    class RafTester
    {
        public RandomAccessFile<TestRecord> TestRaf { get; set; }
        public List<RafTestBock> InsertedNumbers { get; set; }
        public RafTester()
        {
            var rafBlockFile = "C:/Users/janik/source/repos/US_SemestralnaPraca2/StructureTester/testData/rafBlocks.txt";
            var rafConfFile = "C:/Users/janik/source/repos/US_SemestralnaPraca2/StructureTester/testData/rafConf.txt";
            TestRaf = new RandomAccessFile<TestRecord>(rafBlockFile, rafConfFile, 10);
            InsertedNumbers = new List<RafTestBock>();
        }

        public void AddData(int count)
        {
            Console.WriteLine("Starting insertion");
            //var random = new Random();
            InsertedNumbers.Capacity = count;
            for (int i = 0; i < count; i++)
            {
                //var n = random.Next();
                var record = new TestRecord(i);
                if (TestRaf.Add(record, out var index))
                {
                    InsertedNumbers.Add(new RafTestBock(i, index));
                }
            }

            Console.WriteLine("Insertion test finished");
        }

        public bool TestFind()
        {
            Console.WriteLine("Starting find test");
            foreach (var insertedRecord in InsertedNumbers)
            {
                if (!TestRaf.TryFind(new TestRecord(insertedRecord.TestRecordValue), insertedRecord.IndexAddress, out var dataRecord))
                {
                    Console.WriteLine("{0} data record not found", insertedRecord.TestRecordValue);
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
            var data = InsertedNumbers.OrderBy(record => rand.Next());
            var queue = new Queue<RafTestBock>();
            foreach (var testRecord in data)
            {
                queue.Enqueue(testRecord);
            }

            var help = 0;
            while (help < count)
            {
                var item = queue.Dequeue();
                if (!TestRaf.TryRemove(new TestRecord(item.TestRecordValue),item.IndexAddress, out var dataRecord))
                {
                    Console.WriteLine("{0} data record not removed");
                    return false;
                }

                help++;
            }

            InsertedNumbers = queue.ToList();
            Console.WriteLine("Delete test finished successfully");
            return true;
        }

        public bool ReverseFindTest()
        {
            Console.WriteLine("Starting reverse find test");
            foreach (var insertedRecord in InsertedNumbers)
            {
                if (TestRaf.TryFind(new TestRecord(insertedRecord.TestRecordValue), insertedRecord.IndexAddress, out var dataRecord))
                {
                    Console.WriteLine("{0} data record found", insertedRecord.TestRecordValue);
                    return false;
                }
            }

            Console.WriteLine("Reverse Find test finished successfully");
            return true;
        }

        public void DoRafTesting()
        {
            AddData(5000);
            var result = TestFind() && TestDeleteAll(2500) && TestFind() && RandomRemoveInsertTest(1000) && TestFind();// && ReverseFindTest();
            Console.WriteLine(result ? "tests were successful" : "tests failed");
            Console.ReadLine();
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
            InsertedNumbers.Clear();

            while (current < rounds)
            {
                var count = randomCount.Next(50, 200);
                var insOrDel = randomInsOrDel.NextDouble();
                for (int i = 0; i < count; i++)
                {
                    if (InsertedNumbers.Count == 0)
                        insOrDel = 0.1;

                    if (insOrDel < 0.5)
                    {
                        var num = randomInsertNumber.Next();
                        var record = new TestRecord(num);
                        if (TestRaf.Add(record, out var index))
                        {
                            InsertedNumbers.Add(new RafTestBock(num,index));
                        }

                    }
                    else
                    {
                        var index = randomDeleteNumber.Next(0, InsertedNumbers.Count - 1);
                        var value = InsertedNumbers[index];
                        InsertedNumbers.Remove(value);

                        if (!TestRaf.TryRemove(new TestRecord(value.TestRecordValue),value.IndexAddress, out var result))
                        {
                            Console.WriteLine("Did not remove value {0}", value.TestRecordValue);

                            if (TestRaf.TryFind(new TestRecord(value.TestRecordValue), value.IndexAddress, out var value2))
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
    }
}
