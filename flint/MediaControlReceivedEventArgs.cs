namespace flint
{
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
        public MediaControlReceivedEventArgs(Pebble.Endpoints endPoint, byte[] payload)
            : base(endPoint, payload)
        {
            Command = (Pebble.MediaControls)Payload[0];
        }

        /// <summary> Create a new media control event.  The payload should be 
        /// 1 byte long.
        /// </summary>
        /// <param name="payload"></param>
        public MediaControlReceivedEventArgs(byte[] payload)
            : this(Pebble.Endpoints.MusicControl, payload)
        {
        }
    }
}