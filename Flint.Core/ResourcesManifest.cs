using System;
using System.Runtime.Serialization;

namespace Flint.Core
{
    /// <summary> Maps to the resources-specific infromation in manifest.json. </summary>
    [DataContract]
    public struct ResourcesManifest
    {
        /// <summary> The filename of the resources package in the bundle. </summary>
        [DataMember(Name = "name", IsRequired = true)]
        public string Filename { get; private set; }

        /// <summary> The time at which the resources package was created. (?) </summary>
        [DataMember(Name = "timestamp", IsRequired = true)]
        public uint Timestamp { get; private set; }

        /// <summary> The time at which the resources package was created. (?) </summary>
        public DateTime TimestampDateTime
        {
            get { return Util.GetDateTimeFromTimestamp(Timestamp); }
        }

        /// <summary> The CRC of the resources package. </summary>
        [DataMember(Name = "crc", IsRequired = true)]
        public uint CRC { get; private set; }

        /// <summary> The human-readable version string for the resources package. </summary>
        [DataMember(Name = "friendlyVersion", IsRequired = false)]
        public string FriendlyVersion { get; private set; }

        /// <summary> The size of the resources package in bytes. </summary>
        [DataMember(Name = "size", IsRequired = true)]
        public int Size { get; private set; }
    }
}