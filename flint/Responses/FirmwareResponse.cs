using System;
using System.Linq;

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
            if ( BitConverter.IsLittleEndian )
                Array.Reverse( data, 0, 4 );

            DateTime timestamp = Util.GetDateTimeFromTimestamp( BitConverter.ToInt32( data, 0 ) );
            string version = Util.GetString( data, 4, 32 );
            string commit = Util.GetString( data, 36, 8 );
            Boolean isRecovery = BitConverter.ToBoolean( data, 44 );
            byte hardwarePlatform = data[45];
            byte metadataVersion = data[46];
            return new FirmwareVersion( timestamp, version, commit, isRecovery, hardwarePlatform, metadataVersion );
        }
    }
}