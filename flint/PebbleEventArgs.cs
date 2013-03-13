using System;
using System.Text;

namespace flint
{
    /// <summary> Event args for any Pebble message, containing an endpoint and 
    /// the payload in bytes.
    /// </summary>
    public class MessageReceivedEventArgs : EventArgs
    {
        public Pebble.Endpoints Endpoint { get; private set; }
        public byte[] Message { get; private set; }

        public MessageReceivedEventArgs(Pebble.Endpoints endpoint, byte[] message)
        {
            Endpoint = endpoint;
            Message = message;
        }
    }

    /// <summary> Event args for a PING response. </summary>
    public class PingReceivedEventArgs : EventArgs
    {
        public UInt32 Cookie { get; private set; }

        /// <summary> Create new eventargs for a PING. </summary>
        /// <param name="payload">The payload. Has to be five bytes long, 
        /// otherwise something's wrong.</param>
        public PingReceivedEventArgs(byte[] payload)
        {
            if (payload.Length != 5)
            {
                throw new ArgumentOutOfRangeException("payload", "Payload for PING must be five bytes");
            }
            // No need to worry about endianness as ping cookies are echoed byte for byte.
            Cookie = BitConverter.ToUInt32(payload, 1);
        }
    }

    /// <summary> Event args for a LOGS message. </summary>
    public class LogReceivedEventArgs : EventArgs
    {
        public DateTime Timestamp { get; private set; }
        public byte Level { get; private set; }
        public Int16 LineNo { get; private set; }
        public String Filename { get; private set; }
        public String Message { get; private set; }

        public LogReceivedEventArgs(byte[] payload)
        {
            byte[] metadata = new byte[8];
            byte msgsize;
            Array.Copy(payload, metadata, 8);
            /* 
             * Unpack the metadata.  Eight bytes:
             * 0..3 -> integer timestamp
             * 4    -> Message level (severity)
             * 5    -> Size of the message
             * 6..7 -> Line number (?)
             */
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(metadata);
                Timestamp = timestampToDT(BitConverter.ToInt32(metadata, 4));
                Level = metadata[3];
                msgsize = metadata[2];
                LineNo = BitConverter.ToInt16(metadata, 0);
            }
            else
            {
                Timestamp = timestampToDT(BitConverter.ToInt32(metadata, 0));
                Level = metadata[4];
                msgsize = metadata[5];
                LineNo = BitConverter.ToInt16(metadata, 6);
            }
            // Now to extract the actual data
            byte[] _filename = new byte[16];
            byte[] _data = new byte[msgsize];
            Array.Copy(payload, 8, _filename, 0, 16);
            Array.Copy(payload, 24, _data, 0, msgsize);

            Filename = Encoding.UTF8.GetString(_filename);
            Message = Encoding.UTF8.GetString(_data);
        }

        /// <summary> Convert a Unix timestamp to a DateTime object.
        /// </summary>
        /// <remarks>
        /// This has some issues, as Pebble isn't timezone-aware and it's 
        /// unclear how either side deals with leap seconds.  For basic usage
        /// this should be plenty, though.
        /// </remarks>
        /// <param name="ts"></param>
        /// <returns></returns>
        static DateTime timestampToDT(Int32 ts)
        {
            return new DateTime(1970, 1, 1).AddSeconds(ts);
        }

        public override string ToString()
        {
            String template = "{0} {1,3} {2}:{3,3} {4}";
            return string.Format(template, Timestamp, Level, Filename, LineNo, Message);
        }
    }

    /// <summary> Event args for a media control event (play/pause, forward, 
    /// previous). 
    /// </summary>
    public class MediaControlReceivedEventArgs : EventArgs
    {
        public Pebble.MediaControls Command { get; private set; }

        /// <summary> Create a new media control event.  The payload should be 
        /// 1 byte long.
        /// </summary>
        /// <param name="payload"></param>
        public MediaControlReceivedEventArgs(byte[] payload)
        {
            Command = (Pebble.MediaControls)payload[0];
        }
    }
}
