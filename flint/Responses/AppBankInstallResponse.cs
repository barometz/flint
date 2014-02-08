using System;

namespace flint.Responses
{
    [Endpoint(Endpoint.AppManager, 2)]
    public class AppbankInstallResponse : ResponseBase
    {
        public enum MessageType : byte
        {
            Available = 0,
            Removed = 1,
            Updated = 2
        }

        public MessageType MsgType { get; private set; }

        protected override void Load( byte[] payload )
        {
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(payload, 1, 4);
            }
            uint result = Util.GetUInt32(payload, 1);
            MsgType = Util.GetEnum<MessageType>(result); 
        }
    }
}