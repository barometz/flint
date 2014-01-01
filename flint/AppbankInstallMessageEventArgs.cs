using System;

namespace flint
{
    public class AppbankInstallMessageEventArgs : MessageReceivedEventArgs
    {
        public enum MessageType
        {
            Available,
            Removed,
            Updated
        }

        public MessageType MsgType { get; private set; }

        public AppbankInstallMessageEventArgs(Pebble.Endpoints endPoint, byte[] payload)
            : base(endPoint, payload)
        {
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(Payload, 1, 4);
            }
            uint result = BitConverter.ToUInt32(Payload, 1);
            MsgType = (MessageType)result; 
        }

        public AppbankInstallMessageEventArgs(byte[] payload)
            : this(Pebble.Endpoints.AppManager, payload)
        {
        }
    }

}
