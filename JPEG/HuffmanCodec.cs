using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace JPEG
{
    internal class HuffmanNode
    {
        public byte? LeafLabel { get; set; }
        public int Frequency { get; set; }
        public HuffmanNode Left { get; set; }
        public HuffmanNode Right { get; set; }
    }

    internal class BitsWithLength
    {
        public int Bits { get; set; }
        public int BitsCount { get; set; }

        public class Comparer : IEqualityComparer<BitsWithLength>
        {
            public bool Equals(BitsWithLength x, BitsWithLength y)
            {
                if (x == y) return true;
                if (x == null || y == null)
                    return false;
                return x.BitsCount == y.BitsCount && x.Bits == y.Bits;
            }

            public int GetHashCode(BitsWithLength obj)
            {
                if (obj == null)
                    return 0;
                return obj.Bits ^ obj.BitsCount;
            }
        }
    }

    internal class BitsBuffer
    {
        private readonly List<byte> buffer = new List<byte>();
        private readonly BitsWithLength unfinishedBits = new BitsWithLength();

        public void Add(BitsWithLength bitsWithLength)
        {
            var bitsCount = bitsWithLength.BitsCount;
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

    internal class HuffmanCodec
    {
        private static Options codecOptions;

        public static byte[] Encode(byte[] data, out Dictionary<BitsWithLength, byte> decodeTable, out long bitsCount,
            Options options)
        {
            codecOptions = options;
            ThreadPool.SetMaxThreads(options.Threads, options.Threads);
            var frequences = CalcFrequences(data);

            var root = BuildHuffmanTree(frequences);

            var encodeTable = new BitsWithLength[byte.MaxValue + 1];
            FillEncodeTable(root, encodeTable);

            var bitsBuffer = new BitsBuffer();

            foreach (var b in data)
                bitsBuffer.Add(encodeTable[b]);

            decodeTable = CreateDecodeTable(encodeTable);

            return bitsBuffer.ToArray(out bitsCount);
        }

        public static byte[] Decode(byte[] encodedData, Dictionary<BitsWithLength, byte> decodeTable, long bitsCount)
        {
            var result = new List<byte>();

            var sample = new BitsWithLength {Bits = 0, BitsCount = 0};
            for (var byteNum = 0; byteNum < encodedData.Length; byteNum++)
            {
                var b = encodedData[byteNum];
                for (var bitNum = 0; bitNum < 8 && byteNum*8 + bitNum < bitsCount; bitNum++)
                {
                    sample.Bits = (sample.Bits << 1) + ((b & (1 << (8 - bitNum - 1))) != 0 ? 1 : 0);
                    sample.BitsCount++;

                    byte decodedByte;
                    if (decodeTable.TryGetValue(sample, out decodedByte))
                    {
                        result.Add(decodedByte);
                        sample.BitsCount = 0;
                        sample.Bits = 0;
                    }
                }
            }
            return result.ToArray();
        }

        private static Dictionary<BitsWithLength, byte> CreateDecodeTable(BitsWithLength[] encodeTable)
        {
            var result = new Dictionary<BitsWithLength, byte>(new BitsWithLength.Comparer());
            for (var b = 0; b < encodeTable.Length; b++)
            {
                var bitsWithLength = encodeTable[b];
                if (bitsWithLength == null)
                    continue;
                result[bitsWithLength] = (byte) b;
            }
            return result;
        }

        private static void FillEncodeTable(HuffmanNode node, BitsWithLength[] encodeSubstitutionTable,
            int bitvector = 0, int depth = 0)
        {
            if (node.LeafLabel != null)
                encodeSubstitutionTable[node.LeafLabel.Value] = new BitsWithLength {Bits = bitvector, BitsCount = depth};
            else
            {
                if (node.Left != null)
                {
                    ThreadPool.QueueUserWorkItem(state =>
                    {
                        FillEncodeTable(node.Left, encodeSubstitutionTable, (bitvector << 1) + 1, depth + 1);
                    });
                    FillEncodeTable(node.Left, encodeSubstitutionTable, (bitvector << 1) + 1, depth + 1);
                    FillEncodeTable(node.Right, encodeSubstitutionTable, (bitvector << 1) + 0, depth + 1);
                }
            }
        }

        private static HuffmanNode BuildHuffmanTree(Dictionary<byte, int> frequences)
        {
            var nodes = new HashSet<HuffmanNode>(
                frequences.Keys
                .AsParallel()
                .Select(b => new HuffmanNode
                {
                    Frequency = frequences[b],
                    LeafLabel = b
                }));
            while (nodes.Count > 1)
            {
                var firstMin = GetMin(nodes);
                nodes.Remove(firstMin);
                var secondMin = GetMin(nodes);
                nodes.Remove(secondMin);
                nodes.Add(new HuffmanNode
                {
                    Frequency = firstMin.Frequency + secondMin.Frequency,
                    Left = secondMin,
                    Right = firstMin
                });
            }
            return nodes.First();
        }

        private static HuffmanNode GetMin(IEnumerable<HuffmanNode> nodes) =>
            nodes.AsParallel()
                .WithDegreeOfParallelism(codecOptions.Threads)
                .MinOrDefault(node => node.Frequency);

        private static Dictionary<byte, int> CalcFrequences(IEnumerable<byte> data) =>
            data
                .AsParallel()
                .WithDegreeOfParallelism(codecOptions.Threads)
                .GroupBy(b => b)
                .ToDictionary(g => g.Key, g => g.Count());
    }
}