using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

using Ionic.Zip;

namespace flint
{
    /// <summary> Represents a Pebble app bundle (.pbw file). </summary>
    public class PebbleBundle
    {
        /// <summary> Maps to the information in manifest.json. </summary>
        [DataContract]
        public class BundleManifest
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
                public int Timestamp { get; private set; }
                /// <summary> The time at which the application binary was created. (?) </summary>
                public DateTime TimestampDT { get { return Util.TimestampToDateTime(Timestamp); } }
                
                /// <summary> The CRC of the application binary. </summary>
                [DataMember(Name = "crc", IsRequired = true)]
                public uint CRC { get; private set; }
                
                /// <summary> The size of the application binary in bytes. </summary>
                [DataMember(Name = "size", IsRequired = true)]
                public int Size { get; private set; }
            }

            /// <summary> Maps to the firmware-specific information in manifest.json. </summary>
            [DataContract]
            public struct FirmwareManifest
            {
                /// <summary> The filename of the firmware binary in the bundle. </summary>
                [DataMember(Name = "name", IsRequired = true)]
                public string Filename { get; private set; }

                /// <summary> The time at which the firmware binary was created. (?) </summary>
                [DataMember(Name = "timestamp", IsRequired = true)]
                public int Timestamp { get; private set; }
                /// <summary> The time at which the firmware binary was created. (?) </summary>
                public DateTime TimestampDT { get { return Util.TimestampToDateTime(Timestamp); } }

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
                public bool IsRecovery { get { return (Type == "recovery"); } }

                /// <summary> The size of the firmware binary in bytes. </summary>
                [DataMember(Name = "size", IsRequired = true)]
                public int Size { get; private set; }
            }

            /// <summary> Maps to the resources-specific infromation in manifest.json. </summary>
            [DataContract]
            public struct ResourcesManifest
            {
                /// <summary> The filename of the resources package in the bundle. </summary>
                [DataMember(Name = "name", IsRequired = true)]
                public string Filename { get; private set; }

                /// <summary> The time at which the resources package was created. (?) </summary>
                [DataMember(Name = "timestamp", IsRequired = true)]
                public int Timestamp { get; private set; }
                /// <summary> The time at which the resources package was created. (?) </summary>
                public DateTime TimestampDT { get { return Util.TimestampToDateTime(Timestamp); } }

                /// <summary> The CRC of the resources package. </summary>
                [DataMember(Name = "crc", IsRequired = true)]
                public uint CRC { get; private set; }

                /// <summary> The human-readable version string for the resources package. </summary>
                [DataMember(Name = "friendlyVersion", IsRequired = true)]
                public string FriendlyVersion { get; private set; }

                /// <summary> The size of the resources package in bytes. </summary>
                [DataMember(Name = "size", IsRequired = true)]
                public int Size { get; private set; }
            }

            [DataMember(Name = "manifestVersion", IsRequired = true)]
            public int ManifestVersion { get; private set; }

            [DataMember(Name = "generatedAt", IsRequired = true)]
            public int GeneratedAt { get; private set; }

            /// <summary> The date and time at which this bundle was generated. </summary>
            public DateTime GeneratedAtDT { get { return Util.TimestampToDateTime(GeneratedAt); } }

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
            [DataMember(Name = "resources", IsRequired=false)]
            public ResourcesManifest Resources { get; private set; }

