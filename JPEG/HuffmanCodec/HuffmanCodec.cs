using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JPEG.Extensions;

namespace JPEG.HuffmanCodec
{ 
    public class HuffmanCodec
    {
        public static byte[] EncodeSequential(byte[] data, out ConcurrentDictionary<BitsWithLength, byte> decodeTable,
            out long bitsCount, int degreeOfParallelism)
        {
            var frequences = CalcFrequences(data, degreeOfParallelism);

            var root = BuildHuffmanTree(frequences);

            var encodeTable = new BitsWithLength[byte.MaxValue + 1];
            FillEncodeTable(root, encodeTable);

            var bitsBuffer = new BitsBuffer();
            foreach (var b in data)
                bitsBuffer.Add(encodeTable[b]);

            decodeTable = CreateDecodeTable(encodeTable, degreeOfParallelism);

            return bitsBuffer.ToArray(out bitsCount);
        }

        private static readonly BitsBuffer globalBuffer = new BitsBuffer();
        public static byte[] Encode(byte[] data, out ConcurrentDictionary<BitsWithLength, byte> decodeTable,
            out long bitsCount, int degreeOfParallelism)
        {
            var frequences = CalcFrequences(data, degreeOfParallelism);

            var root = BuildHuffmanTree(frequences);

            var encodeTable = new BitsWithLength[byte.MaxValue + 1];
            FillEncodeTable(root, encodeTable);

            var dataSize = data.Length;
            var chunkLength = dataSize / degreeOfParallelism;

            ParallelEnumerable.Range(0, degreeOfParallelism + 1)
                .AsOrdered()
                .WithDegreeOfParallelism(degreeOfParallelism)
                .Select(
                    i =>
                    {
                        var chunk = new BitsBuffer();
                        var lower = chunkLength * i;
                        var upper = Math.Min(chunkLength * (i + 1), dataSize);
                        for (var j = lower; j < upper; j++)
                        {
                            chunk.Add(encodeTable[data[j]]);
                        }
                        return chunk;
                    })
                    .ForEach(AddRange);
            decodeTable = CreateDecodeTable(encodeTable, degreeOfParallelism);

            return globalBuffer.ToArray(out bitsCount);
        }

        private static void AddRange(BitsBuffer chunk)
        {
            foreach (var b in chunk.buffer)
                globalBuffer.Add(new BitsWithLength { Bits = b, BitsCount = 8 }, 8);
            globalBuffer.Add(chunk.unfinishedBits);
        }



        public static byte[] Decode(byte[] encodedData, ConcurrentDictionary<BitsWithLength, byte> decodeTable,
            long bitsCount, int degreeOfParallelism)
        {
            var result = new List<byte>();

            var sample = new BitsWithLength { Bits = 0, BitsCount = 0 };
            for (var byteNum = 0; byteNum < encodedData.Length; byteNum++)
            {
                var b = encodedData[byteNum];
                for (var bitNum = 0; bitNum < 8 && byteNum * 8 + bitNum < bitsCount; bitNum++)
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

        private static ConcurrentDictionary<BitsWithLength, byte> CreateDecodeTable(BitsWithLength[] encodeTable, int degreeOfParallelism)
        {
            var result = new ConcurrentDictionary<BitsWithLength, byte>(new BitsWithLength.Comparer());

            Parallel.For(0, encodeTable.Length, new ParallelOptions { MaxDegreeOfParallelism = degreeOfParallelism },
                x =>
                {
                    var bitsWithLength = encodeTable[x];
                    if (bitsWithLength == null)
                        return;

                    result[bitsWithLength] = (byte)x;
                });
            return result;
        }

        private static void FillEncodeTable(HuffmanNode node, BitsWithLength[] encodeSubstitutionTable,
            int bitvector = 0, int depth = 0)
        {
            if (node.LeafLabel != null)
                encodeSubstitutionTable[node.LeafLabel.Value] = new BitsWithLength {Bits = bitvector, BitsCount = depth};
            else
            {
                if (node.Left == null) return;
                FillEncodeTable(node.Left, encodeSubstitutionTable, (bitvector << 1) + 1, depth + 1);
                FillEncodeTable(node.Right, encodeSubstitutionTable, (bitvector << 1) + 0, depth + 1);
            }
        }

        private static HuffmanNode BuildHuffmanTree(int[] frequences)
        {
            var nodes =
                new HashSet<HuffmanNode>(
                    Enumerable.Range(0, byte.MaxValue + 1)
                        .Select(num => new HuffmanNode {Frequency = frequences[num], LeafLabel = (byte) num})
                        .Where(node => node.Frequency > 0));
            while (nodes.Count > 1)
            {
                var firstMin = nodes.MinOrDefault(node => node.Frequency);
                nodes.ExceptWith(new[] {firstMin});
                var secondMin = nodes.MinOrDefault(node => node.Frequency);
                nodes.ExceptWith(new[] {secondMin});
                nodes.Add(new HuffmanNode
                {
                    Frequency = firstMin.Frequency + secondMin.Frequency,
                    Left = secondMin,
                    Right = firstMin
                });
            }
            return nodes.First();
        }

        private static int[] CalcFrequences(IEnumerable<byte> data, int degreeOfParallelism)
        {
            var result = new int[byte.MaxValue + 1];

            Parallel.ForEach(data, new ParallelOptions { MaxDegreeOfParallelism = degreeOfParallelism },
                x => Interlocked.Add(ref result[x], 1));
            return result;
        }
    }
}








//public static byte[] Decode(byte[] encodedData, Dictionary<BitsWithLength, byte> decodeTable, Options options)
//{
//    var chunks = new List<HuffmanChunk>();

//    for (int i = 0; i < encodedData.Length;)
//    {
//        var bitsCount = BitConverter.ToInt32(encodedData, i);
//        var dataLength = bitsCount / 8 + (bitsCount % 8 == 0 ? 0 : 1);
//        chunks.Add(new HuffmanChunk(
//            encodedData.Skip(i + 4).Take(dataLength).ToArray(),
//            bitsCount));
//        i += 4 + dataLength;
//    }

//    return chunks
//        .AsParallel()
//        .AsOrdered()
//        .WithDegreeOfParallelism(options.MaxDegreeOfParallelism)
//        .SelectMany(chunk => DecodeChunk(chunk.Data, decodeTable, chunk.BitsCount))
//        .ToArray();
//}

//private static IEnumerable<byte> DecodeChunk(byte[] data, Dictionary<BitsWithLength, byte> decodeTable, int totalBitsCount)
//{
//    var result = new List<byte>();

//    var bits = (int)0;
//    var bitsCount = (int)0;
//    for (var byteNum = 0; byteNum < data.Length; byteNum++)
//    {
//        var b = data[byteNum];
//        for (var bitNum = 0; bitNum < 8 && byteNum * 8 + bitNum < totalBitsCount; bitNum++)
//        {
//            bits = (int)((bits << 1) + ((b & (1 << (8 - bitNum - 1))) != 0 ? 1 : 0));
//            bitsCount++;

//            var key = new BitsWithLength(bits, bitsCount);
//            byte decodedByte;
//            if (decodeTable.TryGetValue(key, out decodedByte))
//            {
//                result.Add(decodedByte);
//                bitsCount = 0;
//                bits = 0;
//            }
//        }
//    }

//    return result;
//}