
namespace flint.Responses
{
    public class MusicControlResponse : ResponseBase
    {
        public MediaControls Command { get; private set; }

        public override void Load( byte[] payload )
        {
            Command = (MediaControls) payload[0];
        }
    }
}