using System;

namespace flint.Responses
{
    [Endpoint(Endpoint.Time)]
    public class TimeResponse : ResponseBase
    {
        public DateTime Time { get; private set; }

        protected override void Load( byte[] payload )
        {
            if ( payload.Length != 5 )
            {
                throw new ArgumentOutOfRangeException( "payload", "TIME payload must be 5 bytes, the latter four being the timestamp." );
            }
            
            uint timestamp = Util.GetUInt32( payload, 1 );
            Time = Util.GetDateTimeFromTimestamp( timestamp );
        }
    }
}