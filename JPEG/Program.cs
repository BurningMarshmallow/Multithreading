using System;
using System.Diagnostics;
using System.IO;
using CommandLine;
using JPEG.Transformation;


namespace JPEG
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var options = new Options();
            var isParsed = Parser.Default.ParseArguments(args, options);

            if (isParsed)
            {
                try
                {
                    CheckOptions(options);
                    var sw = Stopwatch.StartNew();
                    ImageConverter.Compress(options.FileToCompress, options.ThreadsCount, options.Quality);

                    ImageConverter.Decompress(options.FileToDecompress, options.ThreadsCount, options.Quality);
                    Console.WriteLine(sw.Elapsed.TotalMilliseconds);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
            else
            {
                Console.WriteLine(options.GetUsage());
            }
        }


        private static void CheckOptions(Options options)
        {
            if (options.Quality < 1 || options.Quality > 99)
                throw new ArgumentException("Quality must be in [1,99] interval");

            if (options.ThreadsCount < 1)
                throw new ArgumentException("Number of threads must be > 0");

            var filesToCheck = new[] { options.FileToCompress, options.FileToDecompress };
            foreach (var filename in filesToCheck)
                if (filename != null && !File.Exists(filename))
                    throw new ArgumentException($"{filename} does not exist");
        }
    }
}
