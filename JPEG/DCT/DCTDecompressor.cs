using System.Linq;
using System.Threading.Tasks;

namespace JPEG.DCT
{
    public static class DCTDecompressor
    {
        public static double[,] UncompressWithDCT(this CompressedImage image, int DCTSize)
        {
            var result = new double[image.Height, image.Width];

            var freqNum = 0;
            for (var y = 0; y < image.Height; y += DCTSize)
            {
                for (var x = 0; x < image.Width; x += DCTSize)
                {
                    var channelFreqs = new double[DCTSize, DCTSize];
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
                    result.SetSubmatrix(processedSubmatrix, y, x);
                }
            }
            return result;
        }


        public static double[,] ParallelUncompressWithDCT(this CompressedImage image, Options options)
        {
            const int DCTSize = 8;
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
