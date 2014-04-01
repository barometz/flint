using System;

namespace Flint.Core.Serialization
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class SerializableAttribute : Attribute
    {
        private int _size;

        public int Size
        {
            get { return _size; }
            set
            {
                if (value < 0)
                    throw new InvalidOperationException("Size cannot be negative");
                _size = value;
            }
        }

        public int Order { get; set; }
    }
}