using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace JPEG.Tests
{
    public class CalculateFrequencesTests
    {
        [TestCase(1000, 50)]
        [TestCase(10000, 50)]
        [TestCase(100000, 50)]
        [TestCase(1000000, 50)]
        public void СorrectnessTest(int count, int threadsCount)
        {
            var data = Enumerable.Range(0, count).Select(x => (byte)(x % 256)).ToList();
            var expected = CalcFrequences(data, threadsCount);

            var parallel = CalcFrequencesParallel(data, threadsCount);

            parallel.ShouldBeEquivalentTo(expected, x => x.WithStrictOrdering());
        }

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

        [TestCase(1000000, 4)]
        [TestCase(1000000, 10)]
        [TestCase(1000000, 25)]
        [TestCase(1000000, 50)]
        public void PerformanceTest(int count, int threadsCount)
        {
            var unparallel = MeasurePerformance(CalcFrequences, count, threadsCount);
            Console.WriteLine("CalcFrequences: {0}", unparallel);

            var parallel = MeasurePerformance(CalcFrequencesParallel, count, threadsCount);
            Console.WriteLine("CalcFrequencesParallel: {0}", parallel);

            unparallel.Should().BeGreaterThan(parallel);
        }

        private static double MeasurePerformance(Func<IEnumerable<byte>, int, int[]> func,
            int count, int degreeOfParallelism)
        {
            var data = Enumerable.Range(0, count).Select(x => (byte) (x % 256));

            var sw = Stopwatch.StartNew();
            func(data, degreeOfParallelism);
            return sw.Elapsed.TotalMilliseconds;
        }

        private static int[] CalcFrequences(IEnumerable<byte> data, int degreeOfParallelism)
        {
            var result = new int[byte.MaxValue + 1];
            foreach (var b in data)
                result[b]++;
            return result;
        }

        private static int[] CalcFrequencesParallel(IEnumerable<byte> data, int degreeOfParallelism)
        {
            var result = new int[byte.MaxValue + 1];

            Parallel.ForEach(data, new ParallelOptions { MaxDegreeOfParallelism = degreeOfParallelism },
                x => Interlocked.Add(ref result[x], 1));
            return result;
        }
    }
}
