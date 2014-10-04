using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Ionic.Zip;

namespace flint
{
    internal static class Util
    {
        /// <summary>
        /// Reads serialized struct data back into a struct, much like fread() might do in C.
        /// </summary>
        /// <param name="fs"></param>
        public static T ReadStruct<T>( Stream fs ) where T : struct
        {
            // Borrowed from http://stackoverflow.com/a/1936208 because BitConverter-ing all of this would be a pain
            var buffer = new byte[Marshal.SizeOf( typeof( T ) )];
            fs.Read( buffer, 0, buffer.Length );
            return ReadStruct<T>( buffer );
        }

        public static T ReadStruct<T>( byte[] bytes ) where T : struct
        {
            if ( bytes.Count() != Marshal.SizeOf( typeof( T ) ) )
            {
                throw new ArgumentException( "Byte array does not match size of target struct." );
            }
            T rv;
            GCHandle hdl = GCHandle.Alloc( bytes, GCHandleType.Pinned );
            try
            {
                rv = (T)Marshal.PtrToStructure( hdl.AddrOfPinnedObject(), typeof( T ) );
            }
            finally
            {
                hdl.Free();
            }
            return rv;
        }

        /// <summary> Convert a Unix timestamp to a DateTime object.
        /// </summary>
        /// <remarks>
        /// This has some issues, as Pebble isn't timezone-aware and it's 
        /// unclear how either side deals with leap seconds.  For basic usage
        /// this should be plenty, though.
        /// </remarks>
        /// <param name="timestamp"></param>
        /// <returns></returns>
        public static DateTime GetDateTimeFromTimestamp( uint timestamp )
        {
            return new DateTime( 1970, 1, 1, 0, 0, 0, DateTimeKind.Unspecified ).AddSeconds( timestamp );
        }

        public static int GetTimestampFromDateTime( DateTime dateTime )
        {
            return (int)( dateTime - new DateTime( 1970, 1, 1, 0, 0, 0, dateTime.Kind ) ).TotalSeconds;
        }

        public static byte[] GetBytes( ZipEntry zipEntry )
        {
            using ( var memoryStream = new MemoryStream() )
            {
                zipEntry.Extract( memoryStream );
                memoryStream.Position = 0;
                return memoryStream.ToArray();
            }
        }

        public static byte[] GetBytes( string @string )
        {
            if ( @string == null ) throw new ArgumentNullException( "string" );
            if ( @string.Length > byte.MaxValue )
                @string = @string.Substring( 0, byte.MaxValue );
            var bytes = new byte[@string.Length + 1];
            bytes[0] = (byte)@string.Length;
            Encoding.UTF8.GetBytes( @string, 0, @string.Length, bytes, 1 );
            return bytes;
        }

        public static byte[] GetBytes( int value )
        {
            var bytes = BitConverter.GetBytes( value );
            if ( BitConverter.IsLittleEndian )
                Array.Reverse( bytes );
            return bytes;
        }

        public static byte[] GetBytes( uint value )
        {
            var bytes = BitConverter.GetBytes( value );
            if ( BitConverter.IsLittleEndian )
                Array.Reverse( bytes );
            return bytes;
        }

        public static byte[] GetBytes( ushort value )
        {
            var bytes = BitConverter.GetBytes( value );
            if ( BitConverter.IsLittleEndian )
                Array.Reverse( bytes );
            return bytes;
        }

        public static string GetString( byte[] bytes, int index, int count )
        {
            string @string = Encoding.UTF8.GetString( bytes, index, count );
            int nullIndex = @string.IndexOf( '\0' );
            if ( nullIndex >= 0 )
                @string = @string.Substring( 0, nullIndex );

            return @string;
        }

        public static uint GetUInt32( byte[] bytes, int index )
        {
            byte[] copiedBytes = GetOrderedBytes( bytes, index, sizeof( uint ) );
            return BitConverter.ToUInt32( copiedBytes, 0 );
        }

        public static ushort GetUInt16( byte[] bytes, int index )
        {
            byte[] copiedBytes = GetOrderedBytes( bytes, index, sizeof( ushort ) );
            return BitConverter.ToUInt16( copiedBytes, 0 );
        }

        public static byte[] CombineArrays( params byte[][] array )
        {
            var rv = new byte[array.Select( x => x.Length ).Sum()];

            for ( int i = 0, insertionPoint = 0; i < array.Length; insertionPoint += array[i].Length, i++ )
                Array.Copy( array[i], 0, rv, insertionPoint, array[i].Length );
            return rv;
        }

        public static T GetEnum<T>( object value, T @default = default(T) ) where T : struct
        {
            var enumType = typeof( T );
            if ( enumType.IsEnum == false )
                throw new InvalidOperationException( string.Format( "{0} is not an enum type", enumType.FullName ) );
            if ( Enum.IsDefined( enumType, Convert.ChangeType( value, Enum.GetUnderlyingType( enumType ) ) ) )
                return (T)Convert.ChangeType( value, Enum.GetUnderlyingType( enumType ) );
            return @default;

        }

        public static UUID GetUUID( byte[] bytes, int index )
        {
            var byteArray = bytes.Skip( index ).Take( 16 ).ToArray();
            if ( byteArray.Length == 16 )
                return new UUID( byteArray );
            return null;
        }

        private static byte[] GetOrderedBytes( byte[] bytes, int index, int length )
        {
            var rv = new byte[length];
            Array.Copy( bytes, index, rv, 0, length );
            if ( BitConverter.IsLittleEndian )
            {
                Array.Reverse( rv );
            }
            return rv;
        }
    }
}
