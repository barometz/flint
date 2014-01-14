using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using InTheHand.Net.Sockets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using Ionic.Zip;
using flint.Responses;

namespace flint
{
    /// <summary> Represents a (connection to a) Pebble.  
    /// PebbleProtocol is blissfully unaware of the *meaning* of anything, 
    /// all that is handled here.
    /// </summary>
    public class Pebble
    {
        // TODO: Exception handling.

        public enum SessionCaps : uint
        {
            GAMMA_RAY = 0x80000000
        }

        private enum TransferType : byte
        {
            Firmware = 1,
            Recovery = 2,
            SysResources = 3,
            Resources = 4,
            Binary = 5
        }

        /// <summary> Received a full message (any message with complete endpoint and payload) 
        /// from the Pebble.
        /// </summary>
        public event EventHandler<MessageReceivedEventArgs> MessageReceived = delegate { };
        /// <summary> Received a LOGS message from the Pebble. </summary>
        public event EventHandler<LogReceivedEventArgs> LogReceived = delegate { };

        private readonly Dictionary<Type, List<CallbackContainer>> _callbackHandlers;
        private static readonly Dictionary<Endpoints, Type> _endpointToResponseMap;

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

        private readonly PebbleProtocol _PebbleProt;
        private uint _SessionCaps = (uint)SessionCaps.GAMMA_RAY;
        private uint _RemoteCaps = (uint)( RemoteCaps.Telephony | RemoteCaps.SMS | RemoteCaps.Android );

        static Pebble()
        {
            _endpointToResponseMap = new Dictionary<Endpoints, Type>();
            _endpointToResponseMap.Add( Endpoints.AppManager, typeof( AppbankResponse ) );
            _endpointToResponseMap.Add( Endpoints.FirmwareVersion, typeof( FirmwareResponse ) );
            _endpointToResponseMap.Add( Endpoints.Ping, typeof( PingResponse ) );
            _endpointToResponseMap.Add( Endpoints.Time, typeof( TimeResponse ) );
            _endpointToResponseMap.Add( Endpoints.MusicControl, typeof( MusicControlResponse ) );
            _endpointToResponseMap.Add( Endpoints.PhoneVersion, typeof( PhoneVersionResponse ) );
        }

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

            _callbackHandlers = new Dictionary<Type, List<CallbackContainer>>();

            _PebbleProt = new PebbleProtocol( port );
            _PebbleProt.RawMessageReceived += RawMessageReceived;

            //This is received immediately after connecting
            RegisterCallback<PhoneVersionResponse>( PhoneVersionReceived );
            //TODO: when are these called?
            //RegisterEndpointCallback( Endpoints.PhoneVersion, PhoneVersionReceived );
            //RegisterEndpointCallback( Endpoints.Version, VersionReceived );
            //RegisterEndpointCallback( Endpoints.AppManager, AppbankStatusResponseReceived );
        }

        /// <summary> Returns one of the paired Pebbles, or a specific one 
        /// when a four-character ID is provided.  Convenience function for 
        /// when you know there's only one, mostly.
        /// </summary>
        /// <param name="pebbleId"></param>
        /// <returns></returns>
        /// <exception cref="PebbleNotFoundException">When no Pebble or no Pebble of the 
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
            var bluetoothDevices = client.DiscoverDevices( 20, true, false, false ).
                Where( bdi => bdi.DeviceName.StartsWith( "Pebble " ) );

            // A list of all available serial ports with some metadata including the PnP device ID,
            // which in turn contains a BT device address we can search for.
            var portListCollection = ( new ManagementObjectSearcher( "SELECT * FROM Win32_SerialPort" ) ).Get();
            var portList = new ManagementBaseObject[portListCollection.Count];
            portListCollection.CopyTo( portList, 0 );

            // Match bluetooth devices and serial ports, then create Pebbles out of them.
            // Seems like a LINQ join should do this much more cleanly. 

