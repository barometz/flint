using System;

namespace flint.Responses
{
    public class TimeResponse : ResponseBase
    {
        public DateTime Time { get; private set; }

        public override void Load( byte[] payload )
        {
            if ( payload.Length != 5 )
            {
                throw new ArgumentOutOfRangeException( "payload", "TIME payload must be 5 bytes, the latter four being the timestamp." );
            }

            if ( BitConverter.IsLittleEndian )
            {
                Array.Reverse( payload, 1, 4 );
            }

            int timestamp = BitConverter.ToInt32( payload, 1 );
            Time = Util.GetDateTimeFromTimestamp( timestamp );
        }
    }
}