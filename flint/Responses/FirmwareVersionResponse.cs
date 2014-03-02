using System;
using System.Linq;

namespace flint.Responses
{
    [Endpoint(Endpoint.FirmwareVersion)]
    public class FirmwareVersionResponse : ResponseBase
    {
        public FirmwareVersion Firmware { get; private set; }
        public FirmwareVersion RecoveryFirmware { get; private set; }

        protected override void Load( byte[] payload )
        {
            Firmware = ParseVersion( payload.Skip( 1 ).Take( 47 ).ToArray() );
            RecoveryFirmware = ParseVersion( payload.Skip( 48 ).Take( 47 ).ToArray() );
        }

        private static FirmwareVersion ParseVersion( byte[] data )
        {
            //TODO: data validation
            /*
             * The layout of the version info is:
             *  0: 3 Timestamp (int32)
             *  4:35 Version (string, padded with \0)
             * 36:43 Commit (hash?) (string, padded with \0)
             * 44:   Is recovery? (Boolean)
             * 45:   HW Platform (byte)
             * 46:   Metadata version (byte)
             */

            DateTime timestamp = Util.GetDateTimeFromTimestamp( Util.GetUInt32( data, 0 ) );
            string version = Util.GetString( data, 4, 32 );
            string commit = Util.GetString( data, 36, 8 );
            bool isRecovery = BitConverter.ToBoolean( data, 44 );
            byte hardwarePlatform = data[45];
            byte metadataVersion = data[46];
            return new FirmwareVersion( timestamp, version, commit, isRecovery, hardwarePlatform, metadataVersion );
        }
    }
}