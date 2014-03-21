using System;
using System.Linq;

namespace Flint.Core
{
    internal static class Crc32
    {
        private const uint CRC_POLY = 0x04C11DB7;

        public static uint Calculate( byte[] buffer )
        {
            if (buffer == null) throw new ArgumentNullException("buffer");
            return ProcessBuffer(buffer);
        }

        private static uint ProcessBuffer( byte[] buffer, uint c = 0xFFFFFFFF )
        {
            int wordCount = buffer.Length/4;
            if (wordCount%4 != 0)
                wordCount++;
            return Enumerable.Range(0, wordCount - 1).Aggregate(c, ( current, i ) =>
                                                                   ProcessWord(buffer.Skip(i*4).Take(4).ToArray(),
                                                                               current));
        }

        private static uint ProcessWord( byte[] word, uint crc )
        {
            if (word.Length < 4)
                word = PadArray(word, 4);

            uint d = BitConverter.ToUInt32(word, 0);
            crc ^= d;

            foreach (int i in Enumerable.Range(0, 32))
            {
                if (( crc & 0x80000000 ) != 0)
                    crc = ( crc << 1 ) ^ CRC_POLY;
                else
                    crc = ( crc << 1 );
            }
            return crc;
        }

        private static T[] PadArray<T>( T[] array, int totalLength )
        {
            if (array == null) throw new ArgumentNullException("array");

            if (array.Length < totalLength)
            {
                Array.Reverse(array);
                Array.Resize(ref array, totalLength);
                Array.Reverse(array);
            }
            return array;
        }
    }
}