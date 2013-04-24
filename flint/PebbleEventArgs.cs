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
        public byte[] Payload { get; private set; }

        public MessageReceivedEventArgs(Pebble.Endpoints endpoint, byte[] payload)
        {
            Endpoint = endpoint;
            Payload = new byte[payload.Length];
            payload.CopyTo(Payload, 0);
        }
    }

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
        public TimeReceivedEventArgs(Pebble.Endpoints endpoint, byte[] payload)
            : base(endpoint, payload)
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
            : this(Pebble.Endpoints.TIME, payload)
        {
        }
    }

    /// <summary> Event args for a PING response. </summary>
    public class PingReceivedEventArgs : MessageReceivedEventArgs
    {
        public UInt32 Cookie { get; private set; }

        /// <summary> Create new eventargs for a PING. </summary>
        /// <param name="payload">The payload. Has to be five bytes long, 
        /// otherwise something's wrong.</param>
        public PingReceivedEventArgs(Pebble.Endpoints endpoint, byte[] payload)
            : base(endpoint, payload)
        {
            if (Payload.Length != 5)
            {
                throw new ArgumentOutOfRangeException("payload", "Payload for PING must be five bytes");
            }
            // No need to worry about endianness as ping cookies are echoed byte for byte.
            Cookie = BitConverter.ToUInt32(Payload, 1);
        }

        public PingReceivedEventArgs(byte[] payload)
            : this(Pebble.Endpoints.PING, payload)
        {
        }

    }

    /// <summary> Event args for a LOGS message. </summary>
    public class LogReceivedEventArgs : MessageReceivedEventArgs
    {
        public DateTime Timestamp { get; private set; }
        public byte Level { get; private set; }
        public Int16 LineNo { get; private set; }
        public String Filename { get; private set; }
        public String Message { get; private set; }

        public LogReceivedEventArgs(Pebble.Endpoints endpoint, byte[] payload)
            : base(endpoint, payload)
        {
            byte[] metadata = new byte[8];
            byte msgsize;
            Array.Copy(Payload, metadata, 8);
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
                Timestamp = Util.TimestampToDateTime(BitConverter.ToInt32(metadata, 4));
                Level = metadata[3];
                msgsize = metadata[2];
                LineNo = BitConverter.ToInt16(metadata, 0);
            }
            else
            {
                Timestamp = Util.TimestampToDateTime(BitConverter.ToInt32(metadata, 0));
                Level = metadata[4];
                msgsize = metadata[5];
                LineNo = BitConverter.ToInt16(metadata, 6);
            }
            // Now to extract the actual data
            byte[] _filename = new byte[16];
            byte[] _data = new byte[msgsize];
            Array.Copy(Payload, 8, _filename, 0, 16);
            Array.Copy(Payload, 24, _data, 0, msgsize);

            Filename = Encoding.UTF8.GetString(_filename);
            Message = Encoding.UTF8.GetString(_data);
        }

        public LogReceivedEventArgs(byte[] payload)
            : this(Pebble.Endpoints.LOGS, payload)
        {
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
    public class MediaControlReceivedEventArgs : MessageReceivedEventArgs
    {
        public Pebble.MediaControls Command { get; private set; }

        /// <summary> Create a new media control event.  The payload should be 
        /// 1 byte long.
        /// </summary>
        /// <param name="payload"></param>
        public MediaControlReceivedEventArgs(Pebble.Endpoints endpoint, byte[] payload)
            : base(endpoint, payload)
        {
            Command = (Pebble.MediaControls)Payload[0];
        }

        /// <summary> Create a new media control event.  The payload should be 
        /// 1 byte long.
        /// </summary>
        /// <param name="payload"></param>
        public MediaControlReceivedEventArgs(byte[] payload)
            : this(Pebble.Endpoints.MUSIC_CONTROL, payload)
        {
        }
    }

    /// <summary>
    /// Event args for when the contents of the Pebble's app bank have been received.
    /// </summary>
    public class AppbankContentsReceivedEventArgs : MessageReceivedEventArgs
    {
        public AppBank AppBank { get; private set; }
        public AppbankContentsReceivedEventArgs(Pebble.Endpoints endpoint, byte[] payload)
            : base(endpoint, payload)
        {
            AppBank = new AppBank(Payload);
        }

        public AppbankContentsReceivedEventArgs(byte[] payload)
            : this(Pebble.Endpoints.APP_MANAGER, payload)
        {
        }
    }

    public class AppbankInstallMessageEventArgs : MessageReceivedEventArgs
    {
        public enum MessageType
        {
            Available,
            Removed,
            Updated
        }

        public MessageType MsgType { get; private set; }

        public AppbankInstallMessageEventArgs(Pebble.Endpoints endpoint, byte[] payload)
            : base(endpoint, payload)
        {
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(Payload, 1, 4);
            }
            uint result = BitConverter.ToUInt32(Payload, 1);
            MsgType = (MessageType)result; 
        }

        public AppbankInstallMessageEventArgs(byte[] payload)
            : this(Pebble.Endpoints.APP_MANAGER, payload)
        {
        }
    }

}
