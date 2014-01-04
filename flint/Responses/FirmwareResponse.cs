using System;
using System.Linq;
using System.Text;

namespace flint.Responses
{
    public class FirmwareResponse : ResponseBase
    {
        public FirmwareVersion Firmware { get; private set; }
        public FirmwareVersion RecoveryFirmware { get; private set; }

        public override void Load( byte[] payload )
        {
            Firmware = ParseVersion( payload.Skip( 1 ).Take( 47 ).ToArray() );
            RecoveryFirmware = ParseVersion( payload.Skip( 48 ).Take( 47 ).ToArray() );
        }

        private static FirmwareVersion ParseVersion( byte[] data )
        {
            /*
             * The layout of the version info is:
             *  0: 3 Timestamp (int32)
             *  4:35 Version (string, padded with \0)
             * 36:43 Commit (hash?) (string, padded with \0)
             * 44:   Is recovery? (Boolean)
             * 45:   HW Platform (byte)
             * 46:   Metadata version (byte)
             */
            byte[] _ts = data.Take( 4 ).ToArray();
            if ( BitConverter.IsLittleEndian )
            {
                Array.Reverse( _ts );
            }
            DateTime timestamp = Util.TimestampToDateTime( BitConverter.ToInt32( _ts, 0 ) );
            string version = Encoding.UTF8.GetString( data.Skip( 4 ).Take( 32 ).ToArray() );
            string commit = Encoding.UTF8.GetString( data.Skip( 36 ).Take( 8 ).ToArray() );
            version = version.Substring( 0, version.IndexOf( '\0' ) );
            commit = commit.Substring( 0, commit.IndexOf( '\0' ) );
            Boolean isRecovery = BitConverter.ToBoolean( data, 44 );
            byte hardwarePlatform = data[45];
            byte metadataVersion = data[46];
            return new FirmwareVersion( timestamp, version, commit, isRecovery, hardwarePlatform, metadataVersion );
        }
    }
}