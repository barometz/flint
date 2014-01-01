using System;

namespace flint
{
    /// <summary> Event args for a PING response. </summary>
    public class PingReceivedEventArgs : MessageReceivedEventArgs
    {
        public UInt32 Cookie { get; private set; }

        /// <summary> Create new eventargs for a PING. </summary>
        /// <param name="payload">The payload. Has to be five bytes long, 
        /// otherwise something's wrong.</param>
        public PingReceivedEventArgs(Pebble.Endpoints endPoint, byte[] payload)
            : base(endPoint, payload)
        {
            if (Payload.Length != 5)
            {
                throw new ArgumentOutOfRangeException("payload", "Payload for PING must be five bytes");
            }
            // No need to worry about endianness as ping cookies are echoed byte for byte.
            Cookie = BitConverter.ToUInt32(Payload, 1);
        }

        public PingReceivedEventArgs(byte[] payload)
            : this(Pebble.Endpoints.Ping, payload)
        {
        }

    }
}