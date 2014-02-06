
namespace flint.Responses
{
    public class MusicControlResponse : ResponseBase
    {
        public MediaControls Command { get; private set; }

        protected override void Load( byte[] payload )
        {
            Command = (MediaControls) payload[0];
        }
    }
}