using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using Flint.Core.Dependencies;

namespace Flint.Core
{
    /// <summary> Represents a Pebble app bundle (.pbw file). </summary>
    public class PebbleBundle
    {
        public enum BundleTypes
        {
            Application,
            Firmware
        }

        private readonly BundleManifest _Manifest;

        /// <summary>
        ///     Create a new PebbleBundle from a .pwb file and parse its metadata.
        /// </summary>
        /// <param name="path">The relative or full path to the file.</param>
        public PebbleBundle( string path )
        {
            //TODO: This used to do System.IO.Path.GetFullPath. Should probably rename the property.
            FullPath = path;
            using ( IZip zip = IoC.Resolve<IZip>() )
            {
                zip.Open( path );

                using ( Stream manifestStream = zip.OpenEntryStream( "manifest.json" ) )
                {
                    if ( manifestStream == null )
                    {
                        throw new InvalidOperationException( "manifest.json not found in archive - not a valid Pebble bundle." );
                    }
                    var serializer = new DataContractJsonSerializer( typeof( BundleManifest ) );
                    _Manifest = (BundleManifest)serializer.ReadObject( manifestStream );
                }

                HasResources = ( _Manifest.Resources.Size != 0 );

                if ( _Manifest.Type == "firmware" )
                {
                    BundleType = BundleTypes.Firmware;
                }
                else
                {
                    BundleType = BundleTypes.Application;
                    using ( Stream binStream = zip.OpenEntryStream( _Manifest.Application.Filename ) )
                    {
                        if ( binStream == null )
                        {
                            throw new Exception( string.Format( "App file {0} not found in archive", _Manifest.Application.Filename ) );
                        }

                        AppMetadata = Util.ReadStruct<ApplicationMetadata>( binStream );
                    }
                }
            }
        }

        public BundleTypes BundleType { get; private set; }
        public Boolean HasResources { get; private set; }

        /// <summary> The filename. </summary>
        public string Filename
        {
            get { return Path.GetFileName( FullPath ); }
        }

        /// <summary> The full path to the file. </summary>
        public string FullPath { get; private set; }

        public ApplicationMetadata AppMetadata { get; private set; }

        public BundleManifest Manifest
        {
            get { return _Manifest; }
        }

        public override string ToString()
        {
            if ( BundleType == BundleTypes.Application )
            {
                return string.Format( "{0} containing watch app {1}", Filename, AppMetadata );
            }

            // This is pretty ugly, but will do for now.
            return string.Format( "{0} containing fw version {1} for hw rev {2}", Filename,
                                 _Manifest.Resources.FriendlyVersion, _Manifest.Firmware.HardwareRevision );
        }

        /// <summary> Maps to the metadata as stored at the start of the application binary. </summary>
        [StructLayout( LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1 )]
        public struct ApplicationMetadata
        {
            /// <summary> Gets a string representation of the application version. </summary>
            public string AppVersion
            {
                get { return string.Format( "{0}.{1}", AppMajorVersion, AppMinorVersion ); }
            }

            /// <summary> Gets a string representation of the SDK version used to produce this application. </summary>
            public string SDKVersion
            {
                get { return string.Format( "{0}.{1}", SDKMajorVersion, SDKMinorVersion ); }
            }

            /// <summary> Gets a string representation of the metadata version. </summary>
            public string StructVersion
            {
                get { return string.Format( "{0}.{1}", StructMajorVersion, StructMinorVersion ); }
            }

            // The data as stored in the binary
            [DataMember(Order = 0)]
            public readonly string header;
            [DataMember(Order = 1)]
            public readonly byte StructMajorVersion;
            [DataMember (Order = 2)]
            public readonly byte StructMinorVersion;
            [DataMember(Order = 3)]
            public readonly byte SDKMajorVersion;
            [DataMember(Order = 4)]
            public readonly byte SDKMinorVersion;
            [DataMember(Order = 5)]
            public readonly byte AppMajorVersion;
            [DataMember(Order = 6)]
            public readonly byte AppMinorVersion;
            [DataMember(Order = 7)]
            public readonly UInt16 Size;
            [DataMember(Order = 8)]
            public readonly uint Offset;
            [DataMember(Order = 9)]
            public readonly uint CRC;
            [DataMember(Order = 10)]
            public readonly string AppName;
            [DataMember(Order = 11)]
            public readonly string CompanyName;
            [DataMember(Order = 12)]
            public readonly uint IconResourceID;
            [DataMember(Order = 13)]
            public readonly uint SymbolTableAddress;
            [DataMember(Order = 14)]
            public readonly uint Flags;
            [DataMember(Order = 15)]
            public readonly uint RelocationListStart;
            [DataMember(Order = 16)]
            public readonly uint RelocationListItemCount;
            [DataMember(Order = 17)]
            public readonly byte[] UUID;

            public override string ToString()
            {
                return string.Format( "{0}, version {1}.{2} by {3}", AppName, AppMajorVersion, AppMinorVersion,
                                     CompanyName );
            }
        }

        /// <summary> Maps to the information in manifest.json. </summary>
        [DataContract]
        public class BundleManifest
        {
            [DataMember( Name = "manifestVersion", IsRequired = true )]
            public int ManifestVersion { get; private set; }

