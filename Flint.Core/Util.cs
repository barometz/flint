using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Flint.Core
{
    internal static class Util
    {
        /// <summary>
        ///     Convert a Unix timestamp to a DateTime object.
        /// </summary>
        /// <remarks>
        ///     This has some issues, as Pebble isn't timezone-aware and it's
        ///     unclear how either side deals with leap seconds.  For basic usage
        ///     this should be plenty, though.
        /// </remarks>
        /// <param name="timestamp"></param>
        /// <returns></returns>
        public static DateTime GetDateTimeFromTimestamp( uint timestamp )
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Unspecified).AddSeconds(timestamp);
        }

        public static int GetTimestampFromDateTime( DateTime dateTime )
        {
            return (int) ( dateTime - new DateTime(1970, 1, 1, 0, 0, 0, dateTime.Kind) ).TotalSeconds;
        }

        public static byte[] GetBytes( Stream stream )
        {
            if (stream == null) throw new ArgumentNullException("stream");
            int size = (int)(stream.Length - stream.Position);
            var rv = new byte[size];
            stream.Read(rv, 0, size);
            return rv;
        }

        public static byte[] GetBytes( string @string )
        {
            if (@string == null) throw new ArgumentNullException("string");
            if (@string.Length > byte.MaxValue)
                @string = @string.Substring(0, byte.MaxValue);
            var bytes = new byte[@string.Length + 1];
            bytes[0] = (byte) @string.Length;
            Encoding.UTF8.GetBytes(@string, 0, @string.Length, bytes, 1);
            return bytes;
        }

        public static byte[] GetBytes( int value )
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return bytes;
        }

        public static byte[] GetBytes( uint value )
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return bytes;
        }

        public static byte[] GetBytes( ushort value )
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return bytes;
        }

        public static string GetString( byte[] bytes, int index, int count )
        {
            string @string = Encoding.UTF8.GetString(bytes, index, count);
            int nullIndex = @string.IndexOf('\0');
            if (nullIndex >= 0)
                @string = @string.Substring(0, nullIndex);

            return @string;
        }

        public static uint GetUInt32( byte[] bytes, int index = 0)
        {
            byte[] copiedBytes = GetOrderedBytes(bytes, index, sizeof (uint));
            return BitConverter.ToUInt32(copiedBytes, 0);
        }

        public static ushort GetUInt16( byte[] bytes, int index = 0)
        {
            byte[] copiedBytes = GetOrderedBytes(bytes, index, sizeof (ushort));
            return BitConverter.ToUInt16(copiedBytes, 0);
        }

        public static byte[] CombineArrays( params byte[][] array )
        {
            var rv = new byte[array.Select(x => x.Length).Sum()];

            for (int i = 0, insertionPoint = 0; i < array.Length; insertionPoint += array[i].Length, i++)
                Array.Copy(array[i], 0, rv, insertionPoint, array[i].Length);
            return rv;
        }

        public static T GetEnum<T>( object value, T @default = default( T ) ) where T : struct
        {
            Type enumType = typeof (T);
            if (Enum.IsDefined(enumType, Convert.ChangeType(value, Enum.GetUnderlyingType(enumType))))
                return (T) Convert.ChangeType(value, Enum.GetUnderlyingType(enumType));
            return @default;
        }

        public static UUID GetUUID( byte[] bytes, int index )
        {
            byte[] byteArray = bytes.Skip(index).Take(UUID.SIZE).ToArray();
            if (byteArray.Length == UUID.SIZE)
                return new UUID(byteArray);
            return null;
        }

        private static byte[] GetOrderedBytes( byte[] bytes, int index, int length )
        {
            var rv = new byte[length];
            Array.Copy(bytes, index, rv, 0, length);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(rv);
            }
            return rv;
        }
    }
}