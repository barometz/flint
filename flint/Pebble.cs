using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;

namespace flint
{
    /// <summary> Represents a (connection to a) Pebble.  
    /// PebbleProtocol is blissfully unaware of the *meaning* of anything, 
    /// all that is handled here.
    /// </summary>
    public class Pebble
    {
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

        public enum MediaControls
        {
            PlayPause = 1,
            Forward = 4,
            Previous = 5,
            // PlayPause also sends 8 for some reason.  To be figured out.
            Other = 8
        }

        /* Capabilities information gratefully taken from 
         * https://github.com/bldewolf/libpebble/commit/ca3c335aef3bdb5914b1b4fcd63701baea9de848
         */

        public enum SessionCaps : uint
        {
            GAMMA_RAY = 0x80000000
        }

        [Flags]
        public enum RemoteCaps : uint
        {
            UNKNOWN = 0,
            IOS = 1,
            ANDROID = 2,
            OSX = 3,
            LINUX = 4,
            WINDOWS = 5,
            TELEPHONY = 16,
            SMS = 32,
            GPS = 64,
            BTLE = 128,
            // 240? No, that doesn't make sense.  But it's apparently true.
            CAMERA_FRONT = 240,
            CAMERA_REAR = 256,
            ACCEL = 512,
            GYRO = 1024,
            COMPASS = 2048
        }

        public event EventHandler<MessageReceivedEventArgs> MessageReceived;
        public event EventHandler<LogReceivedEventArgs> LogReceived;
        public event EventHandler<PingReceivedEventArgs> PingReceived;
        public event EventHandler<MediaControlReceivedEventArgs> MediaControlReceived;
        /// <summary> Holds callbacks for the separate endpoints.  
        /// Saves a lot of typing. There's probably a good reason not to do this.
        /// </summary>
        Dictionary<Endpoints, EventHandler<MessageReceivedEventArgs>> endpointEvents;

        /// <summary> The four-char ID for the Pebble, based on its BT address. 
        /// </summary>
        public String PebbleID { get; private set; }
        /// <summary> The serial port the Pebble is on. 
        /// </summary>
        public String Port
        {
            get
            {
                return pebbleProt.Port;
            }
        }

        PebbleProtocol pebbleProt;
        uint sessionCaps = (uint)SessionCaps.GAMMA_RAY;
        uint remoteCaps = (uint)(RemoteCaps.TELEPHONY | RemoteCaps.SMS | RemoteCaps.ANDROID);

        /// <summary> Create a new Pebble 
        /// </summary>
        /// <param name="port">The serial port to connect to.</param>
        /// <param name="pebbleid">The four-character Pebble ID, based on its BT address.  
        /// Nothing explodes when it's incorrect, it's merely used for identification.</param>
        public Pebble(String port, String pebbleid)
        {
            PebbleID = pebbleid;
            pebbleProt = new PebbleProtocol(port);
            pebbleProt.RawMessageReceived += pebbleProt_RawMessageReceived;

            endpointEvents = new Dictionary<Endpoints, EventHandler<MessageReceivedEventArgs>>();
            RegisterEndpointCallback(Endpoints.PHONE_VERSION, PhoneVersionReceived);
        }

        /// <summary> Returns one of the paired Pebbles, or a specific one 
        /// when a four-character ID is provided.  Convenience function for 
        /// when you know there's only one, mostly.
        /// </summary>
        /// <param name="pebbleid"></param>
        /// <returns></returns>
        /// <exception cref="pebble.PebbleNotFoundException">When no Pebble or no Pebble of the 
        /// specified id was found.</exception>
        public static Pebble GetPebble(String pebbleid = null)
        {
            List<Pebble> peblist = DetectPebbles();

            if (peblist.Count == 0)
            {
                throw new PebbleNotFoundException("No paired Pebble found.");
            }

            if (pebbleid == null)
            {
                return peblist[0];
            }
            else
            {
                Pebble ret = peblist.FirstOrDefault((peb) => peb.PebbleID == pebbleid);
                if (ret == null)
                {
                    throw new PebbleNotFoundException(pebbleid);
                }
                else
                {
                    return ret;
                }
            }
        }

        /// <summary> Detect all Pebbles that have been paired with this system.
        /// Takes a little while because apparently listing all available serial 
        /// ports does.
        /// </summary>
        /// <returns></returns>
        public static List<Pebble> DetectPebbles()
        {
            var client = new BluetoothClient();
            // A list of all BT devices that are paired, in range, and named "Pebble *" 
            var btlist = client.DiscoverDevices(20, true, false, false).
                Where((bdi) => bdi.DeviceName.StartsWith("Pebble "));

            // A list of all available serial ports with some metadata including the PnP device ID,
            // which in turn contains a BT device address we can search for.
            var _portlist = (new ManagementObjectSearcher("SELECT * FROM Win32_SerialPort")).Get();
            var portlist = new ManagementObject[_portlist.Count];
            _portlist.CopyTo(portlist, 0);

            var peblist = new List<Pebble>();

            // Match bluetooth devices and serial ports, then create Pebbles out of them.
            // Seems like a LINQ join should do this much more cleanly. 
            foreach (BluetoothDeviceInfo bdi in btlist)
            {
                foreach (ManagementObject port in portlist)
                {
                    if ((port["PNPDeviceID"] as String).Contains(bdi.DeviceAddress.ToString()))
                    {
                        peblist.Add(new Pebble(port["DeviceID"] as String, bdi.DeviceName.Substring(7)));
                    }
                }
            }

            return peblist;
        }

