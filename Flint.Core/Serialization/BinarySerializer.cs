using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Flint.Core.Serialization
{
    public class BinarySerializer
    {
        public static T ReadObject<T>(Stream stream) where T : struct
        {
            //Intentionally boxing since PropertyInfo.SetValue will box this if we don't
            object rv = new T();
            foreach (var property in 
                from prop in typeof (T).GetRuntimeProperties()
                let attribute = prop.GetCustomAttribute<SerializableAttribute>()
                where attribute != null && prop.CanWrite
                orderby attribute.Order, prop.Name
                select prop)
            {
                var attribute = property.GetCustomAttribute<SerializableAttribute>();

                if (property.PropertyType == typeof(byte))
                    property.SetValue(rv, GetValue(stream, 1, b => b[0]));
                else if (property.PropertyType == typeof(ushort))
                    property.SetValue(rv, GetValue(stream, sizeof(ushort), b => BitConverter.ToUInt16(b, 0)));
                else if (property.PropertyType == typeof(uint))
                    property.SetValue(rv, GetValue(stream, sizeof(uint), b => BitConverter.ToUInt32(b, 0)));
                else if (property.PropertyType == typeof(string))
                    //TODO: Error if size is not set
                    property.SetValue(rv, GetValue(stream, attribute.Size, b => Util.GetString(b, 0, attribute.Size)));
                else if (property.PropertyType == typeof (UUID))
                    property.SetValue(rv, GetValue(stream, UUID.SIZE, b => Util.GetUUID(b, 0)));
                else
                {
                    //TODO: Handle recursion
                    throw new NotImplementedException("TODO: Handle recursion");
                }
                    

            }
            return (T)rv;
        }

        private static T GetValue<T>(Stream stream, int size, Func<byte[], T> converter)
        {
            byte[] bytes = GetBytes(stream, size);
            return converter(bytes);
        }

        private static byte[] GetBytes(Stream stream, int count)
        {
            var rv = new byte[count];
            stream.Read(rv, 0, count);
            return rv;
        }
    }
}