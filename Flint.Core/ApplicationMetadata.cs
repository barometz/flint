using Flint.Core.Serialization;

namespace Flint.Core
{
    /// <summary> Maps to the metadata as stored at the start of the application binary. </summary>
    public struct ApplicationMetadata
    {
        /// <summary> Gets a string representation of the application version. </summary>
        public string AppVersion
        {
            get { return string.Format("{0}.{1}", AppMajorVersion, AppMinorVersion); }
        }

        /// <summary> Gets a string representation of the SDK version used to produce this application. </summary>
        public string SDKVersion
        {
            get { return string.Format("{0}.{1}", SDKMajorVersion, SDKMinorVersion); }
        }

        /// <summary> Gets a string representation of the metadata version. </summary>
        public string StructVersion
        {
            get { return string.Format("{0}.{1}", StructMajorVersion, StructMinorVersion); }
        }

        // The data as stored in the binary
        [Serializable(Order = 0, Size = 8)]
        public string Header { get; set; }
        [Serializable(Order = 1)]
        public byte StructMajorVersion { get; set; }
        [Serializable(Order = 2)]
        public byte StructMinorVersion { get; set; }
        [Serializable(Order = 3)]
        public byte SDKMajorVersion { get; set; }
        [Serializable(Order = 4)]
        public byte SDKMinorVersion { get; set; }
        [Serializable(Order = 5)]
        public byte AppMajorVersion { get; set; }
        [Serializable(Order = 6)]
        public byte AppMinorVersion { get; set; }
        [Serializable(Order = 7)]
        public ushort Size { get; set; }
        [Serializable(Order = 8)]
        public uint Offset { get; set; }
        [Serializable(Order = 9)]
        public uint CRC { get; set; }
        [Serializable(Order = 10, Size = 32)]
        public string AppName { get; set; }
        [Serializable(Order = 11, Size = 32)]
        public string CompanyName { get; set; }
        [Serializable(Order = 12)]
        public uint IconResourceID { get; set; }
        [Serializable(Order = 13)]
        public uint SymbolTableAddress { get; set; }
        [Serializable(Order = 14)]
        public uint Flags { get; set; }
        [Serializable(Order = 15)]
        public uint RelocationListStart { get; set; }
        [Serializable(Order = 16)]
        public uint RelocationListItemCount { get; set; }
        [Serializable(Order = 17)]
        public UUID UUID { get; set; }

        public override string ToString()
        {
            return string.Format("{0}, version {1}.{2} by {3}", AppName, AppMajorVersion, AppMinorVersion,
                CompanyName);
        }
    }
}