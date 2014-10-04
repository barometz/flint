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
            if (buffer.Length%4 != 0)
                wordCount++;

            uint crc = c;
            foreach (var i in Enumerable.Range(0, wordCount))
            {
                crc = ProcessWord(GetUInt32(buffer, i*4), crc);
            }
            return crc; 
        }

        private static uint GetUInt32(byte[] buffer, int start)
        {
            var size = Math.Min(buffer.Length - start, 4);
            var rv = new byte[4];
            Array.Copy(buffer, start, rv, 0, size);
            if (size < 4)
                Array.Reverse(rv, 0, size);
            return BitConverter.ToUInt32(rv, 0);
        }

        private static uint ProcessWord( uint word, uint crc )
        {
            crc ^= word;
            
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
            }
            return array;
        }
    }
}