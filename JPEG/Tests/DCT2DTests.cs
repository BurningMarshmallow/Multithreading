using System;
using System.Diagnostics;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace JPEG.Tests
{
    public class DCT2DTests
    {
        [TestCase(4)]
        [TestCase(10)]
        [TestCase(25)]
        [TestCase(50)]
        [Repeat(50)]
        public void PerformanceTest(int threadsCount)
        {
            var unparallel = MeasurePerformance(DCT2D, threadsCount);
            Console.WriteLine("DCT2D: {0}", unparallel);

            var parallel = MeasurePerformance(DCT2DParallel, threadsCount);
            Console.WriteLine("DCT2DParallel: {0}", parallel);

            unparallel.Should().BeGreaterThan(parallel);
        }

        [TestCase(50)]
        [Repeat(50)]
        public void СorrectnessTest(int threadsCount)
        {
            var data = CreateData();
            var expected = DCT2D(data, threadsCount);

            var parallel = DCT2DParallel(data, threadsCount);

            parallel.ShouldBeEquivalentTo(expected, x => x.WithStrictOrdering());
        }

        private double MeasurePerformance(Func<double[,], int, double[,]> func, int threadsCount)
        {
            var data = CreateData();

            var sw = Stopwatch.StartNew();
            func(data, threadsCount);
            return sw.Elapsed.TotalMilliseconds;
        }

        private double[,] CreateData()
        {
            var rnd = new Random();
            var data = new double[20, 20];
            for(var x = 0; x < data.GetLength(0); x++)
                for (var y = 0; y < data.GetLength(1); y++)
                    data[x, y] = rnd.Next(1000, 5000);
            return data;
        }

        private static double[,] DCT2D(double[,] input, int degreeOfParallelism)
        {
            var height = input.GetLength(0);
            var width = input.GetLength(1);
            var coeffs = new double[width, height];

            for (var u = 0; u < width; u++)
                for (var v = 0; v < height; v++)
                {
                    var sum = 0d;
                    for (var x = 0; x < width; x++)
                        for (var y = 0; y < height; y++)
                        {
                            var a = input[x, y];
                            sum += BasisFunction(a, u, v, x, y, height, width);
                        }
                    coeffs[u, v] = sum * Beta(height, width) * Alpha(u) * Alpha(v);
                }
            return coeffs;
        }

        private static double[,] DCT2DParallel(double[,] input, int degreeOfParallelism)
        {
            var height = input.GetLength(0);
            var width = input.GetLength(1);
            var coeffs = new double[width, height];

            Parallel.For(0, width, new ParallelOptions { MaxDegreeOfParallelism = degreeOfParallelism },
               u =>
               {
                   for (var v = 0; v < height; v++)
                   {
                       var sum = 0d;
                       for (var x = 0; x < width; x++)
                           for (var y = 0; y < height; y++)
                           {
                               var a = input[x, y];
                               sum += BasisFunction(a, u, v, x, y, height, width);
                           }
                       coeffs[u, v] = sum * Beta(height, width) * Alpha(u) * Alpha(v);
                   }
               });
            return coeffs;
        }

        public static double BasisFunction(double a, double u, double v, double x, double y, int height, int width)
        {
            var b = Math.Cos((2d * x + 1d) * u * Math.PI / (2 * width));
            var c = Math.Cos((2d * y + 1d) * v * Math.PI / (2 * height));

            return a * b * c;
        }

        private static double Alpha(int u)
        {
            if (u == 0)
                return 1 / Math.Sqrt(2);
            return 1;
        }

        private static double Beta(int height, int width)
        {
            return 1d / width + 1d / height;
        }
    }
}
