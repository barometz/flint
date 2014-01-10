using System;

namespace flint
{
    /// <summary> Event args for any Pebble message, containing an endpoint and 
    /// the payload in bytes.
    /// </summary>
    public class MessageReceivedEventArgs : EventArgs
    {
        public Endpoints Endpoint { get; private set; }
        public byte[] Payload { get; private set; }

        public MessageReceivedEventArgs(Endpoints endPoint, byte[] payload)
        {
            Endpoint = endPoint;
            Payload = new byte[payload.Length];
            payload.CopyTo(Payload, 0);
        }
    }
}