            [DataMember( Name = "generatedAt", IsRequired = true )]
            public uint GeneratedAt { get; private set; }

            /// <summary> The date and time at which this bundle was generated. </summary>
            public DateTime GeneratedAtDateTime
            {
                get { return Util.GetDateTimeFromTimestamp( GeneratedAt ); }
            }

            /// <summary> Name of the machine on which this bundle was generated. </summary>
            [DataMember( Name = "generatedBy", IsRequired = true )]
            public string GeneratedBy { get; private set; }

            /// <summary> The manifest for the application contained in this bundle. </summary>
            [DataMember( Name = "application", IsRequired = false )]
            public ApplicationManifest Application { get; private set; }

            /// <summary> The manifest for the firmware contained in this bundle. </summary>
            [DataMember( Name = "firmware", IsRequired = false )]
            public FirmwareManifest Firmware { get; private set; }

            /// <summary> The manifest for the resources contained in this bundle. </summary>
            [DataMember( Name = "resources", IsRequired = false )]
            public ResourcesManifest Resources { get; private set; }

            /// <summary> The type of Bundle </summary>
            [DataMember( Name = "type", IsRequired = true )]
            public string Type { get; private set; }

            /// <summary> Maps to the application-specific information in manifest.json. </summary>
            [DataContract]
            public struct ApplicationManifest
            {
                /// <summary> The filename of the application binary in the bundle. </summary>
                [DataMember( Name = "name", IsRequired = true )]
                public string Filename { get; private set; }

                /// <summary> The firmware version required to run this application. </summary>
                [DataMember( Name = "reqFwVer", IsRequired = true )]
                public int RequiredFirmwareVersion { get; private set; }

                /// <summary> The time at which the application binary was created. (?) </summary>
                [DataMember( Name = "timestamp", IsRequired = true )]
                public uint Timestamp { get; private set; }

                /// <summary> The time at which the application binary was created. (?) </summary>
                public DateTime TimestampDT
                {
                    get { return Util.GetDateTimeFromTimestamp( Timestamp ); }
                }

                /// <summary> The CRC of the application binary. </summary>
                [DataMember( Name = "crc", IsRequired = true )]
                public uint CRC { get; private set; }

                /// <summary> The size of the application binary in bytes. </summary>
                [DataMember( Name = "size", IsRequired = true )]
                public int Size { get; private set; }
            }

            /// <summary> Maps to the firmware-specific information in manifest.json. </summary>
            [DataContract]
            public struct FirmwareManifest
            {
                /// <summary> The filename of the firmware binary in the bundle. </summary>
                [DataMember( Name = "name", IsRequired = true )]
                public string Filename { get; private set; }

                /// <summary> The time at which the firmware binary was created. (?) </summary>
                [DataMember( Name = "timestamp", IsRequired = true )]
                public uint Timestamp { get; private set; }

                /// <summary> The time at which the firmware binary was created. (?) </summary>
                public DateTime TimestampDT
                {
                    get { return Util.GetDateTimeFromTimestamp( Timestamp ); }
                }

                /// <summary> The CRC of the firmware binary. </summary>
                [DataMember( Name = "crc", IsRequired = true )]
                public uint CRC { get; private set; }

                /// <summary> The hardware revision this firmware was built for. </summary>
                [DataMember( Name = "hwrev", IsRequired = true )]
                public string HardwareRevision { get; private set; }

                /// <summary> The type of the firmware (recovery or normal). </summary>
                [DataMember( Name = "type", IsRequired = true )]
                public string Type { get; private set; }

                /// <summary> Indicates whether the firmware is intended for recovery usage. </summary>
                public bool IsRecovery
                {
                    get { return ( Type == "recovery" ); }
                }

                /// <summary> The size of the firmware binary in bytes. </summary>
                [DataMember( Name = "size", IsRequired = true )]
                public int Size { get; private set; }
            }

            /// <summary> Maps to the resources-specific infromation in manifest.json. </summary>
            [DataContract]
            public struct ResourcesManifest
            {
                /// <summary> The filename of the resources package in the bundle. </summary>
                [DataMember( Name = "name", IsRequired = true )]
                public string Filename { get; private set; }

                /// <summary> The time at which the resources package was created. (?) </summary>
                [DataMember( Name = "timestamp", IsRequired = true )]
                public uint Timestamp { get; private set; }

                /// <summary> The time at which the resources package was created. (?) </summary>
                public DateTime TimestampDateTime
                {
                    get { return Util.GetDateTimeFromTimestamp( Timestamp ); }
                }

                /// <summary> The CRC of the resources package. </summary>
                [DataMember( Name = "crc", IsRequired = true )]
                public uint CRC { get; private set; }

                /// <summary> The human-readable version string for the resources package. </summary>
                [DataMember( Name = "friendlyVersion", IsRequired = true )]
                public string FriendlyVersion { get; private set; }

                /// <summary> The size of the resources package in bytes. </summary>
                [DataMember( Name = "size", IsRequired = true )]
                public int Size { get; private set; }
            }
        }
    }
}