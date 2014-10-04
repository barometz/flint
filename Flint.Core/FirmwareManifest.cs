using System;
using System.Runtime.Serialization;

namespace Flint.Core
{
    /// <summary> Maps to the firmware-specific information in manifest.json. </summary>
    [DataContract]
    public struct FirmwareManifest
    {
        /// <summary> The filename of the firmware binary in the bundle. </summary>
        [DataMember(Name = "name", IsRequired = true)]
        public string Filename { get; private set; }

        /// <summary> The time at which the firmware binary was created. (?) </summary>
        [DataMember(Name = "timestamp", IsRequired = true)]
        public uint Timestamp { get; private set; }

        /// <summary> The time at which the firmware binary was created. (?) </summary>
        public DateTime TimestampDT
        {
            get { return Util.GetDateTimeFromTimestamp(Timestamp); }
        }

        /// <summary> The CRC of the firmware binary. </summary>
        [DataMember(Name = "crc", IsRequired = true)]
        public uint CRC { get; private set; }

        /// <summary> The hardware revision this firmware was built for. </summary>
        [DataMember(Name = "hwrev", IsRequired = true)]
        public string HardwareRevision { get; private set; }

        /// <summary> The type of the firmware (recovery or normal). </summary>
        [DataMember(Name = "type", IsRequired = true)]
        public string Type { get; private set; }

        /// <summary> Indicates whether the firmware is intended for recovery usage. </summary>
        public bool IsRecovery
        {
            get { return (Type == "recovery"); }
        }

        /// <summary> The size of the firmware binary in bytes. </summary>
        [DataMember(Name = "size", IsRequired = true)]
        public int Size { get; private set; }
    }
}