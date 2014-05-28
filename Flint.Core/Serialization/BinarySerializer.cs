using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Flint.Core.Serialization
{
    public class BinarySerializer
    {
        public static T ReadObject<T>(byte[] bytes) where T : struct
        {
            int position = 0;
            //Intentionally boxing since PropertyInfo.SetValue will box this if we don't
            object rv = new T();
            foreach (var property in
                from prop in typeof(T).GetRuntimeProperties()
                let attribute = prop.GetCustomAttribute<SerializableAttribute>()
                where attribute != null && prop.CanWrite
                orderby attribute.Order, prop.Name
                select prop)
            {
                var attribute = property.GetCustomAttribute<SerializableAttribute>();

                if (property.PropertyType == typeof (byte))
                {
                    property.SetValue(rv, bytes[position]);
                    position++;
                }
                else if (property.PropertyType == typeof (ushort))
                {
                    property.SetValue(rv, BitConverter.ToUInt16(bytes, position));
                    position += sizeof (ushort);
                }
                else if (property.PropertyType == typeof (uint))
                {
                    property.SetValue(rv, BitConverter.ToUInt32(bytes, position));
                    position += sizeof (uint);
                }
                else if (property.PropertyType == typeof (string))
                {
                    //TODO: Error if size is not set
                    property.SetValue(rv, Util.GetString(bytes, position, attribute.Size));
                    position += attribute.Size;
                }
                else if (property.PropertyType == typeof (UUID))
                {
                    property.SetValue(rv, Util.GetUUID(bytes, position));
                    position += UUID.SIZE;
                }
                else
                {
                    //TODO: Handle object property recursion
                    throw new NotImplementedException("TODO: Handle recursion");
                }
            }
            return (T)rv;
        }
    }
}