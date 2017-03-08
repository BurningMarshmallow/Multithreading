using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JPEG
{
    public static class DCTHelper
    {
        public static CompressedImage CompressWithDCT(this double[,] channelPixels, int DCTSize, int compressionLevel = 4)
        {
            var frequencesPerBlock = -1;

            var height = channelPixels.GetLength(0);
            var width = channelPixels.GetLength(1);

            var result = new List<double>();

            for (int y = 0; y < height; y += DCTSize)
            {
                for (int x = 0; x < width; x += DCTSize)
                {
                    var subMatrix = channelPixels.GetSubMatrix(y, DCTSize, x, DCTSize, DCTSize);
                    subMatrix.ShiftMatrixValues(-128);

                    var channelFreqs = DCT.DCT2D(subMatrix);

                    frequencesPerBlock = DCTSize * DCTSize;
                    for (int i = 0; i < DCTSize; i++)
                    {
                        for (int j = 0; j < DCTSize; j++)
                        {
                            if (i + j < compressionLevel)
                            {
                                result.Add(channelFreqs[i, j]);
                                continue;
                            }
                            channelFreqs[i, j] = 0;
                            frequencesPerBlock--;
                        }
                    }
                }
            }

            return new CompressedImage { CompressionLevel = compressionLevel, FrequencesPerBlock = frequencesPerBlock, Frequences = result, Height = height, Width = width };
        }

        private static int GetFrequencesPerBlock(int DCTSize, int compressionLevel)
        {
            var frequencesPerBlock = DCTSize * DCTSize;
            for (var i = 0; i < DCTSize; i++)
                for (var j = 0; j < DCTSize; j++)
                    if (i + j >= compressionLevel)
                        frequencesPerBlock--;
            return frequencesPerBlock;
        }

        public static CompressedImage ParallelCompressWithDCT(this double[,] channelPixels, Options options)
        {
            var DCTSize = 8;
            var compressionLevel = DCTSize * options.Quality / 100;
            var height = channelPixels.GetLength(0);
            var width = channelPixels.GetLength(1);

            var frequencesPerBlock = GetFrequencesPerBlock(DCTSize, compressionLevel);
            var blocksCount = width * height / (DCTSize * DCTSize);
            var freqsCount = Enumerable.Range(1, compressionLevel).Sum();
            var bufferLength = Enumerable.Range(1, compressionLevel).Sum() * blocksCount;

            var frequences = new double[bufferLength];
            Parallel.For(0, blocksCount, new ParallelOptions { MaxDegreeOfParallelism = options.Threads }, i =>
            {
                var bufferIndex = i * freqsCount;
                var y = i / (width / DCTSize);
                var x = i % (width / DCTSize);
                var subMatrixFreqs = GetFrequencesFromSubmatrix(channelPixels, DCTSize, compressionLevel, y * DCTSize, x * DCTSize);
                for (var shift = 0; shift < subMatrixFreqs.Count; shift++)
                    frequences[bufferIndex + shift] = subMatrixFreqs[shift];
            });


            return new CompressedImage
            {
                CompressionLevel = compressionLevel,
                FrequencesPerBlock = frequencesPerBlock,
                Frequences = frequences.ToList(),
                Height = height,
                Width = width
            };
        }

        private static List<double> GetFrequencesFromSubmatrix(double[,] channelPixels,
            int DCTSize, int compressionLevel, int y, int x)
        {
            var subMatrix = channelPixels.GetSubMatrix(y, DCTSize, x, DCTSize, DCTSize);
            subMatrix.ShiftMatrixValues(-128);
            var localResult = new List<double>();
            var channelFreqs = DCT.DCT2D(subMatrix);

            for (var i = 0; i < DCTSize; i++)
            {
                for (var j = 0; j < DCTSize; j++)
                {
                    if (i + j < compressionLevel)
                    {
                        localResult.Add(channelFreqs[i, j]);
                        continue;
                    }
                    channelFreqs[i, j] = 0;
                }
            }
            return localResult;
        }

        public static double[,] UncompressWithDCT(this CompressedImage image, int DCTSize)
        {
            var result = new double[image.Height, image.Width];

            int freqNum = 0;
            for (int y = 0; y < image.Height; y += DCTSize)
            {
                for (int x = 0; x < image.Width; x += DCTSize)
                {
                    var channelFreqs = new double[DCTSize, DCTSize];
                    for (int i = 0; i < DCTSize; i++)
                    {
                        for (int j = 0; j < DCTSize; j++)
                        {
                            if (i + j < image.CompressionLevel)
                                channelFreqs[i, j] = image.Frequences[freqNum++];
                        }
                    }
                    var processedSubmatrix = DCT.IDCT2D(channelFreqs);
                    processedSubmatrix.ShiftMatrixValues(128);
                    result.SetSubmatrix(processedSubmatrix, y, x);
                }
            }
            return result;
        }


        public static double[,] ParallelUncompressWithDCT(this CompressedImage image, Options options)
        {
            var DCTSize = 8;
            var result = new double[image.Height, image.Width];
            var blocksCount = image.Width * image.Height / (DCTSize * DCTSize);
            var freqsCount = Enumerable.Range(1, image.CompressionLevel).Sum();
            Parallel.For(0, blocksCount, new ParallelOptions { MaxDegreeOfParallelism = options.Threads },
                blockIndex =>
            {
                var y = blockIndex / (image.Width / DCTSize);
                var x = blockIndex % (image.Width / DCTSize);
                var channelFreqs = new double[DCTSize, DCTSize];
                var freqNum = blockIndex * freqsCount;

                for (var i = 0; i < DCTSize; i++)
                {
                    for (var j = 0; j < DCTSize; j++)
                    {
                        if (i + j < image.CompressionLevel)
                            channelFreqs[i, j] = image.Frequences[freqNum++];
                    }
                }
                var processedSubmatrix = DCT.IDCT2D(channelFreqs);
                processedSubmatrix.ShiftMatrixValues(128);
                result.SetSubmatrix(processedSubmatrix, y * DCTSize, x * DCTSize);
            });
            return result;
        }
    }
}
