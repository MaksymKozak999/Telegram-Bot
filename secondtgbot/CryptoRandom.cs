using System;
using System.Security.Cryptography;

namespace secondtgbot
{
    public static class CryptoRandom
    {
        private static readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();

        // Returns a cryptographically secure random integer in range [0, maxExclusive).
        // Works in all .NET versions (.NET Framework & .NET Core/5+).
        
        public static int Next(int maxExclusive)
        {
            if (maxExclusive <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxExclusive));

            byte[] bytes = new byte[4];
            _rng.GetBytes(bytes);

            // Mask off the sign bit to ensure non-negative values
            
            uint value = BitConverter.ToUInt32(bytes, 0) & 0x7FFFFFFF;

            return (int)(value % (uint)maxExclusive);
        }

        public static int Next(int minValue, int maxValueInclusive)
        {
            
        if (minValue > maxValueInclusive)
                throw new ArgumentException("minValue cannot be greater than maxValueInclusive");

            // Handle edge case where min == max
            if (minValue == maxValueInclusive)
                return minValue;

            // Calculate total count of numbers in range safely against overflow
            long range = (long)maxValueInclusive - minValue + 1;

            byte[] bytes = new byte[8];
            _rng.GetBytes(bytes);

            ulong value = BitConverter.ToUInt64(bytes, 0) & 0x7FFFFFFFFFFFFFFF;
            return (int)(minValue + (long)(value % (ulong)range));
        }
    }
}
