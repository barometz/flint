
namespace flint.Responses
{
    [Endpoint(Endpoint.MusicControl)]
    public class MusicControlResponse : ResponseBase
    {
        public MediaControl Command { get; private set; }

        protected override void Load( byte[] payload )
        {
            Command = Util.GetEnum<MediaControl>(payload[0]);
        }
    }
}