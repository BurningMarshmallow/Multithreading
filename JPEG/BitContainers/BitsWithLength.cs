﻿using System.Collections.Generic;

namespace JPEG.BitContainers
{
    public class BitsWithLength
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
}
