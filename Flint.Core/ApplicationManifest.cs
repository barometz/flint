using System;
using System.Runtime.Serialization;

namespace Flint.Core
{
    /// <summary> Maps to the application-specific information in manifest.json. </summary>
    [DataContract]
    public struct ApplicationManifest
    {
        /// <summary> The filename of the application binary in the bundle. </summary>
        [DataMember(Name = "name", IsRequired = true)]
        public string Filename { get; private set; }

        /// <summary> The firmware version required to run this application. </summary>
        [DataMember(Name = "reqFwVer", IsRequired = true)]
        public int RequiredFirmwareVersion { get; private set; }

        /// <summary> The time at which the application binary was created. (?) </summary>
        [DataMember(Name = "timestamp", IsRequired = true)]
        public uint Timestamp { get; private set; }

        /// <summary> The time at which the application binary was created. (?) </summary>
        public DateTime TimestampDT
        {
            get { return Util.GetDateTimeFromTimestamp(Timestamp); }
        }

        /// <summary> The CRC of the application binary. </summary>
        [DataMember(Name = "crc", IsRequired = true)]
        public uint CRC { get; private set; }

        /// <summary> The size of the application binary in bytes. </summary>
        [DataMember(Name = "size", IsRequired = true)]
        public int Size { get; private set; }
    }
}