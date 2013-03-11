using System;

namespace flint
{
    public class MessageReceivedEventArgs : EventArgs
    {
        public Pebble.Endpoints Endpoint { get; private set; }
        public byte[] Message { get; private set; }

        public MessageReceivedEventArgs(Pebble.Endpoints endpoint, byte[] message)
        {
            Endpoint = endpoint;
            Message = message;
        }
    }

    /// <summary>
    /// Represents a (connection to a) Pebble.
    /// </summary>
    public class Pebble
    {
        
        public enum Endpoints : ushort
        {
            FIRMWARE = 1,
            TIME = 11,
            VERSIONS = 16,
            PHONE_VERSION = 17,
            SYSTEM_MESSAGE = 18,
            MUSIC_CONTROL = 32,
            PHONE_CONTROL = 33,
            LOGS = 2000,
            PING = 2001,
            DRAW = 2002,
            RESET = 2003,
            APPMFG = 2004,
            NOTIFICATION = 3000,
            SYS_REG = 5000,
            FCT_REG = 5001,
            APP_INSTALL_MANAGER = 6000,
            RUNKEEPER = 7000,
            PUT_BYTES = 48879,
            MAX_ENDPOINT = 65535
        }

        public event EventHandler<MessageReceivedEventArgs> MessageReceived;
        public event EventHandler<MessageReceivedEventArgs> UnknownEndpointReceived;

        PebbleProtocol pebbleProt;

        public Pebble(String port)
        {
            pebbleProt = new PebbleProtocol(port);
            pebbleProt.RawMessageReceived += pebbleProt_RawMessageReceived;
        }

        void pebbleProt_RawMessageReceived(object sender, RawMessageReceivedEventArgs e)
        {
            EventHandler<MessageReceivedEventArgs> handler;
            switch (e.Endpoint)
            {
                default:
                    handler = UnknownEndpointReceived;
                    break;
            }

            if (handler != null)
            {
                handler(this, new MessageReceivedEventArgs((Endpoints)e.Endpoint, e.Payload));
            }

            // Catchall:
            handler = MessageReceived;
            if (handler != null)
            {
                handler(this, new MessageReceivedEventArgs((Endpoints)e.Endpoint, e.Payload));
            }
        }
    }
}
