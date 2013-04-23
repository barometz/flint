using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace flint
{
    static class Util
    {
        /// <summary>
        /// Reads serialized struct data back into a struct, much like fread() might do in C.
        /// </summary>
        /// <param name="fs"></param>
        public static T ReadStruct<T>(Stream fs) where T : struct
        {
            // Borrowed from http://stackoverflow.com/a/1936208 because BitConverter-ing all of this would be a pain
            byte[] buffer = new byte[Marshal.SizeOf(typeof(T))];
            fs.Read(buffer, 0, buffer.Length);
            return ReadStruct<T>(buffer);
        }

        public static T ReadStruct<T>(byte[] bytes) where T : struct
        {
            if (bytes.Count() != Marshal.SizeOf(typeof(T)))
            {
                throw new ArgumentException("Byte array does not match size of target type.");
            }
            T ret;
            GCHandle hdl = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try
            {
                ret = (T)Marshal.PtrToStructure(hdl.AddrOfPinnedObject(), typeof(T));
            }
            finally
            {
                hdl.Free();
            }
            return ret;
        }

        /// <summary> Convert a Unix timestamp to a DateTime object.
        /// </summary>
        /// <remarks>
        /// This has some issues, as Pebble isn't timezone-aware and it's 
        /// unclear how either side deals with leap seconds.  For basic usage
        /// this should be plenty, though.
        /// </remarks>
        /// <param name="ts"></param>
        /// <returns></returns>
        public static DateTime TimestampToDateTime(Int32 ts)
        {
            return new DateTime(1970, 1, 1).AddSeconds(ts);
        }

        static uint CRC32_ProcessWord(uint data, uint crc)
        {
            // Crudely ported from https://github.com/pebble/libpebble/blob/master/pebble/stm32_crc.py
            uint poly = 0x04C11DB7;
            crc = crc ^ data;
            for (int i = 0; i < 32; i++)
            {
                if ((crc & 0x80000000) != 0)
                {
                    crc = (crc << 1) ^ poly;
                }
                else
                {
                    crc = (crc << 1);
                }
            }
            return crc;
        }

        /// <summary>
        /// CRC32 function that uses the same parameters etc as Pebble's hardware implementation.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static uint CRC32(byte[] data)
        {
            if (data.Count() % 4 != 0)
            {
                int padsize = 4 - data.Count() % 4;
                data = data.Concat(new byte[padsize]).ToArray();
            }
            uint crc = 0xFFFFFFFF;
            for (int i = 0; i < data.Count(); i += 4)
            {
                uint currentword = BitConverter.ToUInt32(data, i);
                crc = CRC32_ProcessWord(currentword, crc);
            }
            return crc;
        }

    }
}
