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

        /// <summary> Endpoints (~"commands") used by Pebble to indicate particular instructions 
        /// or instruction types.
        /// </summary>
        public enum Endpoints : ushort
        {
            FIRMWARE = 1,
            TIME = 11,
            VERSION = 16,
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
            APP_MANAGER = 6000,
            RUNKEEPER = 7000,
            PUT_BYTES = 48879,
            MAX_ENDPOINT = 65535
        }

        /// <summary> Media control instructions as understood by Pebble </summary>
        public enum MediaControls
        {
            PlayPause = 1,
            Forward = 4,
            Previous = 5,
            // PlayPause also sends 8 for some reason.  To be figured out.
            Other = 8
        }

        public enum PutBytesTypes : byte
        {
            Firmware = 1,
            Recovery = 2,
            SystemResources = 3,
            Resources = 4,
            Binary = 5
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

        public class FirmwareVersion
        {
            public DateTime Timestamp { get; private set; }
            public String Version { get; private set; }
            public String Commit { get; private set; }
            public Boolean IsRecovery { get; private set; }
            public byte HardwarePlatform { get; private set; }
            public byte MetadataVersion { get; private set; }

            public FirmwareVersion(DateTime timestamp, String version, String commit,
                bool isrecovery, byte hardwareplatform, byte metadataversion)
            {
                Timestamp = timestamp;
                Version = version;
                Commit = commit;
                IsRecovery = isrecovery;
                HardwarePlatform = hardwareplatform;
                MetadataVersion = metadataversion;
            }

            public override string ToString()
            {
                String format = "Version {0}, commit {1} ({2})\n"
                    + "Recovery:         {3}\n"
                    + "HW Platform:      {4}\n"
                    + "Metadata version: {5}";
                return String.Format(format, Version, Commit, Timestamp, IsRecovery, HardwarePlatform, MetadataVersion);
            }

        }

        /// <summary> Occurs when the Pebble (is considered to have) disconnected, 
        /// either by manual disconnect or through a ping timeout.
        /// </summary>
        public event EventHandler OnDisconnect;
        /// <summary> Occurs when the serial interface has successfully connected.
        /// Does not guarantee that the connection actually works.
        /// </summary>
        public event EventHandler OnConnect;

        /// <summary> Received a full message (any message with complete endpoint and payload) 
        /// from the Pebble.
        /// </summary>
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;
        /// <summary> Received a LOGS message from the Pebble. </summary>
        public event EventHandler<LogReceivedEventArgs> LogReceived;
        /// <summary> Received a PING message from the Pebble, presumably in response. </summary>
        public event EventHandler<PingReceivedEventArgs> PingReceived;
        /// <summary> Received a music control message (next/prev/playpause) from the Pebble. </summary>
        public event EventHandler<MediaControlReceivedEventArgs> MediaControlReceived;

        public event EventHandler<AppbankContentsReceivedEventArgs> AppbankContentsReceived;
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
        public Boolean Alive { get; private set; }
        // Time in ms to wait before considering the recurring ping to be dead
        public int PingTimeout { get; set; }

        /** Pebble version info **/

        /// <summary>The main firmware installed on the Pebble. </summary>
        public FirmwareVersion Firmware { get; private set; }
        /// <summary> The recovery firmware installed on the Pebble. </summary>
        public FirmwareVersion RecoveryFirmware { get; private set; }

        PebbleProtocol pebbleProt;
        uint sessionCaps = (uint)SessionCaps.GAMMA_RAY;
        uint remoteCaps = (uint)(RemoteCaps.TELEPHONY | RemoteCaps.SMS | RemoteCaps.ANDROID);

        System.Timers.Timer pingTimer;

        byte[] putBytesBuffer = { };

        /// <summary> Create a new Pebble 
        /// </summary>
        /// <param name="port">The serial port to connect to.</param>
        /// <param name="pebbleid">The four-character Pebble ID, based on its BT address.  
        /// Nothing explodes when it's incorrect, it's merely used for identification.</param>
        public Pebble(String port, String pebbleid)
        {
            Alive = false;
            PingTimeout = 10000;
            PebbleID = pebbleid;
            pebbleProt = new PebbleProtocol(port);
            pebbleProt.RawMessageReceived += pebbleProt_RawMessageReceived;

            endpointEvents = new Dictionary<Endpoints, EventHandler<MessageReceivedEventArgs>>();
            RegisterEndpointCallback(Endpoints.PHONE_VERSION, PhoneVersionReceived);
            RegisterEndpointCallback(Endpoints.VERSION, VersionReceived);
            RegisterEndpointCallback(Endpoints.APP_MANAGER, AppbankStatusResponseReceived);

            pingTimer = new System.Timers.Timer(16180);
            pingTimer.Elapsed += pingTimer_Elapsed;
            pingTimer.Start();
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
            Alive = true;
            EventHandler onconnect = OnConnect;
            if (onconnect != null)
            {
                onconnect(this, new EventArgs());
            }
        }

        /// <summary>
        /// Disconnect from the Pebble, if a connection existed.
        /// </summary>
        public void Disconnect()
        {
            try
            {
                pebbleProt.Close();
            }
            finally
            {
                // If closing the serial port didn't work for some reason we're still effectively 
                // disconnected, although the port will probably be in an invalid state.  Need to 
                // find a good way to handle that.
                Alive = false;
                EventHandler ondisconnect = OnDisconnect;
                if (ondisconnect != null)
                {
                    ondisconnect(this, new EventArgs());
                }
            }
        }

        /// <summary> Recurring prod to check whether the Pebble is still connected and responding.
        /// </summary>
        /// <remarks>
        /// Ugly hack?  Yes.  It might be possible to do this more properly with the 32feet.NET bt lib.
        /// </remarks>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void pingTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (Alive)
            {
                byte[] data = { 0 };
                
                try
                {
                    sendMessage(Endpoints.TIME, data);
                    var wait = new EndpointSync<TimeReceivedEventArgs>(this, Endpoints.TIME);
                    wait.WaitAndReturn(timeout: PingTimeout);
                }
                catch (TimeoutException)
                {
                    Disconnect();
                }
            }
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
                endpointEvents[endpoint] = new EventHandler<MessageReceivedEventArgs>((o, m) => { });
                endpointEvents[endpoint] += handler;
            }
        }

        /// <summary> Deregister a given callback for a given function. </summary>
        /// <param name="endpoint"></param>
        /// <param name="handler"></param>
        public void DeregisterEndpointCallback(Endpoints endpoint, EventHandler<MessageReceivedEventArgs> handler)
        {
            if (endpointEvents.ContainsKey(endpoint)
                && endpointEvents[endpoint] != null)
            {
                endpointEvents[endpoint] -= handler;
            }
        }

        #region Messages to Pebble

        /// <summary> Send the Pebble a ping. </summary>
        /// <param name="cookie"></param>
        /// <param name="async">If true, return null immediately and let the caller wait for a PING event.  If false, 
        /// wait for the reply and return the PingReceivedEventArgs.</param>
        public PingReceivedEventArgs Ping(UInt32 cookie = 0, Boolean async = false)
        {
            byte[] _cookie = new byte[5];
            // No need to worry about endianness as it's sent back byte for byte anyway.  
            Array.Copy(BitConverter.GetBytes(cookie), 0, _cookie, 1, 4);

            sendMessage(Endpoints.PING, _cookie);
            if (!async)
            {
                var wait = new EndpointSync<PingReceivedEventArgs>(this, Endpoints.PING);
                return wait.WaitAndReturn(timeout: 10000);
            }
            else
            {
                return null;
            }
        }

        /// <summary> Generic notification support.  Shouldn't have to use this, but feel free. </summary>
        /// <param name="type">Notification type.  So far we've got 0 for mail, 1 for SMS.</param>
        /// <param name="parts">Message parts will be clipped to 255 bytes.</param>
        public void Notification(byte type, params String[] parts)
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
            sendMessage(Endpoints.NOTIFICATION, data);
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
        public void SetNowPlaying(String artist, String album, String track)
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

            sendMessage(Endpoints.MUSIC_CONTROL, data);
        }

        /// <summary> Set the time on the Pebble. Mostly convenient for syncing. </summary>
        /// <param name="dt">The desired DateTime.  Doesn't care about timezones.</param>
        public void SetTime(DateTime dt)
        {
            byte[] data = { 2 };
            int timestamp = (int)(dt - new DateTime(1970, 1, 1)).TotalSeconds;
            byte[] _timestamp = BitConverter.GetBytes(timestamp);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(_timestamp);
            }
            data = data.Concat(_timestamp).ToArray();
            sendMessage(Endpoints.TIME, data);
        }

        /// <summary> Send a malformed ping (to trigger a LOGS response) </summary>
        public void BadPing()
        {
            byte[] cookie = { 1, 2, 3, 4, 5, 6, 7 };
            sendMessage(Endpoints.PING, cookie);
        }

        public void PutBytes(byte[] data, PutBytesTypes type)
        {
            if (putBytesBuffer.Count() != 0)
            {
                // Probably not the best way to handle this, should look up mutex locks or somesuch.
                throw new InvalidOperationException("PUTBYTES operation in progress.");
            }
            putBytesBuffer = new byte[data.Count()];
            data.CopyTo(putBytesBuffer, 0);

        }

        #endregion

        #region Requests to send to Pebble

        /// <summary> Get the Pebble's version info.  </summary>
        /// <param name="async">If true, return immediately.  If false, wait until the response 
        /// has been received.</param>
        public void GetVersion(Boolean async = false)
        {
            byte[] data = { 0 };
            sendMessage(Endpoints.VERSION, data);
            if (!async)
            {
                var wait = new EndpointSync<MessageReceivedEventArgs>(this, Endpoints.VERSION);
                wait.WaitAndReturn(timeout: 5000);
            }
        }

        /// <summary>
        /// Get the time from the connected Pebble.
        /// </summary>
        /// <param name="async">When true, this returns null immediately.  Otherwise it waits for the event and sends 
        /// the appropriate TimeReceivedEventArgs.</param>
        /// <returns>A TimeReceivedEventArgs with the time, or null.</returns>
        public TimeReceivedEventArgs GetTime(bool async = false)
        {
            byte[] data = { 0 };
            sendMessage(Endpoints.TIME, data);
            if (!async)
            {
                var wait = new EndpointSync<TimeReceivedEventArgs>(this, Endpoints.TIME);
                return wait.WaitAndReturn();
            }
            else
            {
                return null;
            }
        }

        public AppbankContentsReceivedEventArgs GetAppbankContents(bool async = false)
        {
            sendMessage(Endpoints.APP_MANAGER, new byte[] { 1 });
            if (!async)
            {
                var wait = new EndpointSync<AppbankContentsReceivedEventArgs>(this, Endpoints.APP_MANAGER);
                return wait.WaitAndReturn();
            }
            else
            {
                return null;
            }
        }

        #endregion

        /// <summary> Send a message to the connected Pebble.  
        /// The payload should at most be 2048 bytes large.
        /// </summary>
        /// <remarks>
        /// Yes, the docs at developers.getpebble.com say 4 kB.  I've received some errors from the Pebble that indicated 2 kB
        /// and that's what I'll assume for the time being.
        /// </remarks>
        /// <param name="endpoint"></param>
        /// <param name="payload"></param>
        /// <exception cref="ArgumentOutOfRangeException">Passed on when the payload is too large.</exception>
        void sendMessage(Endpoints endpoint, byte[] payload)
        {
            try
            {
                pebbleProt.sendMessage((ushort)endpoint, payload);
            }
            catch (TimeoutException e)
            {
                Disconnect();
            }
        }

        #region Pebble message event handlers

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

        void VersionReceived(object sender, MessageReceivedEventArgs e)
        {
            this.Firmware = Pebble.ParseVersion(e.Payload.Skip(1).Take(47).ToArray());
            this.RecoveryFirmware = Pebble.ParseVersion(e.Payload.Skip(48).Take(47).ToArray());
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
            sendMessage(Endpoints.PHONE_VERSION, msg);
        }

        private void AppbankStatusResponseReceived(object sender, MessageReceivedEventArgs e)
        {
            if (e.Payload[0] == 1)
            {
                EventHandler<AppbankContentsReceivedEventArgs> h = AppbankContentsReceived;
                if (h != null)
                {
                    h(this, new AppbankContentsReceivedEventArgs(e.Payload));
                }
            }
        }

        #endregion

        #region Utility stuff

        static FirmwareVersion ParseVersion(byte[] data)
        {
            /*
             * The layout of the version info is:
             *  0: 3 Timestamp (int32)
             *  4:35 Version (string, padded with \0)
             * 36:43 Commit (hash?) (string, padded with \0)
             * 44:   Is recovery? (Boolean)
             * 45:   HW Platform (byte)
             * 46:   Metadata version (byte)
             */
            byte[] _ts = data.Take(4).ToArray();
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(_ts);
            }
            DateTime timestamp = Util.TimestampToDateTime(BitConverter.ToInt32(_ts, 0));
            String version = Encoding.UTF8.GetString(data.Skip(4).Take(32).ToArray());
            String commit = Encoding.UTF8.GetString(data.Skip(36).Take(8).ToArray());
            version = version.Substring(0, version.IndexOf('\0'));
            commit = commit.Substring(0, commit.IndexOf('\0'));
            Boolean is_recovery = BitConverter.ToBoolean(data, 44);
            byte hardware_platform = data[45];
            byte metadata_ver = data[46];
            return new FirmwareVersion(timestamp, version, commit, is_recovery, hardware_platform, metadata_ver);
        }

        public override string ToString()
        {
            return string.Format("Pebble {0} on {1}", PebbleID, Port);
        }

        #endregion
    }
}
