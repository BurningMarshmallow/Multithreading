using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using JPEG.HuffmanCodec;
using NUnit.Framework;

namespace JPEG.Tests
{
    public class CreateDecodeTableTests
    {
        [TestCase(1000, 4)]
        [TestCase(1000, 10)]
        [TestCase(1000, 25)]
        [TestCase(1000, 50)]

        [TestCase(10000, 4)]
        [TestCase(10000, 10)]
        [TestCase(10000, 25)]
        [TestCase(10000, 50)]

        [TestCase(100000, 4)]
        [TestCase(100000, 10)]
        [TestCase(100000, 25)]
        [TestCase(100000, 50)]
        public void PerformanceTest(int count, int threadsCount)
        {
            var unparallel = MeasurePerformance(CreateDecodeTable, count, threadsCount);
            Console.WriteLine("CreateDecodeTable: {0}", unparallel);

            var parallel = MeasurePerformance(CreateDecodeTableParallel, count, threadsCount);
            Console.WriteLine("CreateDecodeTableConcurentDictParallel: {0}", parallel);

            unparallel.Should().BeGreaterThan(parallel);
        }

        [TestCase(1000, 50)]
        [TestCase(10000, 50)]
        [TestCase(100000, 50)]
        public void СorrectnessTest(int count, int threadsCount)
        {
            var data = Enumerable.Range(0, count)
                .Select(x => new BitsWithLength { Bits = x % 256, BitsCount = x % 256 })
                .ToArray();
            var expected = CreateDecodeTable(data, threadsCount);

            var parallel = CreateDecodeTableParallel(data, threadsCount);

            parallel.ShouldBeEquivalentTo(expected);
        }

        private double MeasurePerformance(Func<BitsWithLength[], int, IDictionary<BitsWithLength, byte>> func, int count, int degreeOfParallelism)
        {
            var data = Enumerable.Range(0, count)
                .Select(x => new BitsWithLength { Bits = x % 256, BitsCount = x % 256 })
                .ToArray();

            var sw = Stopwatch.StartNew();
            func(data, degreeOfParallelism);
            return sw.Elapsed.TotalMilliseconds;
        }

        private static Dictionary<BitsWithLength, byte> CreateDecodeTable(BitsWithLength[] encodeTable, int degreeOfParallelism)
        {
            var result = new Dictionary<BitsWithLength, byte>(new BitsWithLength.Comparer());
            for (var b = 0; b < encodeTable.Length; b++)
            {
                var bitsWithLength = encodeTable[b];
                if (bitsWithLength == null)
                    continue;

                result[bitsWithLength] = (byte)b;
            }
            return result;
        }

        private static ConcurrentDictionary<BitsWithLength, byte> CreateDecodeTableParallel(BitsWithLength[] encodeTable, int degreeOfParallelism)
        {
            var result = new ConcurrentDictionary<BitsWithLength, byte>(new BitsWithLength.Comparer());

            Parallel.For(0, encodeTable.Length, new ParallelOptions { MaxDegreeOfParallelism = degreeOfParallelism },
                x =>
                {
                    var bitsWithLength = encodeTable[x];
                    if (bitsWithLength == null)
                        return;

                    result[bitsWithLength] = (byte)x;
                });
            return result;
        }
    }
}
