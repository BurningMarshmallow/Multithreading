using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading;

namespace JPEG
{
	class Program
	{
	    private const int CompressionQuality = 70;

	    private static void Main(string[] args)
		{
			try
			{
				const string fileName = @"..\..\sample.bmp";
				var compressedFileName = fileName + ".compressed." + CompressionQuality;
				var uncompressedFileName = fileName + ".uncompressed." + CompressionQuality + ".bmp";

				var bmp = (Bitmap)Image.FromFile(fileName);

				if(bmp.Width % DCTSize != 0 || bmp.Height % DCTSize != 0)
					throw new Exception($"Image width and height must be multiple of {DCTSize}");

				var grayscaleMatrix = BitmapToGrayscaleMatrix(bmp);

				var compressedImage = Compress(grayscaleMatrix, CompressionQuality);
				compressedImage.Save(compressedFileName);

				compressedImage = CompressedImage.Load(compressedFileName);
				var uncompressedImage = Uncompress(compressedImage);
				var grayscaleBmp = GrayscaleMatrixToBitmap(uncompressedImage);
				grayscaleBmp.Save(uncompressedFileName, ImageFormat.Bmp);
			}
			catch(Exception e)
			{
				Console.WriteLine(e);
			}
		}

	    private static double[,] BitmapToGrayscaleMatrix(Bitmap bmp)
		{
			var result = new double[bmp.Height, bmp.Width];

			var bitmapData1 = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, bmp.PixelFormat);
			var pixelSize = Image.GetPixelFormatSize(bmp.PixelFormat) / 8;

		    unsafe
			{
				var imagePointer1 = (byte*)bitmapData1.Scan0;

			    int j;
			    for(j = 0; j < bitmapData1.Height; j++)
				{
				    int i;
				    for(i = 0; i < bitmapData1.Width; i++)
					{
						result[j, i] = (imagePointer1[0] + imagePointer1[1] + imagePointer1[2]) / 3.0;
						imagePointer1 += pixelSize;
					}
					imagePointer1 += bitmapData1.Stride - bitmapData1.Width * pixelSize;
				}
			}
			bmp.UnlockBits(bitmapData1);
			return result;
		}

		static Bitmap GrayscaleMatrixToBitmap(double[,] grayscaleMatrix)
		{
			var result = new Bitmap(grayscaleMatrix.GetLength(1), grayscaleMatrix.GetLength(0), PixelFormat.Format24bppRgb);
			var bitmapData1 = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

			int i, j;
			unsafe
			{
				var imagePointer1 = (byte*)bitmapData1.Scan0;
				for(j = 0; j < bitmapData1.Height; j++)
				{
					for(i = 0; i < bitmapData1.Width; i++)
					{
						var componentValue = (int)grayscaleMatrix[j, i];
						if(componentValue > byte.MaxValue)
							componentValue = byte.MaxValue;
						else if(componentValue < 0)
							componentValue = 0;

						imagePointer1[0] = (byte) componentValue;
						imagePointer1[1] = (byte) componentValue;
						imagePointer1[2] = (byte) componentValue;
						imagePointer1 += 3;
					}
					imagePointer1 += (bitmapData1.Stride - (bitmapData1.Width * 3));
				}
			}
			result.UnlockBits(bitmapData1);

			return result;
		}

		private static CompressedImage Compress(double[,] channelPixels, int quality = 50)
		{
			var height = channelPixels.GetLength(0);
			var width = channelPixels.GetLength(1);

			var allQuantizedBytes = new List<byte>(height * width);

			for(var y = 0; y < height; y += DCTSize)
			{
				for(var x = 0; x < width; x += DCTSize)
				{
					var subMatrix = GetSubMatrix(channelPixels, y, DCTSize, x, DCTSize);
					ShiftMatrixValues(subMatrix, -128);
					var channelFreqs = DCT.DCT2D(subMatrix);
					var quantizedFreqs = Quantize(channelFreqs, quality);
					var quantizedBytes = ZigZagScan(quantizedFreqs);
					allQuantizedBytes.AddRange(quantizedBytes);
				}
			}

			long bitsCount;
			Dictionary<BitsWithLength, byte> decodeTable;
			var compressedBytes = HuffmanCodec.Encode(allQuantizedBytes, out decodeTable, out bitsCount);

			return new CompressedImage {Quality = quality, CompressedBytes = compressedBytes, BitsCount = bitsCount, DecodeTable = decodeTable, Height = height, Width = width};
		}

