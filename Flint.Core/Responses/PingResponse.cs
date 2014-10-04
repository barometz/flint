using System;

namespace Flint.Core.Responses
{
    [Endpoint( Endpoint.Ping )]
    public class PingResponse : ResponseBase
    {
        public uint Cookie { get; private set; }

        protected override void Load( byte[] payload )
        {
            if (payload.Length != 5)
            {
                throw new ArgumentOutOfRangeException("payload", "Payload for PING must be five bytes");
            }
            // No need to worry about endianness as ping cookies are echoed byte for byte.
            Cookie = BitConverter.ToUInt32(payload, 1);
        }
    }
}