namespace JPEG
{
    public static class MatrixExtensions
    {
        public static void ShiftMatrixValues(this double[,] subMatrix, int shiftValue)
        {
            for (int y = 0; y < subMatrix.GetLength(0); y++)
            {
                for (int x = 0; x < subMatrix.GetLength(1); x++)
                {
                    subMatrix[y, x] = subMatrix[y, x] + shiftValue;
                }
            }
        }

        public static void SetSubmatrix(this double[,] destination, double[,] source, int yOffset, int xOffset)
        {
            for (int y = 0; y < source.GetLength(0); y++)
            {
                for (int x = 0; x < source.GetLength(1); x++)
                {
                    destination[yOffset + y, xOffset + x] = source[y, x];
                }
            }
        }

        public static T[,] GetSubMatrix<T>(this T[,] array, int yOffset, int yLength, int xOffset, int xLength, int DCTSize)
        {
            var result = new T[DCTSize, DCTSize];
            for (int j = 0; j < yLength; j++)
            {
                for (int i = 0; i < xLength; i++)
                {
                    result[j, i] = array[yOffset + j, xOffset + i];
                }
            }
            return result;
        }
    }
}
