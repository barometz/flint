using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using InTheHand.Net.Sockets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using Ionic.Zip;

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
            Firmware = 1,
            Time = 11,
            Version = 16,
            PhoneVersion = 17,
            SystemMessage = 18,
            MusicControl = 32,
            PhoneControl = 33,
            Logs = 2000,
            Ping = 2001,
            Draw = 2002,
            Reset = 2003,
            Appmfg = 2004,
            Notification = 3000,
            SysReg = 5000,
            FctReg = 5001,
            AppManager = 6000,
            RunKeeper = 7000,
            PutBytes = 48879,
            MaxEndpoint = 65535
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

        private enum TransferType : byte
        {
            Firmware = 1,
            Recovery = 2,
            SysResources = 3,
            Resources = 4,
            Binary = 5
        }

        /// <summary> Occurs when the Pebble (is considered to have) disconnected, 
        /// either by manual disconnect or through a ping timeout.
        /// </summary>
        public event EventHandler OnDisconnect = delegate { };
        /// <summary> Occurs when the serial interface has successfully connected.
        /// Does not guarantee that the connection actually works.
        /// </summary>
        public event EventHandler OnConnect = delegate { };

        /// <summary> Received a full message (any message with complete endpoint and payload) 
        /// from the Pebble.
        /// </summary>
        public event EventHandler<MessageReceivedEventArgs> MessageReceived = delegate { };
        /// <summary> Received a LOGS message from the Pebble. </summary>
        public event EventHandler<LogReceivedEventArgs> LogReceived = delegate { };
        /// <summary> Received a PING message from the Pebble, presumably in response. </summary>
        public event EventHandler<PingReceivedEventArgs> PingReceived = delegate { };
        /// <summary> Received a music control message (next/prev/playpause) from the Pebble. </summary>
        public event EventHandler<MediaControlReceivedEventArgs> MediaControlReceived = delegate { };

        public event EventHandler<AppbankContentsReceivedEventArgs> AppbankContentsReceived = delegate { };
        public event EventHandler<AppbankInstallMessageEventArgs> AppbankInstallMessage = delegate { };
        /// <summary> Holds callbacks for the separate endpoints.  
        /// Saves a lot of typing. There's probably a good reason not to do this.
        /// </summary>
        private readonly Dictionary<Endpoints, EventHandler<MessageReceivedEventArgs>> endpointEvents;

        /// <summary> The four-char ID for the Pebble, based on its BT address. 
        /// </summary>
        public string PebbleID { get; private set; }
        /// <summary> The serial port the Pebble is on. 
        /// </summary>
        public string Port
        {
            get
            {
                return _PebbleProt.Port;
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

        private readonly PebbleProtocol _PebbleProt;
        private uint _SessionCaps = (uint)SessionCaps.GAMMA_RAY;
        private uint _RemoteCaps = (uint)( RemoteCaps.TELEPHONY | RemoteCaps.SMS | RemoteCaps.ANDROID );

        private readonly System.Timers.Timer _PingTimer;



        /// <summary> Create a new Pebble 
        /// </summary>
        /// <param name="port">The serial port to connect to.</param>
        /// <param name="pebbleId">The four-character Pebble ID, based on its BT address.  
        /// Nothing explodes when it's incorrect, it's merely used for identification.</param>
        public Pebble( string port, string pebbleId )
        {
            Alive = false;
            PingTimeout = 10000;
            PebbleID = pebbleId;
            _PebbleProt = new PebbleProtocol( port );
            _PebbleProt.RawMessageReceived += pebbleProtocolRawMessageReceived;

            endpointEvents = new Dictionary<Endpoints, EventHandler<MessageReceivedEventArgs>>();
            RegisterEndpointCallback( Endpoints.PhoneVersion, PhoneVersionReceived );
            RegisterEndpointCallback( Endpoints.Version, VersionReceived );
            RegisterEndpointCallback( Endpoints.AppManager, AppbankStatusResponseReceived );

            _PingTimer = new System.Timers.Timer( 16180 );
            //_PingTimer.Elapsed += pingTimer_Elapsed;
            _PingTimer.Start();
        }

        /// <summary> Returns one of the paired Pebbles, or a specific one 
        /// when a four-character ID is provided.  Convenience function for 
        /// when you know there's only one, mostly.
        /// </summary>
        /// <param name="pebbleId"></param>
        /// <returns></returns>
        /// <exception cref="pebble.PebbleNotFoundException">When no Pebble or no Pebble of the 
        /// specified id was found.</exception>
        public static Pebble GetPebble( string pebbleId = null )
        {
            List<Pebble> pebbleList = DetectPebbles();

            if ( pebbleList.Count == 0 )
            {
                throw new PebbleNotFoundException( "No paired Pebble found." );
            }

            if ( pebbleId == null )
            {
                return pebbleList[0];
            }

            Pebble ret = pebbleList.FirstOrDefault( peb => peb.PebbleID == pebbleId );
            if ( ret == null )
            {
                throw new PebbleNotFoundException( pebbleId );
            }
            return ret;
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
            var btlist = client.DiscoverDevices( 20, true, false, false ).
                Where( bdi => bdi.DeviceName.StartsWith( "Pebble " ) );

            // A list of all available serial ports with some metadata including the PnP device ID,
            // which in turn contains a BT device address we can search for.
            var _portlist = ( new ManagementObjectSearcher( "SELECT * FROM Win32_SerialPort" ) ).Get();
            var portlist = new ManagementObject[_portlist.Count];
            _portlist.CopyTo( portlist, 0 );

            // Match bluetooth devices and serial ports, then create Pebbles out of them.
            // Seems like a LINQ join should do this much more cleanly. 

            return ( from bdi in btlist
                     from port in portlist
                     where ( (string)port["PNPDeviceID"] ).Contains( bdi.DeviceAddress.ToString() )
                     select new Pebble( port["DeviceID"] as string, bdi.DeviceName.Substring( 7 ) ) ).ToList();
        }

        /// <summary> Set the capabilities you want to tell the Pebble about.  
        /// Should be called before connecting.
        /// </summary>
        /// <param name="sessionCap"></param>
        /// <param name="remoteCaps"></param>
        public void SetCaps( uint? sessionCap = null, uint? remoteCaps = null )
        {
            if ( sessionCap != null )
            {
                _SessionCaps = (uint)sessionCap;
            }

            if ( remoteCaps != null )
            {
                _RemoteCaps = (uint)remoteCaps;
            }
        }

        /// <summary> Connect with the Pebble. 
        /// </summary>
        /// <exception cref="System.IO.IOException">Passed on when no connection can be made.</exception>
        public void Connect()
        {
            _PebbleProt.Connect();
            Alive = true;
            OnConnect( this, EventArgs.Empty );
        }

        /// <summary>
        /// Disconnect from the Pebble, if a connection existed.
        /// </summary>
        public void Disconnect()
        {
            try
            {
                _PebbleProt.Close();
            }
            finally
            {
                // If closing the serial port didn't work for some reason we're still effectively 
                // disconnected, although the port will probably be in an invalid state.  Need to 
                // find a good way to handle that.
                Alive = false;
                OnDisconnect( this, EventArgs.Empty );
            }
        }

        /// <summary> Recurring prod to check whether the Pebble is still connected and responding.
        /// </summary>
        /// <remarks>
        /// Ugly hack?  Yes.  It might be possible to do this more properly with the 32feet.NET bt lib.
        /// </remarks>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void pingTimer_Elapsed( object sender, System.Timers.ElapsedEventArgs e )
        {
            if ( Alive )
            {
                byte[] data = { 0 };

                try
                {
                    SendMessage( Endpoints.Time, data );
                    var wait = new EndpointSync<TimeReceivedEventArgs>( this, Endpoints.Time );
                    wait.WaitAndReturn( timeout: PingTimeout );
                }
                catch ( TimeoutException )
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
        public void RegisterEndpointCallback( Endpoints endpoint, EventHandler<MessageReceivedEventArgs> handler )
        {
            if ( handler == null )
            {
                throw new ArgumentNullException( "handler" );
            }
            if ( endpointEvents.ContainsKey( endpoint ) && endpointEvents[endpoint] != null )
            {
                endpointEvents[endpoint] += handler;
            }
            else
            {
                endpointEvents[endpoint] = ( o, m ) => { };
                endpointEvents[endpoint] += handler;
            }
        }

        /// <summary> Deregister a given callback for a given function. </summary>
        /// <param name="endpoint"></param>
        /// <param name="handler"></param>
        public void UnregisterEndpointCallback( Endpoints endpoint, EventHandler<MessageReceivedEventArgs> handler )
        {
            if ( endpointEvents.ContainsKey( endpoint )
                && endpointEvents[endpoint] != null )
            {
                //TODO: Delegate subtraction is not reliable
                endpointEvents[endpoint] -= handler;
            }
        }

        #region Messages to Pebble

        /// <summary> Send the Pebble a ping. </summary>
        /// <param name="cookie"></param>
        /// <param name="async">If true, return null immediately and let the caller wait for a PING event.  If false, 
        /// wait for the reply and return the PingReceivedEventArgs.</param>
        public PingReceivedEventArgs Ping( uint cookie = 0, bool async = false )
        {
            var _cookie = new byte[5];
            // No need to worry about endianness as it's sent back byte for byte anyway.  
            Array.Copy( BitConverter.GetBytes( cookie ), 0, _cookie, 1, 4 );

            SendMessage( Endpoints.Ping, _cookie );
            if ( !async )
            {
                var wait = new EndpointSync<PingReceivedEventArgs>( this, Endpoints.Ping );
                return wait.WaitAndReturn();
            }
            return null;
        }

        /// <summary> Generic notification support.  Shouldn't have to use this, but feel free. </summary>
        /// <param name="type">Notification type.  So far we've got 0 for mail, 1 for SMS.</param>
        /// <param name="parts">Message parts will be clipped to 255 bytes.</param>
        public void Notification( byte type, params string[] parts )
        {
            string[] ts = { ( new DateTime( 1970, 1, 1 ) - DateTime.Now ).TotalSeconds.ToString() };
            parts = parts.Take( 2 ).Concat( ts ).Concat( parts.Skip( 2 ) ).ToArray();
            byte[] data = { type };
            foreach ( string part in parts )
            {
                byte[] partBytes = Encoding.UTF8.GetBytes( part );
                if ( partBytes.Length > 255 )
                {
                    partBytes = partBytes.Take( 255 ).ToArray();
                }
                byte[] len = { Convert.ToByte( partBytes.Length ) };
                data = data.Concat( len ).Concat( partBytes ).ToArray();
            }
            SendMessage( Endpoints.Notification, data );
        }

        /// <summary>
        /// Send an email notification.  The message parts are clipped to 255 bytes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        public void NotificationMail( string sender, string subject, string body )
        {
            Notification( 0, sender, body, subject );
        }

        /// <summary>
        /// Send an SMS notification.  The message parts are clipped to 255 bytes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="body"></param>
        public void NotificationSMS( string sender, string body )
        {
            Notification( 1, sender, body );
        }

        /// <summary> Send "Now playing.." metadata to the Pebble.  
        /// The track, album and artist should each not be longer than 255 bytes.
        /// </summary>
        /// <param name="track"></param>
        /// <param name="album"></param>
        /// <param name="artist"></param>
        public void SetNowPlaying( string artist, string album, string track )
        {
            // No idea what this does.  Do it anyway.
            byte[] data = { 16 };

            byte[] _artist = Encoding.UTF8.GetBytes( artist );
            byte[] _album = Encoding.UTF8.GetBytes( album );
            byte[] _track = Encoding.UTF8.GetBytes( track );
            byte[] artistlen = { (byte)_artist.Length };
            byte[] albumlen = { (byte)_album.Length };
            byte[] tracklen = { (byte)_track.Length };

            data = data.Concat( artistlen ).Concat( _artist ).ToArray();
            data = data.Concat( albumlen ).Concat( _album ).ToArray();
            data = data.Concat( tracklen ).Concat( _track ).ToArray();

            SendMessage( Endpoints.MusicControl, data );
        }

        /// <summary> Set the time on the Pebble. Mostly convenient for syncing. </summary>
        /// <param name="dt">The desired DateTime.  Doesn't care about timezones.</param>
        public void SetTime( DateTime dt )
        {
            byte[] data = { 2 };
            int timestamp = (int)( dt - new DateTime( 1970, 1, 1 ) ).TotalSeconds;
            byte[] _timestamp = BitConverter.GetBytes( timestamp );
            if ( BitConverter.IsLittleEndian )
            {
                Array.Reverse( _timestamp );
            }
            data = data.Concat( _timestamp ).ToArray();
            SendMessage( Endpoints.Time, data );
        }

        /// <summary> Send a malformed ping (to trigger a LOGS response) </summary>
        public void BadPing()
        {
            byte[] cookie = { 1, 2, 3, 4, 5, 6, 7 };
            SendMessage( Endpoints.Ping, cookie );
        }

        public void InstallApp( PebbleBundle bundle )
        {
            if ( bundle == null )
                throw new ArgumentNullException( "bundle" );
            if ( bundle.BundleType != PebbleBundle.BundleTypes.Application )
                throw new ArgumentException( "Bundle must be an application" );

            var metaData = bundle.AppMetadata;
            var uuid = metaData.UUID;
            RemoveAppByUUID( uuid );

            var appBank = GetAppbankContents().AppBank;

            //TODO: null checks
            byte firstFreeIndex = 1;
            foreach ( var app in appBank.Apps )
                if ( app.Index == firstFreeIndex )
                    firstFreeIndex++;
            if ( firstFreeIndex == appBank.Size )
                throw new Exception( "All app banks are full" );

            var zipFile = ZipFile.Read( bundle.FullPath );
            //TODO: Handle nulls and file not found
            var appEntry = zipFile.Entries.First( x => x.FileName == bundle.Manifest.Application.Filename );
            byte[] appBinary = GetBytes( appEntry );



            if ( PutBytes( appBinary, firstFreeIndex, TransferType.Binary ) == false )
                throw new PebbleException( string.Format( "Failed to send application binary {0}/pebble-app.bin", bundle.FullPath ) );

            if ( bundle.HasResources )
            {
                var resourcesEntry = zipFile.Entries.First( x => x.FileName == bundle.Manifest.Resources.Filename );
                byte[] resourcesBinary = GetBytes( resourcesEntry );
                if ( PutBytes( resourcesBinary, firstFreeIndex, TransferType.Resources ) == false )
                    throw new PebbleException( string.Format( "Failed to send application resources {0}/app_resources.pbpack", bundle.FullPath ) );
            }
            AddApp( firstFreeIndex );
        }

        #endregion

        #region Requests to send to Pebble

        /// <summary> Get the Pebble's version info.  </summary>
        /// <param name="async">If true, return immediately.  If false, wait until the response 
        /// has been received.</param>
        public void GetVersion( Boolean async = false )
        {
            byte[] data = { 0 };
            SendMessage( Endpoints.Version, data );
            if ( !async )
            {
                var wait = new EndpointSync<MessageReceivedEventArgs>( this, Endpoints.Version );
                wait.WaitAndReturn( timeout: 5000 );
            }
        }

        /// <summary>
        /// Get the time from the connected Pebble.
        /// </summary>
        /// <param name="async">When true, this returns null immediately.  Otherwise it waits for the event and sends 
        /// the appropriate TimeReceivedEventArgs.</param>
        /// <returns>A TimeReceivedEventArgs with the time, or null.</returns>
        public TimeReceivedEventArgs GetTime( bool async = false )
        {
            byte[] data = { 0 };
            SendMessage( Endpoints.Time, data );
            if ( !async )
            {
                var wait = new EndpointSync<TimeReceivedEventArgs>( this, Endpoints.Time );
                return wait.WaitAndReturn();
            }
            return null;
        }

        /// <summary>
        /// Fetch the contents of the Appbank.
        /// </summary>
        /// <param name="async">When true, this returns null immediately.  Otherwise it waits for the event and sends 
        /// the appropriate AppbankContentsReceivedEventArgs.</param>
        /// <returns></returns>
        public AppbankContentsReceivedEventArgs GetAppbankContents( bool async = false )
        {
            SendMessage( Endpoints.AppManager, new byte[] { 1 } );
            if ( !async )
            {
                var wait = new EndpointSync<AppbankContentsReceivedEventArgs>( this, Endpoints.AppManager );
                return wait.WaitAndReturn();
            }
            return null;
        }

        /// <summary>
        /// Remove an app from the Pebble, using an App instance retrieved from the Appbank.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="async">When true, this returns null immediately.  Otherwise it waits for the event and sends 
        /// the appropriate AppbankInstallMessageEventArgs.</param>
        /// <returns></returns>
        public AppbankInstallMessageEventArgs RemoveApp( AppBank.App app, bool async = false )
        {
            var msg = ConcatByteArray( new byte[] { 2 },
                OrderByteArray( BitConverter.GetBytes( app.ID ) ),
                OrderByteArray( BitConverter.GetBytes( app.Index ) ) );

            SendMessage( Endpoints.AppManager, msg );
            if ( !async )
            {
                var wait = new EndpointSync<AppbankInstallMessageEventArgs>( this, Endpoints.AppManager );
                return wait.WaitAndReturn();
            }
            return null;
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
        private void SendMessage( Endpoints endpoint, byte[] payload )
        {
            try
            {
                _PebbleProt.SendMessage( (ushort)endpoint, payload );
            }
            catch ( TimeoutException e )
            {
                Disconnect();
            }
        }

        #region Pebble message event handlers

        private void pebbleProtocolRawMessageReceived( object sender, RawMessageReceivedEventArgs e )
        {
            Endpoints endpoint = (Endpoints)e.Endpoint;
            // Switch for the specific events
            switch ( endpoint )
            {
                case Endpoints.Logs:
                    LogReceived( this, new LogReceivedEventArgs( e.Payload ) );
                    break;
                case Endpoints.Ping:
                    PingReceived( this, new PingReceivedEventArgs( e.Payload ) );
                    break;
                case Endpoints.MusicControl:
                    MediaControlReceived( this, new MediaControlReceivedEventArgs( e.Payload ) );
                    break;
            }

            // Catchall:
            MessageReceived( this, new MessageReceivedEventArgs( endpoint, e.Payload ) );

            // Endpoint-specific
            if ( endpointEvents.ContainsKey( endpoint ) )
            {
                EventHandler<MessageReceivedEventArgs> h = endpointEvents[endpoint];
                if ( h != null )
                {
                    h( this, new MessageReceivedEventArgs( endpoint, e.Payload ) );
                }
            }
        }

        private void VersionReceived( object sender, MessageReceivedEventArgs e )
        {
            Firmware = ParseVersion( e.Payload.Skip( 1 ).Take( 47 ).ToArray() );
            RecoveryFirmware = ParseVersion( e.Payload.Skip( 48 ).Take( 47 ).ToArray() );
        }

        private void PhoneVersionReceived( object sender, MessageReceivedEventArgs e )
        {
            byte[] prefix = { 0x01, 0xFF, 0xFF, 0xFF, 0xFF };
            byte[] session = BitConverter.GetBytes( _SessionCaps );
            byte[] remote = BitConverter.GetBytes( _RemoteCaps );
            if ( BitConverter.IsLittleEndian )
            {
                Array.Reverse( session );
                Array.Reverse( remote );
            }

            byte[] msg = new byte[0];
            msg = msg.Concat( prefix ).Concat( session ).Concat( remote ).ToArray();
            SendMessage( Endpoints.PhoneVersion, msg );
        }

        private void AppbankStatusResponseReceived( object sender, MessageReceivedEventArgs e )
        {
            switch ( e.Payload[0] )
            {
                case 1:
                    AppbankContentsReceived( this, new AppbankContentsReceivedEventArgs( e.Payload ) );
                    break;
                case 2:
                    AppbankInstallMessage( this, new AppbankInstallMessageEventArgs( e.Payload ) );
                    break;
            }
        }

        #endregion

        #region Utility stuff

        private static FirmwareVersion ParseVersion( byte[] data )
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
            byte[] _ts = data.Take( 4 ).ToArray();
            if ( BitConverter.IsLittleEndian )
            {
                Array.Reverse( _ts );
            }
            DateTime timestamp = Util.TimestampToDateTime( BitConverter.ToInt32( _ts, 0 ) );
            string version = Encoding.UTF8.GetString( data.Skip( 4 ).Take( 32 ).ToArray() );
            string commit = Encoding.UTF8.GetString( data.Skip( 36 ).Take( 8 ).ToArray() );
            version = version.Substring( 0, version.IndexOf( '\0' ) );
            commit = commit.Substring( 0, commit.IndexOf( '\0' ) );
            Boolean isRecovery = BitConverter.ToBoolean( data, 44 );
            byte hardwarePlatform = data[45];
            byte metadataVersion = data[46];
            return new FirmwareVersion( timestamp, version, commit, isRecovery, hardwarePlatform, metadataVersion );
        }

        public override string ToString()
        {
            return string.Format( "Pebble {0} on {1}", PebbleID, Port );
        }

        #endregion

        private void RemoveAppByUUID( byte[] uuid )
        {
            byte[] data = ConcatByteArray( new byte[] { 2 }, uuid );
            SendMessage( Endpoints.AppManager, data );
            var wait = new EndpointSync<AppbankInstallMessageEventArgs>( this, Endpoints.AppManager );
            wait.WaitAndReturn();
        }

        private bool PutBytes( byte[] binary, byte index, TransferType transferType )
        {
            bool success = true;
            byte[] token = null;
            var resetEvent = new ManualResetEvent( false );
            EventHandler<MessageReceivedEventArgs> sendResponseHandler =
                ( sender, e ) =>
                {
                    if ( e.Payload[0] != 1 )
                        success = false;
                    else if ( token == null )
                        token = e.Payload.Skip( 1 ).ToArray();
                    resetEvent.Set();
                };

            RegisterEndpointCallback( Endpoints.PutBytes, sendResponseHandler );
            byte[] length = OrderByteArray( BitConverter.GetBytes( binary.Length ) );

            //Get token
            var header = ConcatByteArray( new byte[] { 1 }, length, new[] { (byte)transferType, index } );
            SendMessage( Endpoints.PutBytes, header );
            resetEvent.WaitOne();
            if ( success == false )
                return false;

            const int BUFFER_SIZE = 2000;
            //Send at most 2000 bytes at a time
            for ( int i = 0; i <= binary.Length / BUFFER_SIZE; i++ )
            {
                byte[] data = binary.Skip( BUFFER_SIZE * i ).Take( BUFFER_SIZE ).ToArray();
                var dataHeader = ConcatByteArray( new byte[] { 2 }, token, OrderByteArray( BitConverter.GetBytes( data.Length ) ) );
                resetEvent.Reset();
                SendMessage( Endpoints.PutBytes, ConcatByteArray( dataHeader, data ) );
                resetEvent.WaitOne();
                if ( success == false )
                    return false;
            }

            //Send commit message
            uint crc = Crc32.Calculate( binary );
            byte[] crcBytes = OrderByteArray( BitConverter.GetBytes( crc ) );
            byte[] commitMessage = ConcatByteArray( new byte[] { 3 }, token, crcBytes );
            resetEvent.Reset();
            SendMessage( Endpoints.PutBytes, commitMessage );
            resetEvent.WaitOne();
            if ( success == false )
                return false;

            //Send complete message
            var completeMessage = ConcatByteArray( new byte[] { 5 }, token );
            resetEvent.Reset();
            SendMessage( Endpoints.PutBytes, completeMessage );
            resetEvent.WaitOne();

            return success;
        }

        private void AddApp( byte index )
        {
            byte[] indexBytes = OrderByteArray( BitConverter.GetBytes( (uint)index ) );
            var data = ConcatByteArray( new byte[] { 3 }, indexBytes );
            SendMessage( Endpoints.AppManager, data );
        }

        private byte[] GetBytes( ZipEntry zipEntry )
        {
            using ( var memoryStream = new MemoryStream() )
            {
                zipEntry.Extract( memoryStream );
                memoryStream.Position = 0;
                return memoryStream.ToArray();
            }
        }

        private static byte[] OrderByteArray( IEnumerable<byte> bytes )
        {
            if ( BitConverter.IsLittleEndian )
                return bytes.Reverse().ToArray();
            return bytes.ToArray();
        }

        public static byte[] ConcatByteArray( params byte[][] array )
        {
            var rv = new byte[array.Select( x => x.Length ).Sum()];

            for ( int i = 0, insertionPoint = 0; i < array.Length; insertionPoint += array[i].Length, i++ )
                Array.Copy( array[i], 0, rv, insertionPoint, array[i].Length );
            return rv;
        }
    }
}