        /// <summary> Set the capabilities you want to tell the Pebble about.  
        /// Should be called before connecting.
        /// </summary>
        /// <param name="session_cap"></param>
        /// <param name="remote_caps"></param>
        public void SetCaps(uint? session_cap = null, uint? remote_caps = null)
        {
            if (session_cap != null)
            {
                sessionCaps = (uint)session_cap;
            }

            if (remote_caps != null)
            {
                remoteCaps = (uint)remote_caps;
            }
        }

        /// <summary> Connect with the Pebble. 
        /// </summary>
        /// <exception cref="System.IO.IOException">Passed on when no connection can be made.</exception>
        public void Connect()
        {
            pebbleProt.Connect();
        }

        /// <summary> Subscribe to the event of a particular endpoint message 
        /// being received.  This enables subscribing to any endpoint, 
        /// including those that have yet to be discovered.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="handler"></param>
        public void RegisterEndpointCallback(Endpoints endpoint, EventHandler<MessageReceivedEventArgs> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }
            if (endpointEvents.ContainsKey(endpoint) && endpointEvents[endpoint] != null)
            {
                endpointEvents[endpoint] += handler;
            }
            else
            {
                endpointEvents[endpoint] = new EventHandler<MessageReceivedEventArgs>(handler);
            }
        }

        public void DeregisterEndpointCallback(Endpoints endpoint, EventHandler<MessageReceivedEventArgs> handler)
        {
            if (endpointEvents.ContainsKey(endpoint)
                && endpointEvents[endpoint] != null)
            {
                endpointEvents[endpoint] -= handler;
            }
        }

        /** Pebble actions **/

        /// <summary> Send the Pebble a ping. 
        /// </summary>
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

        /// <summary>
        /// Generic notification support.  Shouldn't have to use this, but feel free.
        /// </summary>
        /// <param name="type">Notification type.  So far we've got 0 for mail, 1 for SMS.</param>
        /// <param name="parts">Message parts will be clipped to 255 bytes.</param>
        void Notification(byte type, params String[] parts)
        {
            String[] ts = { (new DateTime(1970, 1, 1) - DateTime.Now).TotalSeconds.ToString() };
            parts = parts.Take(2).Concat(ts).Concat(parts.Skip(2)).ToArray();
            byte[] data = { type };
            foreach (String part in parts)
            {
                byte[] _part = Encoding.UTF8.GetBytes(part);
                if (_part.Length > 255)
                {
                    _part = _part.Take(255).ToArray();
                }
                byte[] len = { Convert.ToByte(_part.Length) };
                data = data.Concat(len).Concat(_part).ToArray();
            }
            pebbleProt.sendMessage((UInt16)Endpoints.NOTIFICATION, data);
        }

        /// <summary>
        /// Send an email notification.  The message parts are clipped to 255 bytes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        public void NotificationMail(String sender, String subject, String body)
        {
            Notification(0, sender, body, subject);
        }

        /// <summary>
        /// Send an SMS notification.  The message parts are clipped to 255 bytes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="body"></param>
        public void NotificationSMS(String sender, String body)
        {
            Notification(1, sender, body);
        }

        /// <summary> Send "Now playing.." metadata to the Pebble.  
        /// The track, album and artist should each not be longer than 255 bytes.
        /// </summary>
        /// <param name="track"></param>
        /// <param name="album"></param>
        /// <param name="artist"></param>
        public void NowPlaying(String artist, String album, String track)
        {
            // No idea what this does.  Do it anyway.
            byte[] data = { 16 };

            byte[] _artist = Encoding.UTF8.GetBytes(artist);
            byte[] _album = Encoding.UTF8.GetBytes(album);
            byte[] _track = Encoding.UTF8.GetBytes(track);
            byte[] artistlen = { (byte)_artist.Length };
            byte[] albumlen = { (byte)_album.Length };
            byte[] tracklen = { (byte)_track.Length };

            data = data.Concat(artistlen).Concat(_artist).ToArray();
            data = data.Concat(albumlen).Concat(_album).ToArray();
            data = data.Concat(tracklen).Concat(_track).ToArray();

            pebbleProt.sendMessage((UInt16)Endpoints.MUSIC_CONTROL, data);
        }

        /// <summary> Send a malformed ping (to trigger a LOGS response)
        /// </summary>
        public void BadPing()
        {
            byte[] cookie = { 1, 2, 3, 4, 5, 6, 7 };
            pebbleProt.sendMessage((UInt16)Endpoints.PING, cookie);
        }

        /** Pebble message event handlers **/

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
                case Endpoints.MUSIC_CONTROL:
                    EventHandler<MediaControlReceivedEventArgs> mediahandler = MediaControlReceived;
                    if (mediahandler != null)
                    {
                        mediahandler(this, new MediaControlReceivedEventArgs(e.Payload));
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
            if (endpointEvents.ContainsKey(endpoint))
            {
                EventHandler<MessageReceivedEventArgs> h = endpointEvents[endpoint];
                if (h != null)
                {
                    h(this, new MessageReceivedEventArgs(endpoint, e.Payload));
                }
            }
        }

        void PhoneVersionReceived(object sender, MessageReceivedEventArgs e)
        {
            byte[] prefix = { 0x01, 0xFF, 0xFF, 0xFF, 0xFF };
            byte[] session = BitConverter.GetBytes(sessionCaps);
            byte[] remote = BitConverter.GetBytes(remoteCaps);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(session);
                Array.Reverse(remote);
            }

            byte[] msg = new byte[0];
            msg = msg.Concat(prefix).Concat(session).Concat(remote).ToArray();
            pebbleProt.sendMessage((ushort)Endpoints.PHONE_VERSION, msg);
        }

        public override string ToString()
        {
            return string.Format("Pebble {0} on {1}", PebbleID, Port);
        }
    }
}
