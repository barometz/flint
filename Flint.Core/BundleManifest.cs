using System;
using System.Runtime.Serialization;

namespace Flint.Core
{
    /// <summary> Maps to the information in manifest.json. </summary>
    [DataContract]
    public class BundleManifest
    {
        [DataMember(Name = "manifestVersion", IsRequired = true)]
        public int ManifestVersion { get; private set; }

        [DataMember(Name = "generatedAt", IsRequired = true)]
        public uint GeneratedAt { get; private set; }

        /// <summary> The date and time at which this bundle was generated. </summary>
        public DateTime GeneratedAtDateTime
        {
            get { return Util.GetDateTimeFromTimestamp(GeneratedAt); }
        }

        /// <summary> Name of the machine on which this bundle was generated. </summary>
        [DataMember(Name = "generatedBy", IsRequired = true)]
        public string GeneratedBy { get; private set; }

        /// <summary> The manifest for the application contained in this bundle. </summary>
        [DataMember(Name = "application", IsRequired = false)]
        public ApplicationManifest Application { get; private set; }

        /// <summary> The manifest for the firmware contained in this bundle. </summary>
        [DataMember(Name = "firmware", IsRequired = false)]
        public FirmwareManifest Firmware { get; private set; }

        /// <summary> The manifest for the resources contained in this bundle. </summary>
        [DataMember(Name = "resources", IsRequired = false)]
        public ResourcesManifest Resources { get; private set; }

        /// <summary> The type of Bundle </summary>
        [DataMember(Name = "type", IsRequired = true)]
        public string Type { get; private set; }

    }
}