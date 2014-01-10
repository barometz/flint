using System;
using System.Text;

namespace flint
{
    /// <summary> Event args for a LOGS message. </summary>
    public class LogReceivedEventArgs : MessageReceivedEventArgs
    {
        public DateTime Timestamp { get; private set; }
        public byte Level { get; private set; }
        public Int16 LineNo { get; private set; }
        public string Filename { get; private set; }
        public string Message { get; private set; }

        public LogReceivedEventArgs(Endpoints endPoint, byte[] payload)
            : base(endPoint, payload)
        {
            byte[] metadata = new byte[8];
            byte messageSize;
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
                messageSize = metadata[2];
                LineNo = BitConverter.ToInt16(metadata, 0);
            }
            else
            {
                Timestamp = Util.TimestampToDateTime(BitConverter.ToInt32(metadata, 0));
                Level = metadata[4];
                messageSize = metadata[5];
                LineNo = BitConverter.ToInt16(metadata, 6);
            }
            // Now to extract the actual data
            byte[] _filename = new byte[16];
            byte[] _data = new byte[messageSize];
            Array.Copy(Payload, 8, _filename, 0, 16);
            Array.Copy(Payload, 24, _data, 0, messageSize);

            Filename = Encoding.UTF8.GetString(_filename).TrimEnd('\0');
            Message = Encoding.UTF8.GetString(_data);
        }

        public LogReceivedEventArgs(byte[] payload)
            : this(Endpoints.Logs, payload)
        {
        }

        public override string ToString()
        {
            const string template = "{0} {1,3} {2}({3,3}) {4}";
            return string.Format(template, Timestamp, Level, Filename, LineNo, Message);
        }
    }
}