		private static double[,] Uncompress(CompressedImage image)
		{
			var allQuantizedBytes = HuffmanCodec.Decode(image.CompressedBytes, image.DecodeTable, image.BitsCount);

			var result = new double[image.Height, image.Width];

			var freqNum = 0;
			for(var y = 0; y < image.Height; y += DCTSize)
			{
				for(var x = 0; x < image.Width; x += DCTSize)
				{
					var quantizedBytes = new byte[DCTSize * DCTSize];
					Array.Copy(allQuantizedBytes, y*image.Width + x*DCTSize, quantizedBytes, 0, quantizedBytes.Length);
					var quantizedFreqs = ZigZagUnScan(quantizedBytes);
					var channelFreqs = DeQuantize(quantizedFreqs, image.Quality);
					var subMatrix = DCT.IDCT2D(channelFreqs);
					ShiftMatrixValues(subMatrix, 128);
					SetSubmatrix(result, subMatrix, y, x);
				}
			}
			return result;
		}

		private static void ShiftMatrixValues(double[,] subMatrix, int shiftValue)
		{
			for(var y = 0; y < subMatrix.GetLength(0); y++)
			{
				for(var x = 0; x < subMatrix.GetLength(1); x++)
				{
					subMatrix[y, x] = subMatrix[y, x] + shiftValue;
				}
			}
		}

		private static void SetSubmatrix(double[,] destination, double[,] source, int yOffset, int xOffset)
		{
			for(var y = 0; y < source.GetLength(0); y++)
			{
				for(var x = 0; x < source.GetLength(1); x++)
				{
					destination[yOffset + y, xOffset + x] = source[y, x];
				}
			}
		}

		private static T[,] GetSubMatrix<T>(T[,] array, int yOffset, int yLength, int xOffset, int xLength)
		{
			var result = new T[DCTSize, DCTSize];
			for(var j = 0; j < yLength; j++)
			{
				for(var i = 0; i < xLength; i++)
				{
					result[j, i] = array[yOffset + j, xOffset + i];
				}
			}
			return result;
		}

		private static byte[] ZigZagScan(byte[,] channelFreqs)
		{
			return new[]
			{
				channelFreqs[0, 0], channelFreqs[0, 1], channelFreqs[1, 0], channelFreqs[2, 0], channelFreqs[1, 1], channelFreqs[0, 2], channelFreqs[0, 3], channelFreqs[1, 2],
				channelFreqs[2, 1], channelFreqs[3, 0], channelFreqs[4, 0], channelFreqs[3, 1], channelFreqs[2, 2], channelFreqs[1, 3],  channelFreqs[0, 4], channelFreqs[0, 5],
				channelFreqs[1, 4], channelFreqs[2, 3], channelFreqs[3, 2], channelFreqs[4, 1], channelFreqs[5, 0], channelFreqs[6, 0], channelFreqs[5, 1], channelFreqs[4, 2],
				channelFreqs[3, 3], channelFreqs[2, 4], channelFreqs[1, 5],  channelFreqs[0, 6], channelFreqs[0, 7], channelFreqs[1, 6], channelFreqs[2, 5], channelFreqs[3, 4],
				channelFreqs[4, 3], channelFreqs[5, 2], channelFreqs[6, 1], channelFreqs[7, 0], channelFreqs[7, 1], channelFreqs[6, 2], channelFreqs[5, 3], channelFreqs[4, 4],
				channelFreqs[3, 5], channelFreqs[2, 6], channelFreqs[1, 7], channelFreqs[2, 7], channelFreqs[3, 6], channelFreqs[4, 5], channelFreqs[5, 4], channelFreqs[6, 3],
				channelFreqs[7, 2], channelFreqs[7, 3], channelFreqs[6, 4], channelFreqs[5, 5], channelFreqs[4, 6], channelFreqs[3, 7], channelFreqs[4, 7], channelFreqs[5, 6],
				channelFreqs[6, 5], channelFreqs[7, 4], channelFreqs[7, 5], channelFreqs[6, 6], channelFreqs[5, 7], channelFreqs[6, 7], channelFreqs[7, 6], channelFreqs[7, 7]
			};
		}

