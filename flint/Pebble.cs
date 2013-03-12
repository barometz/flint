using System;
using System.Collections.Generic;
using System.Text;

namespace flint
{
    /// <summary>
    /// Represents a (connection to a) Pebble.
    /// </summary>
    public class Pebble
    {
        // TODO: Autodetecting Pebbles, either by prodding all serial devices or through WMI
        //       (http://msdn.microsoft.com/en-us/library/aa394587%28v=vs.85%29.aspx)
        // TODO: Exception handling.

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
        public event EventHandler<LogReceivedEventArgs> LogReceived;
        public event EventHandler<PingReceivedEventArgs> PingReceived;

        /// <summary>
        /// Holds callbacks for the separate endpoints.  Saves a lot of typing.
        /// There's probably a good reason not to do this.
        /// </summary>
        Dictionary<Endpoints, List<EventHandler<MessageReceivedEventArgs>>> endpointEvents;

        PebbleProtocol pebbleProt;

        public Pebble(String port)
        {
            pebbleProt = new PebbleProtocol(port);
            pebbleProt.RawMessageReceived += pebbleProt_RawMessageReceived;
            
            endpointEvents = new Dictionary<Endpoints,List<EventHandler<MessageReceivedEventArgs>>>();
        }

        public void RegisterEndpointCallback(Endpoints endpoint, EventHandler<MessageReceivedEventArgs> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }
            if (endpointEvents.ContainsKey(endpoint) && endpointEvents[endpoint] != null)
            {
                if (!endpointEvents[endpoint].Contains(handler))
                {
                    endpointEvents[endpoint].Add(handler);
                }
            }
            else
            {
                endpointEvents[endpoint] = new List<EventHandler<MessageReceivedEventArgs>>();
                endpointEvents[endpoint].Add(handler);
            }
        }

        public void DeregisterEndpointCallback(Endpoints endpoint, EventHandler<MessageReceivedEventArgs> handler)
        {
            if (endpointEvents.ContainsKey(endpoint)
                && endpointEvents[endpoint] != null
                && endpointEvents[endpoint].Contains(handler))
            {
                endpointEvents[endpoint].Remove(handler);
            }
        }

        /// <summary> Send the Pebble a ping. </summary>
        /// <param name="cookie"></param>
        /// <param name="async">If set to true, the method returns immediately 
        /// and you'll have to keep an eye on the relevant event.  Otherwise 
        /// it'll wait until there's a reply or timeout.  The latter will throw
        /// a TimeoutException.</param>
        public void Ping(UInt32 cookie = 0, Boolean async = false)
        {
            byte[] _cookie = new byte[5];
            // No need to worry about endianness as it's sent back byte for byte anyway.  
            Array.Copy(BitConverter.GetBytes(cookie), 0, _cookie, 1, 4);

            pebbleProt.sendMessage((UInt16)Endpoints.PING, _cookie);
            if (!async)
            {
                var wait = new EndpointSync(this, Endpoints.PING);
                wait.WaitAndReturn(timeout: 5000);
            }
        }

        void pebbleProt_RawMessageReceived(object sender, RawMessageReceivedEventArgs e)
        {
            Endpoints endpoint = (Endpoints)e.Endpoint;
            // Switch for the specific events
            switch (endpoint)
            {
                case Endpoints.LOGS:
                    EventHandler<LogReceivedEventArgs> loghandler = LogReceived;
                    if (loghandler != null)
                    {
                        loghandler(this, new LogReceivedEventArgs(e.Payload));
                    }
                    break;
                case Endpoints.PING:
                    EventHandler<PingReceivedEventArgs> pinghandler = PingReceived;
                    if (pinghandler != null)
                    {
                        pinghandler(this, new PingReceivedEventArgs(e.Payload));
                    }
                    break;
            }

            // Catchall:
            EventHandler<MessageReceivedEventArgs> allhandler = MessageReceived;
            if (allhandler != null)
            {
                allhandler(this, new MessageReceivedEventArgs(endpoint, e.Payload));
            }

            // Endpoint-specific
            if (endpointEvents.ContainsKey(endpoint) && endpointEvents[endpoint] != null)
            {
                var handlers = new List<EventHandler<MessageReceivedEventArgs>>(endpointEvents[endpoint]);
                foreach (var h in handlers)
                {
                    if (h != null)
                    {
                        h(this, new MessageReceivedEventArgs(endpoint, e.Payload));
                    }
                }
            }

        }
    }
}
