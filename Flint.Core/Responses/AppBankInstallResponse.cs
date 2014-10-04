namespace Flint.Core.Responses
{
    [Endpoint( Endpoint.AppManager, 2 )]
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
            uint result = Util.GetUInt32(payload, 1);
            MsgType = Util.GetEnum<MessageType>(result);
        }
    }
}