		private static byte[,] ZigZagUnScan(byte[] quantizedBytes)
		{
			return new[,]
			{
				{ quantizedBytes[0], quantizedBytes[1], quantizedBytes[5], quantizedBytes[6], quantizedBytes[14], quantizedBytes[15], quantizedBytes[27], quantizedBytes[28] },
				{ quantizedBytes[2], quantizedBytes[4], quantizedBytes[7], quantizedBytes[13], quantizedBytes[16], quantizedBytes[26], quantizedBytes[29], quantizedBytes[42] },
				{ quantizedBytes[3], quantizedBytes[8], quantizedBytes[12], quantizedBytes[17], quantizedBytes[25], quantizedBytes[30], quantizedBytes[41], quantizedBytes[43] },
				{ quantizedBytes[9], quantizedBytes[11], quantizedBytes[18], quantizedBytes[24], quantizedBytes[31], quantizedBytes[40], quantizedBytes[44], quantizedBytes[53] },
				{ quantizedBytes[10], quantizedBytes[19], quantizedBytes[23], quantizedBytes[32], quantizedBytes[39], quantizedBytes[45], quantizedBytes[52], quantizedBytes[54] },
				{ quantizedBytes[20], quantizedBytes[22], quantizedBytes[33], quantizedBytes[38], quantizedBytes[46], quantizedBytes[51], quantizedBytes[55], quantizedBytes[60] },
				{ quantizedBytes[21], quantizedBytes[34], quantizedBytes[37], quantizedBytes[47], quantizedBytes[50], quantizedBytes[56], quantizedBytes[59], quantizedBytes[61] },
				{ quantizedBytes[35], quantizedBytes[36], quantizedBytes[48], quantizedBytes[49], quantizedBytes[57], quantizedBytes[58], quantizedBytes[62], quantizedBytes[63] }
			};
		}

		private static byte[,] Quantize(double[,] channelFreqs, int quality)
		{
			var result = new byte[channelFreqs.GetLength(0), channelFreqs.GetLength(1)];

			var quantizationMatrix = GetQuantizationMatrix(quality);
			for(var y = 0; y < channelFreqs.GetLength(0); y++)
			{
				for(var x = 0; x < channelFreqs.GetLength(1); x++)
				{
					result[y, x] = (byte)(channelFreqs[y, x] / quantizationMatrix[y, x]);
				}
			}

			return result;
		}

		private static double[,] DeQuantize(byte[,] quantizedBytes, int quality)
		{
			var result = new double[quantizedBytes.GetLength(0), quantizedBytes.GetLength(1)];
			var quantizationMatrix = GetQuantizationMatrix(quality);

			for(var y = 0; y < quantizedBytes.GetLength(0); y++)
			{
				for(var x = 0; x < quantizedBytes.GetLength(1); x++)
				{
					result[y, x] = ((sbyte)quantizedBytes[y, x]) * quantizationMatrix[y, x];//NOTE cast to sbyte not to loose negative numbers
				}
			}

			return result;
		}

		private static int[,] GetQuantizationMatrix(int quality)
		{
			if(quality < 1 || quality > 99)
				throw new ArgumentException("quality must be in [1,99] interval");

			var multiplier = quality < 50 ? 5000 / quality : 200 - 2 * quality;

			var result = new[,]
			{
				{16, 11, 10, 16, 24, 40, 51, 61},
				{12, 12, 14, 19, 26, 58, 60, 55},
				{14, 13, 16, 24, 40, 57, 69, 56},
				{14, 17, 22, 29, 51, 87, 80, 62},
				{18, 22, 37, 56, 68, 109, 103, 77},
				{24, 35, 55, 64, 81, 104, 113, 92},
				{49, 64, 78, 87, 103, 121, 120, 101},
				{72, 92, 95, 98, 112, 100, 103, 99}
			};

			for(var y = 0; y < result.GetLength(0); y++)
			{
				for(var x = 0; x < result.GetLength(1); x++)
				{
					result[y, x] = (multiplier * result[y, x] + 50) / 100;
				}
			}
			return result;
		}

		const int DCTSize = 8;
	}
}
