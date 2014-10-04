using Flint.Core.Serialization;

namespace Flint.Core
{
    public struct App
    {
        [Serializable(Order = 0)]
        public uint ID { get; set; }
        [Serializable(Order = 1)]
        public uint Index { get; set; }
        [Serializable(Order = 2, Size = 32)]
        public string Name { get; set; }
        [Serializable(Order = 3, Size = 32)]
        public string Company { get; set; }
        [Serializable(Order = 4)]
        public uint Flags { get; set; }
        [Serializable(Order = 5)]
        public ushort Version { get; set; }

        /// <summary> A string representation of the app version. </summary>
        //public string Version { get { return string.Format("{0}.{1}", MajorVersion, MinorVersion); } }
        public override string ToString()
        {
            return string.Format("{0}, version {1} by {2}", Name, Version, Company);
        }
    }
}