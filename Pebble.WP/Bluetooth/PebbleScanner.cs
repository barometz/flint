using Flint.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Networking.Proximity;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace Pebble.WP.Bluetooth
{
    public static class PebbleScanner
    {
        /// <summary> 
        /// Detect all Pebble bluetooth connections that have been paired with this system.
        /// </summary>
        /// <returns></returns>
        public static async Task<IList<Flint.Core.Pebble>> DetectPebbles()
        {
            // Configure PeerFinder to search for all paired devices.
            PeerFinder.AlternateIdentities["Bluetooth:Paired"] = "";
            return ( await PeerFinder.FindAllPeersAsync() ).Select(
                    x => new Flint.Core.Pebble( new PebbleBluetoothConnection( x ), x.DisplayName ) ).ToList();
        }

        private class PebbleBluetoothConnection : IBluetoothConnection
        {
            private const uint BUFFER_SIZE = 256;

            public event EventHandler<BytesReceivedEventArgs> DataReceived = delegate { };

            private readonly PeerInformation _PeerInformation;
            private readonly StreamSocket _socket = new StreamSocket();
            private DataReader _Reader;

            public PebbleBluetoothConnection( PeerInformation peerInformation )
            {
                if ( peerInformation == null ) throw new ArgumentNullException( "peerInformation" );
                _PeerInformation = peerInformation;
            }

            public async Task OpenAsync()
            {
                try
                {
                    await _socket.ConnectAsync( _PeerInformation.HostName, "1" );
                    _Reader = new DataReader( _socket.InputStream )
                    {
                        ByteOrder = ByteOrder.LittleEndian,
                        InputStreamOptions = InputStreamOptions.Partial
                    };
                }
                catch ( Exception e )
                {
                    Debug.WriteLine( e.ToString() );
                }
            }

            public void Close()
            {
                _socket.Dispose();
                _Reader = null;
            }

            public async void Write( byte[] data )
            {
                await _socket.OutputStream.WriteAsync( data.AsBuffer() );
                uint loaded = await _Reader.LoadAsync( BUFFER_SIZE );
                if ( loaded > 0 )
                {
                    IBuffer buffer = _Reader.ReadBuffer( loaded );
                    if ( buffer.Length > 0 )
                    {
                        DataReceived( this, new BytesReceivedEventArgs( buffer.ToArray() ) );
                    }
                }
            }
        }
    }
}