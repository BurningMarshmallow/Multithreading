using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Patterns
{
    [TestFixture]
    public class ParallelForTests
    {
        private const int N = 64*1024*1024;
        private readonly int[] data = new Randomizer().GetInts(int.MinValue, int.MaxValue, N);
        private long expectedSum, actualSum;

        [TestFixtureSetUp]
        public void SetUpFixture()
        {
            foreach (var num in data)
                expectedSum += num;
        }

        [SetUp]
        public void SetUp()
        {
            actualSum = 0;
        }

        [Test]
        public void Ordered()
        {
            for (int i = 0; i < data.Length; i++)
                actualSum += data[i];

            Assert.AreEqual(expectedSum, actualSum);
        }

        [Test]
        public void ParallelFor()
        {
            Parallel.For(0, N, i =>
                               {
                                   Interlocked.Add(ref actualSum, data[i]);
                               });

            Assert.AreEqual(expectedSum, actualSum);
        }

        [Test]
        public void ParallelForeach()
        {
            Parallel.ForEach(data, element =>
                               {
                                   Interlocked.Add(ref actualSum, element);
                               });

            Assert.AreEqual(expectedSum, actualSum);
        }

        [Test]
        public void ParallelForeachWithPartitioner()
        {
            var processorCount = Environment.ProcessorCount;
            var partitioner = Partitioner.Create(0, N, N / processorCount);

            Parallel.ForEach(partitioner, partition =>
                                          {
                                              long localSum = 0;
                                              for (int i = partition.Item1; i < partition.Item2; i++)
                                                  localSum += data[i];
                                              Interlocked.Add(ref actualSum, localSum);
                                          });

            Assert.AreEqual(expectedSum, actualSum);
        }

        [Test]
        public void ParallelInvoke()
        {
            var processorCount = Environment.ProcessorCount;
            var partitionSize = N/processorCount;

            var actions = new Action[processorCount];
            for (int i = 0; i < actions.Length; i++)
            {
                var iLocal = i;
                actions[i] = () =>
                             {
                                 long localSum = 0;
                                 for (int j = iLocal*partitionSize; j < (iLocal+1) * partitionSize; j++)
                                     localSum += data[j];
                                 Interlocked.Add(ref actualSum, localSum);
                             };
            }

            Parallel.Invoke(actions);

            Assert.AreEqual(expectedSum, actualSum);
        }
    }
}