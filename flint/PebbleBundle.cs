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
            public class ApplicationManifest
            {
                /// <summary> The firmware version required to run this application. </summary>
                [DataMember(Name = "reqFwVer")]
                public readonly int RequiredFirmwareVersion;
                /// <summary> The filename of the application binary in the bundle. </summary>
                [DataMember(Name = "name")]
                public readonly String Filename;
            }
            [DataMember(Name = "manifestVersion")]
            public readonly int ManifestVersion;
            
            [DataMember(Name="generatedAt")]
            private int GeneratedAtTS;
            
            /// <summary> The date and time at which this bundle was generated. </summary>
            public DateTime GeneratedAt { get { return Util.TimestampToDateTime(GeneratedAtTS); } }
            
            /// <summary> Name of the machine on which this bundle was generated. </summary>
            [DataMember(Name = "generatedBy")]
            public readonly String GeneratedBy;
            
            /// <summary> The manifest for the application contained in this bundle. </summary>
            [DataMember(Name = "application")]
            public readonly ApplicationManifest Application;
        }

        /// <summary> Maps to the metadata as stored at the start of the application binary. </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack=1)]
        public struct ApplicationMetadata
        {
            /// <summary> Gets a string representation of the application version. </summary>
            public String AppVersion { get { return String.Format("{0}.{1}", AppMajorVersion, AppMinorVersion); } }
            
            /// <summary> Gets a string representation of the SDK version used to produce this application. </summary>
            public String SDKVersion { get { return String.Format("{0}.{1}", SDKMajorVersion, SDKMinorVersion); } }
            
            /// <summary> Gets a string representation of the metadata version. </summary>
            public String StructVersion { get { return String.Format("{0}.{1}", StructMajorVersion, StructMinorVersion); } }
            
            // The data as stored in the binary
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst=8)]
            public readonly String header;
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
            public readonly String AppName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public readonly String CompanyName;
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
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
            public readonly String UUID;

            public override string ToString()
            {
                String format = "{0}, version {1}.{2} by {3}";
                return String.Format(format, AppName, AppMajorVersion, AppMinorVersion, CompanyName);
            }
        }
        /// <summary> The filename. </summary>
        public String Filename { get { return Path.GetFileName(FullPath); } }
        /// <summary> The full path to the file. </summary>
        public String FullPath { get; private set; }
        public ApplicationMetadata Application { get; private set; }

        ZipFile Bundle;
        BundleManifest Manifest;
        
        /// <summary>
        /// Create a new PebbleBundle from a .pwb file and parse its metadata.
        /// </summary>
        /// <param name="path">The relative or full path to the file.</param>
        public PebbleBundle(String path)
        {
            Stream jsonstream;
            Stream binstream;
            
            FullPath = Path.GetFullPath(path);
            Bundle = ZipFile.Read(FullPath);

            if (Bundle.ContainsEntry("manifest.json"))
            {
                jsonstream = Bundle["manifest.json"].OpenReader();
            }
            else
            {
                throw new ArgumentException("manifest.json not found in archive - not a Pebble bundle.");
            }

            var serializer = new DataContractJsonSerializer(typeof(BundleManifest));
            Manifest = serializer.ReadObject(jsonstream) as BundleManifest;

            if (Bundle.ContainsEntry(Manifest.Application.Filename))
            {
                binstream = Bundle[Manifest.Application.Filename].OpenReader();
            }
            else
            {
                String format = "App file {0} not found in archive";
                throw new ArgumentException(String.Format(format, Manifest.Application.Filename));
            }

            Application = Util.ReadStruct<ApplicationMetadata>(binstream);
        }



        public override string ToString()
        {
            String format = "{0} from {1}";
            return String.Format(format, Application, Filename);
        }
    }
}