            /// <summary> The type of Bundle </summary>
            [DataMember(Name = "type", IsRequired = true)]
            public string Type { get; private set; }
        }

        /// <summary> Maps to the metadata as stored at the start of the application binary. </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        public struct ApplicationMetadata
        {
            /// <summary> Gets a string representation of the application version. </summary>
            public string AppVersion { get { return String.Format("{0}.{1}", AppMajorVersion, AppMinorVersion); } }

            /// <summary> Gets a string representation of the SDK version used to produce this application. </summary>
            public string SDKVersion { get { return String.Format("{0}.{1}", SDKMajorVersion, SDKMinorVersion); } }

            /// <summary> Gets a string representation of the metadata version. </summary>
            public string StructVersion { get { return String.Format("{0}.{1}", StructMajorVersion, StructMinorVersion); } }

            // The data as stored in the binary
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8)]
            public readonly string header;
            [MarshalAs(UnmanagedType.U1)]
            public readonly byte StructMajorVersion;
            [MarshalAs(UnmanagedType.U1)]
            public readonly byte StructMinorVersion;
            [MarshalAs(UnmanagedType.U1)]
            public readonly byte SDKMajorVersion;
            [MarshalAs(UnmanagedType.U1)]
            public readonly byte SDKMinorVersion;
            [MarshalAs(UnmanagedType.U1)]
            public readonly byte AppMajorVersion;
            [MarshalAs(UnmanagedType.U1)]
            public readonly byte AppMinorVersion;
            [MarshalAs(UnmanagedType.U2)]
            public readonly UInt16 Size;
            [MarshalAs(UnmanagedType.U4)]
            public readonly uint Offset;
            [MarshalAs(UnmanagedType.U4)]
            public readonly uint CRC;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public readonly string AppName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public readonly string CompanyName;
            [MarshalAs(UnmanagedType.U4)]
            public readonly uint IconResourceID;
            [MarshalAs(UnmanagedType.U4)]
            public readonly uint SymbolTableAddress;
            [MarshalAs(UnmanagedType.U4)]
            public readonly uint Flags;
            [MarshalAs(UnmanagedType.U4)]
            public readonly uint RelocationListStart;
            [MarshalAs(UnmanagedType.U4)]
            public readonly uint RelocationListItemCount;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public readonly byte[] UUID;

            public override string ToString()
            {
                const string format = "{0}, version {1}.{2} by {3}";
                return String.Format(format, AppName, AppMajorVersion, AppMinorVersion, CompanyName);
            }
        }

        public enum BundleTypes
        {
            Application,
            Firmware
        }

        public BundleTypes BundleType { get; private set; }
        public Boolean HasResources { get; private set; }

        /// <summary> The filename. </summary>
        public string Filename { get { return Path.GetFileName(FullPath); } }
        /// <summary> The full path to the file. </summary>
        public string FullPath { get; private set; }
        public ApplicationMetadata AppMetadata { get; private set; }

        private readonly ZipFile _Bundle;
        private readonly BundleManifest _Manifest;

        public BundleManifest Manifest
        {
            get { return _Manifest; }
        }

        /// <summary>
        /// Create a new PebbleBundle from a .pwb file and parse its metadata.
        /// </summary>
        /// <param name="path">The relative or full path to the file.</param>
        public PebbleBundle(string path)
        {
            Stream jsonStream;

            FullPath = Path.GetFullPath(path);
            _Bundle = ZipFile.Read(FullPath);

            if (_Bundle.ContainsEntry("manifest.json"))
            {
                jsonStream = _Bundle["manifest.json"].OpenReader();
            }
            else
            {
                throw new ArgumentException("manifest.json not found in archive - not a Pebble bundle.");
            }

            var serializer = new DataContractJsonSerializer(typeof(BundleManifest));

            
            _Manifest = (BundleManifest)serializer.ReadObject(jsonStream);
            jsonStream.Close();

            HasResources = (_Manifest.Resources.Size != 0);

            if (_Manifest.Type == "firmware")
            {
                BundleType = BundleTypes.Firmware;
            }
            else
            {
                BundleType = BundleTypes.Application;
                Stream binStream;
                if (_Bundle.ContainsEntry(_Manifest.Application.Filename))
                {
                    binStream = _Bundle[_Manifest.Application.Filename].OpenReader();
                }
                else
                {
                    const string format = "App file {0} not found in archive";
                    throw new ArgumentException(String.Format(format, _Manifest.Application.Filename));
                }

                AppMetadata = Util.ReadStruct<ApplicationMetadata>(binStream);
                binStream.Close();
            }
            _Bundle.Dispose();
        }

        public override string ToString()
        {
            if (BundleType == BundleTypes.Application)
            {
                const string format = "{0} containing watch app {1}";
                return String.Format(format, Filename, AppMetadata);
            }
            else
            {
                // This is pretty ugly, but will do for now.
                const string format = "{0} containing fw version {1} for hw rev {2}";
                return String.Format(format, Filename, _Manifest.Resources.FriendlyVersion, _Manifest.Firmware.HardwareRevision);
            }
        }
    }
}
