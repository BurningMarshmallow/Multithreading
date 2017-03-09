using System;
using System.Diagnostics;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace JPEG.Tests
{
    public class IDCT2DTests
    {
        [TestCase(10)]
        [TestCase(25)]
        [TestCase(50)]
        [Repeat(100)]
        public void PerformanceTest(int threadsCount)
        {
            var unparallel = MeasurePerformance(IDCT2D, threadsCount);
            Console.WriteLine("IDCT2D: {0}", unparallel);

            var parallel = MeasurePerformance(IDCT2DParallel, threadsCount);
            Console.WriteLine("IDCT2DParallel: {0}", parallel);

            unparallel.Should().BeGreaterThan(parallel);
        }

        [TestCase(50)]
        [Repeat(100)]
        public void СorrectnessTest(int threadsCount)
        {
            var data = CreateData();
            var expected = IDCT2D(data, threadsCount);

            var parallel = IDCT2DParallel(data, threadsCount);

            parallel.ShouldBeEquivalentTo(expected, x => x.WithStrictOrdering());
        }

        private double MeasurePerformance(Func<double[,], int, double[,]> func, int degreeOfParallelism)
        {
            var data = CreateData();

            var sw = Stopwatch.StartNew();
            func(data, degreeOfParallelism);
            return sw.Elapsed.TotalMilliseconds;
        }

        private double[,] CreateData()
        {
            var rnd = new Random();
            var data = new double[16, 16];
            for (var x = 0; x < data.GetLength(0); x++)
                for (var y = 0; y < data.GetLength(1); y++)
                    data[x, y] = rnd.Next(1000, 5000);
            return data;
        }

        private static double[,] IDCT2D(double[,] coeffs, int degreeOfParallelism)
        {
            var height = coeffs.GetLength(0);
            var width = coeffs.GetLength(1);
            var output = new double[width, height];

            for (var x = 0; x < width; x++)
                for (var y = 0; y < height; y++)
                {
                    var sum = 0d;
                    for (var u = 0; u < width; u++)
                        for (var v = 0; v < height; v++)
                        {
                            var a = coeffs[u, v];
                            sum += BasisFunction(a, u, v, x, y, height, width) * Alpha(u) * Alpha(v);
                        }
                    output[x, y] = sum * Beta(height, width);
                }
            return output;
        }

        public static double[,] IDCT2DParallel(double[,] coeffs, int degreeOfParallelism)
        {
            var height = coeffs.GetLength(0);
            var width = coeffs.GetLength(1);
            var output = new double[width, height];

            Parallel.For(0, width, new ParallelOptions { MaxDegreeOfParallelism = degreeOfParallelism },
               x =>
               {
                   for (var y = 0; y < height; y++)
                   {
                       var sum = 0d;

                       for (var u = 0; u < width; u++)
                           for (var v = 0; v < height; v++)
                           {
                               var a = coeffs[u, v];
                               sum += BasisFunction(a, u, v, x, y, height, width) * Alpha(u) * Alpha(v);
                           }
                       output[x, y] = sum * Beta(height, width);
                   }
               });
            return output;
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