            return ( from device in bluetoothDevices
                     from port in portList
                     where ( (string)port["PNPDeviceID"] ).Contains( device.DeviceAddress.ToString() )
                     select new Pebble( port["DeviceID"] as string, device.DeviceName.Substring( 7 ) ) ).ToList();
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
            }
        }

        public void RegisterCallback<T>( Action<T> callback ) where T : IResponse, new()
        {
            if ( callback == null ) throw new ArgumentNullException( "callback" );

            List<CallbackContainer> callbacks;
            if ( _callbackHandlers.TryGetValue( typeof( T ), out callbacks ) == false )
                _callbackHandlers[typeof( T )] = callbacks = new List<CallbackContainer>();

            callbacks.Add( CallbackContainer.Create( callback ) );
        }

        public bool UnregisterCallback<T>( Action<T> callback ) where T : IResponse
        {
            if ( callback == null ) throw new ArgumentNullException( "callback" );
            List<CallbackContainer> callbacks;
            if ( _callbackHandlers.TryGetValue( typeof( T ), out callbacks ) )
                return callbacks.Remove( callbacks.FirstOrDefault( x => x.IsMatch( callback ) ) );
            return false;
        }

        #region Messages to Pebble

        /// <summary> Send the Pebble a ping. </summary>
        /// <param name="pingData"></param>
        public async Task<PingResponse> PingAsync( uint pingData = 0 )
        {
            // No need to worry about endianness as it's sent back byte for byte anyway.
            byte[] data = BitConverter.GetBytes( pingData );

            data = ConcatByteArray( new byte[] { 0 }, data );

            return await SendMessageAsync<PingResponse>( Endpoints.Ping, data );
        }

        /// <summary> Generic notification support.  Shouldn't have to use this, but feel free. </summary>
        /// <param name="type">Notification type.  So far we've got 0 for mail, 1 for SMS.</param>
        /// <param name="parts">Message parts will be clipped to 255 bytes.</param>
        public void Notification( byte type, params string[] parts )
        {
            //Epoch is Jan 1 1970 00:00:00 UTC
            string timeStamp = ( new DateTime( 1970, 1, 1, 0, 0, 0, DateTimeKind.Utc ) - DateTime.UtcNow ).TotalSeconds.ToString( CultureInfo.InvariantCulture );

            parts = parts.Take( 2 ).Concat( new[] { timeStamp } ).Concat( parts.Skip( 2 ) ).ToArray();

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
            SendMessageAsync( Endpoints.Notification, data );
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
        public async Task<MusicControlResponse> SetNowPlaying( string artist, string album, string track )
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

            return await SendMessageAsync<MusicControlResponse>( Endpoints.MusicControl, data );
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
            SendMessageAsync( Endpoints.Time, data );
        }

        /// <summary> Send a malformed ping (to trigger a LOGS response) </summary>
        public void BadPing()
        {
            byte[] cookie = { 1, 2, 3, 4, 5, 6, 7 };
            SendMessageAsync( Endpoints.Ping, cookie );
        }

        public async Task InstallAppAsync( PebbleBundle bundle, IProgress<ProgressValue> progress = null )
        {
            if ( bundle == null )
                throw new ArgumentNullException( "bundle" );
            if ( bundle.BundleType != PebbleBundle.BundleTypes.Application )
                throw new ArgumentException( "Bundle must be an application" );

            if ( progress != null )
                progress.Report( new ProgressValue( "Removing previous install(s) of the app if they exist", 1 ) );
            var metaData = bundle.AppMetadata;
            var uuid = metaData.UUID;

            AppbankInstallResponse appbankInstallResponse = await RemoveAppByUUID( uuid );
            if ( appbankInstallResponse.Success == false )
                return;

            if ( progress != null )
                progress.Report( new ProgressValue( "Getting current apps", 10 ) );
            AppbankResponse appBankResult = await GetAppbankContentsAsync();

            if ( appBankResult.Success == false )
                throw new PebbleException( "Could not obtain app list; try again" );
            var appBank = appBankResult.AppBank;

            byte firstFreeIndex = 1;
            foreach ( var app in appBank.Apps )
                if ( app.Index == firstFreeIndex )
                    firstFreeIndex++;
            if ( firstFreeIndex == appBank.Size )
                throw new PebbleException( "All app banks are full" );

            if ( progress != null )
                progress.Report( new ProgressValue( "Reading app data", 30 ) );

            var zipFile = ZipFile.Read( bundle.FullPath );
            var appEntry = zipFile.Entries.FirstOrDefault( x => x.FileName == bundle.Manifest.Application.Filename );
            if ( appEntry == null )
                throw new PebbleException( "Could find application file" );

            byte[] appBinary = GetBytes( appEntry );

            if ( progress != null )
                progress.Report( new ProgressValue( "Transferring app to Pebble", 50 ) );

            if ( await PutBytes( appBinary, firstFreeIndex, TransferType.Binary ) == false )
                throw new PebbleException( string.Format( "Failed to send application binary {0}/pebble-app.bin", bundle.FullPath ) );

            if ( bundle.HasResources )
            {
                var resourcesEntry = zipFile.Entries.FirstOrDefault( x => x.FileName == bundle.Manifest.Resources.Filename );
                if ( resourcesEntry == null )
                    throw new PebbleException( "Could not find resource file" );

                byte[] resourcesBinary = GetBytes( resourcesEntry );
                if ( progress != null )
                    progress.Report( new ProgressValue( "Transferring app resources to Pebble", 70 ) );
                if ( await PutBytes( resourcesBinary, firstFreeIndex, TransferType.Resources ) == false )
                    throw new PebbleException( string.Format( "Failed to send application resources {0}/app_resources.pbpack", bundle.FullPath ) );
            }
            if ( progress != null )
                progress.Report( new ProgressValue( "Adding app", 90 ) );
            AddApp( firstFreeIndex );
            if ( progress != null )
                progress.Report( new ProgressValue( "Done", 100 ) );
        }

        #endregion

        #region Requests to send to Pebble

        public async Task<FirmwareResponse> GetFirmwareVersionAsync()
        {
            return await SendMessageAsync<FirmwareResponse>( Endpoints.FirmwareVersion, new byte[] { 0 } );
        }

        /// <summary>
        /// Get the time from the connected Pebble.
        /// </summary>
        /// <param name="async">When true, this returns null immediately.  Otherwise it waits for the event and sends 
        /// the appropriate TimeReceivedEventArgs.</param>
        /// <returns>A TimeReceivedEventArgs with the time, or null.</returns>
        public async Task<TimeResponse> GetTimeAsync()
        {
            return await SendMessageAsync<TimeResponse>( Endpoints.Time, new byte[] { 0 } );
        }

        /// <summary>
        /// Fetch the contents of the Appbank.
        /// </summary>
        /// <param name="async">When true, this returns null immediately.  Otherwise it waits for the event and sends 
        /// the appropriate AppbankContentsReceivedEventArgs.</param>
        /// <returns></returns>
        public async Task<AppbankResponse> GetAppbankContentsAsync()
        {
            return await SendMessageAsync<AppbankResponse>( Endpoints.AppManager, new byte[] { 1 } );
        }

        /// <summary>
        /// Remove an app from the Pebble, using an App instance retrieved from the Appbank.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="async">When true, this returns null immediately.  Otherwise it waits for the event and sends 
        /// the appropriate AppbankInstallMessageEventArgs.</param>
        /// <returns></returns>
        public async Task<AppbankInstallResponse> RemoveAppAsync( AppBank.App app )
        {
            var msg = ConcatByteArray( new byte[] { 2 },
                OrderByteArray( BitConverter.GetBytes( app.ID ) ),
                OrderByteArray( BitConverter.GetBytes( app.Index ) ) );

            return await SendMessageAsync<AppbankInstallResponse>( Endpoints.AppManager, msg );
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
        private Task<byte[]> SendMessageAsync( Endpoints endpoint, byte[] payload )
        {
            return Task.Run( () =>
                                {
                                    var resetEvent = new ManualResetEvent( false );
                                    byte[] result = null;

                                    EventHandler<RawMessageReceivedEventArgs> eventHandler = null;
                                    eventHandler = ( sender, e ) =>
                                                       {
                                                           if ( e.Endpoint == (ushort)endpoint )
                                                           {
                                                               result = e.Payload;
                                                               _PebbleProt.RawMessageReceived -= eventHandler;
                                                               resetEvent.Set();
                                                           }
                                                           else if ( e.Endpoint == (ushort)Endpoints.Logs )
                                                           {
                                                               var args = new LogReceivedEventArgs( e.Payload );
                                                               Debug.WriteLine( "Logs received " + args.ToString() );
                                                           }
                                                       };
                                    try
                                    {
                                        lock ( _PebbleProt )
                                        {
                                            _PebbleProt.RawMessageReceived += eventHandler;
                                            _PebbleProt.SendMessage( (ushort)endpoint, payload );
                                            if ( resetEvent.WaitOne( TimeSpan.FromSeconds( ( 5 ) ) ) )
                                                return result;
                                        }
                                    }
                                    catch ( TimeoutException )
                                    {
                                        Disconnect();
                                    }
                                    return null;
                                } );
        }

        private Task<T> SendMessageAsync<T>( Endpoints endpoint, byte[] payload )
            where T : class, IResponse, new()
        {
            return Task.Run( () =>
            {
                var resetEvent = new ManualResetEvent( false );
                T result = new T();

                EventHandler<RawMessageReceivedEventArgs> eventHandler = null;
                eventHandler = ( sender, e ) =>
                {
                    if ( e.Endpoint == (ushort)endpoint )
                    {
                        result.Load( e.Payload );
                        _PebbleProt.RawMessageReceived -= eventHandler;
                        resetEvent.Set();
                    }
                    else if ( e.Endpoint == (ushort)Endpoints.Logs )
                    {
                        var errorLog = new LogReceivedEventArgs( e.Payload );
                        Debug.WriteLine( "Logs received " + errorLog );
                        result.SetError( errorLog.ToString() );
                        resetEvent.Set();
                    }
                };
                lock ( _PebbleProt )
                {
                    _PebbleProt.RawMessageReceived += eventHandler;
                    _PebbleProt.SendMessage( (ushort)endpoint, payload );
                    if ( resetEvent.WaitOne( TimeSpan.FromSeconds( ( 5 ) ) ) )
                        return result;
                    result.SetError( "Timed out waiting for a response" );
                }
                return result;
            } );
        }

        #region Pebble message event handlers

        private void RawMessageReceived( object sender, RawMessageReceivedEventArgs e )
        {
            Debug.WriteLine( "Received Message for Endpoint: {0}", (Endpoints)e.Endpoint );
            Type responseType;
            if ( _endpointToResponseMap.TryGetValue( (Endpoints)e.Endpoint, out responseType ) )
            {
                //Check for callbacks
                List<CallbackContainer> callbacks;
                if ( _callbackHandlers.TryGetValue( responseType, out callbacks ) )
                {
                    foreach ( var callback in callbacks )
                        callback.Invoke( e.Payload );
                }
            }

            Endpoints endpoint = (Endpoints)e.Endpoint;
            // Switch for the specific events
            switch ( endpoint )
            {
                case Endpoints.Logs:
                    LogReceived( this, new LogReceivedEventArgs( e.Payload ) );
                    break;
                //case Endpoints.Ping:
                //    PingReceived( this, new PingReceivedEventArgs( e.Payload ) );
                //    break;
                //case Endpoints.MusicControl:
                //    MediaControlReceived( this, new MediaControlReceivedEventArgs( e.Payload ) );
                //    break;
            }

            // Catchall:
            MessageReceived( this, new MessageReceivedEventArgs( endpoint, e.Payload ) );
        }

        private void PhoneVersionReceived( PhoneVersionResponse response )
        {
            byte[] prefix = { 0x01, 0xFF, 0xFF, 0xFF, 0xFF };
            byte[] session = BitConverter.GetBytes( _SessionCaps );
            byte[] remote = BitConverter.GetBytes( _RemoteCaps );
            if ( BitConverter.IsLittleEndian )
            {
                Array.Reverse( session );
                Array.Reverse( remote );
            }

            var msg = new byte[0];
            msg = msg.Concat( prefix ).Concat( session ).Concat( remote ).ToArray();
            SendMessageAsync( Endpoints.PhoneVersion, msg );
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

            var msg = new byte[0];
            msg = msg.Concat( prefix ).Concat( session ).Concat( remote ).ToArray();
            SendMessageAsync( Endpoints.PhoneVersion, msg );
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
            byte[] ts = data.Take( 4 ).ToArray();
            if ( BitConverter.IsLittleEndian )
            {
                Array.Reverse( ts );
            }
            DateTime timestamp = Util.TimestampToDateTime( BitConverter.ToInt32( ts, 0 ) );
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

        private async Task<AppbankInstallResponse> RemoveAppByUUID( byte[] uuid )
        {
            byte[] data = ConcatByteArray( new byte[] { 2 }, uuid );
            return await SendMessageAsync<AppbankInstallResponse>( Endpoints.AppManager, data );
        }

        private async Task<bool> PutBytes( byte[] binary, byte index, TransferType transferType )
        {
            byte[] length = OrderByteArray( BitConverter.GetBytes( binary.Length ) );

            //Get token
            var header = ConcatByteArray( new byte[] { 1 }, length, new[] { (byte)transferType, index } );
            byte[] tokenResult = await SendMessageAsync( Endpoints.PutBytes, header );
            if ( tokenResult == null || tokenResult.Length == 0 || tokenResult[0] != 1 )
                return false;
            byte[] token = tokenResult.Skip( 1 ).ToArray();

            const int BUFFER_SIZE = 2000;
            //Send at most 2000 bytes at a time
            for ( int i = 0; i <= binary.Length / BUFFER_SIZE; i++ )
            {
                byte[] data = binary.Skip( BUFFER_SIZE * i ).Take( BUFFER_SIZE ).ToArray();
                var dataHeader = ConcatByteArray( new byte[] { 2 }, token, OrderByteArray( BitConverter.GetBytes( data.Length ) ) );
                byte[] result = await SendMessageAsync( Endpoints.PutBytes, ConcatByteArray( dataHeader, data ) );
                if ( result == null )
                    return false;
            }

            //Send commit message
            uint crc = Crc32.Calculate( binary );
            byte[] crcBytes = OrderByteArray( BitConverter.GetBytes( crc ) );
            byte[] commitMessage = ConcatByteArray( new byte[] { 3 }, token, crcBytes );
            byte[] commitResult = await SendMessageAsync( Endpoints.PutBytes, commitMessage );
            if ( commitResult == null )
                return false;

            //Send complete message
            var completeMessage = ConcatByteArray( new byte[] { 5 }, token );
            byte[] completeResult = await SendMessageAsync( Endpoints.PutBytes, completeMessage );

            return completeResult != null;
        }

        private void AddApp( byte index )
        {
            byte[] indexBytes = OrderByteArray( BitConverter.GetBytes( (uint)index ) );
            var data = ConcatByteArray( new byte[] { 3 }, indexBytes );
            SendMessageAsync( Endpoints.AppManager, data );
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

        private class CallbackContainer
        {
            private readonly IResponse _response;
            private readonly Delegate _delegate;

            private CallbackContainer( Delegate @delegate, IResponse response )
            {
                _delegate = @delegate;
                _response = response;
            }

            public bool IsMatch<T>( Action<T> callback )
            {
                return _delegate == (Delegate)callback;
            }

            public void Invoke( byte[] payload )
            {
                _response.Load( payload );
                _delegate.DynamicInvoke( _response );
            }

            public static CallbackContainer Create<T>( Action<T> callback ) where T : IResponse, new()
            {
                return new CallbackContainer( callback, new T() );
            }
        }
    }
}
