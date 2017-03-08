using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JPEG
{
    public static class BitmapHelper
    {
        public static double[,] ToGrayscaleMatrix(this Bitmap bmp)
        {
            var result = new double[bmp.Height, bmp.Width];

            var bitmapData1 = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly,
                bmp.PixelFormat);
            var pixelSize = Image.GetPixelFormatSize(bmp.PixelFormat) / 8;

            int i, j;
            unsafe
            {
                byte* imagePointer1 = (byte*)bitmapData1.Scan0;

                for (j = 0; j < bitmapData1.Height; j++)
                {
                    for (i = 0; i < bitmapData1.Width; i++)
                    {
                        result[j, i] = ((imagePointer1[0] + imagePointer1[1] + imagePointer1[2]) / 3.0);
                        imagePointer1 += pixelSize;
                    }
                    imagePointer1 += bitmapData1.Stride - (bitmapData1.Width * pixelSize);
                }
            }
            bmp.UnlockBits(bitmapData1);
            return result;
        }

        public static Bitmap GrayscaleMatrixToBitmap(this double[,] grayscaleMatrix)
        {
            var result = new Bitmap(grayscaleMatrix.GetLength(1), grayscaleMatrix.GetLength(0), PixelFormat.Format24bppRgb);
            BitmapData bitmapData1 = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

            int i, j;
            unsafe
            {
                byte* imagePointer1 = (byte*)bitmapData1.Scan0;
                for (j = 0; j < bitmapData1.Height; j++)
                {
                    for (i = 0; i < bitmapData1.Width; i++)
                    {
                        var componentValue = (int)grayscaleMatrix[j, i];
                        if (componentValue > byte.MaxValue)
                            componentValue = byte.MaxValue;
                        else if (componentValue < 0)
                            componentValue = 0;

                        imagePointer1[0] = (byte)componentValue;
                        imagePointer1[1] = (byte)componentValue;
                        imagePointer1[2] = (byte)componentValue;
                        imagePointer1 += 3;
                    }
                    imagePointer1 += (bitmapData1.Stride - (bitmapData1.Width * 3));
                }
            }
            result.UnlockBits(bitmapData1);

            return result;
        }
    }
}
