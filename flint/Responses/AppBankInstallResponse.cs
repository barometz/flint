using System;

namespace flint.Responses
{
    public class AppbankInstallResponse : ResponseBase
    {
        public enum MessageType
        {
            Available,
            Removed,
            Updated
        }

        public MessageType MsgType { get; private set; }

        protected override void Load( byte[] payload )
        {
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(payload, 1, 4);
            }
            uint result = BitConverter.ToUInt32(payload, 1);
            MsgType = (MessageType)result; 
        }
    }
}