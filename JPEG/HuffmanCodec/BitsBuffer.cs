using System.Collections.Generic;

namespace JPEG.HuffmanCodec
{
    public class BitsBuffer
    {
        public readonly List<byte> buffer = new List<byte>();
        public readonly BitsWithLength unfinishedBits = new BitsWithLength();

        public void Add(BitsWithLength bitsWithLength, int bitsCount = 0)
        {
            if(bitsCount == 0)
                bitsCount = bitsWithLength.BitsCount;
            var bits = bitsWithLength.Bits;

            var neededBits = 8 - unfinishedBits.BitsCount;
            while (bitsCount >= neededBits)
            {
                bitsCount -= neededBits;
                buffer.Add((byte) ((unfinishedBits.Bits << neededBits) + (bits >> bitsCount)));

                bits = bits & ((1 << bitsCount) - 1);

                unfinishedBits.Bits = 0;
                unfinishedBits.BitsCount = 0;

                neededBits = 8;
            }
            unfinishedBits.BitsCount += bitsCount;
            unfinishedBits.Bits = (unfinishedBits.Bits << bitsCount) + bits;
        }

        public byte[] ToArray(out long bitsCount)
        {
            bitsCount = buffer.Count*8L + unfinishedBits.BitsCount;
            var result = new byte[bitsCount/8 + (bitsCount%8 > 0 ? 1 : 0)];
            buffer.CopyTo(result);
            if (unfinishedBits.BitsCount > 0)
                result[buffer.Count] = (byte) (unfinishedBits.Bits << (8 - unfinishedBits.BitsCount));
            return result;
        }
    }
}