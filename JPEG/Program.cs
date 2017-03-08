using System;
using System.Drawing;
using System.Drawing.Imaging;
using CommandLine;

namespace JPEG
{
	class Program
	{
		public const int DCTSize = 8;
        
		static void Main(string[] args)
		{
		    var options = new Options();
            Parser.Default.ParseArguments(args, options);
            var compressionLevel = 8;
            try
			{
				const string fileName = @"..\..\sample.bmp";
				var compressedFileName = fileName + ".compressed." + DCTSize + "." + compressionLevel;
				var uncompressedFileName = fileName + ".uncompressed." + DCTSize + "." + compressionLevel + ".bmp";
//			    var huffmanCompressedFileName = fileName + ".huffman.compressed." + DCTSize + "." + CompressionLevel;
//			    var huffmanUncompressedFileName = fileName + ".huffman.uncompressed." + DCTSize + "." + CompressionLevel;

				var bmp = (Bitmap)Image.FromFile(fileName);

				if(bmp.Width % DCTSize != 0 || bmp.Height % DCTSize != 0)
					throw new Exception($"Image width and height must be multiple of {DCTSize}");

				var grayscaleMatrix = bmp.ToGrayscaleMatrix();

                //var compressedImage = grayscaleMatrix.CompressWithDCT(DCTSize, compressionLevel);
                var compressedImage = grayscaleMatrix.ParallelCompressWithDCT(options);
                compressedImage.Save(compressedFileName);
			    Console.WriteLine("Compressed");

//                var forHuffman = File.ReadAllBytes(compressedFileName);
//			    Dictionary<BitsWithLength, byte> decodeTable;
//			    long bitsCount;
//                var encodedHuff = HuffmanCodec.Encode(forHuffman, out decodeTable, out bitsCount);
//			    File.WriteAllBytes(huffmanCompressedFileName, encodedHuff);
//			    var huffmanCompressed = File.ReadAllBytes(huffmanCompressedFileName);
//			    var decodedHuff = HuffmanCodec.Decode(huffmanCompressed, decodeTable, bitsCount);
//			    File.WriteAllBytes(huffmanUncompressedFileName, decodedHuff);

				compressedImage = CompressedImage.Load(compressedFileName, DCTSize);
				var uncompressedImage = compressedImage.ParallelUncompressWithDCT(options);

				var grayscaleBmp = uncompressedImage.GrayscaleMatrixToBitmap();
				grayscaleBmp.Save(uncompressedFileName, ImageFormat.Bmp);
                Console.WriteLine("Decompressed");
            }
            catch (Exception e)
			{
				Console.WriteLine(e);
			}
		}
	}
}
