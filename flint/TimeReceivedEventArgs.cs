using System;

namespace flint
{
    /// <summary> Received a TIME response from the Pebble.
    /// </summary>
    public class TimeReceivedEventArgs : MessageReceivedEventArgs
    {
        /// <summary> The time as returned by the Pebble.
        /// </summary>
        public DateTime Time { get; private set; }

        /// <summary> Create a new TimeReceivedEventArgs.
        /// </summary>
        /// <param name="payload">Must be 5 bytes long.  The latter four are interpreted as a timestamp.</param>
        public TimeReceivedEventArgs(Pebble.Endpoints endPoint, byte[] payload)
            : base(endPoint, payload)
        {
            if (Payload.Length != 5)
            {
                throw new ArgumentOutOfRangeException("payload", "TIME payload must be 5 bytes, the latter four being the timestamp.");
            }

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(Payload, 1, 4);
            }
            
            int timestamp = BitConverter.ToInt32(Payload, 1);
            Time = Util.TimestampToDateTime(timestamp);
        }

        /// <summary> Create a new TimeReceivedEventArgs.
        /// </summary>
        /// <param name="payload">Must be 5 bytes long.  The latter four are interpreted as a timestamp.</param>
        public TimeReceivedEventArgs(byte[] payload)
            : this(Pebble.Endpoints.Time, payload)
        {
        }
    }
}