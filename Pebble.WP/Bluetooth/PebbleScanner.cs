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
using Pebble.WP.Utilities;

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
            public event EventHandler<BytesReceivedEventArgs> DataReceived = delegate { };

            private readonly PeerInformation _PeerInformation;
            private readonly StreamSocket _socket = new StreamSocket();
            private StreamWatcher _streamWatcher;

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
                    _streamWatcher = new StreamWatcher( _socket.InputStream );
                    _streamWatcher.DataAvailible += StreamWatcherOnDataAvailible;
                }
                catch ( Exception e )
                {
                    Debug.WriteLine( e.ToString() );
                }
            }

            private void StreamWatcherOnDataAvailible( object sender, DataAvailibleEventArgs e )
            {
                DataReceived( this, new BytesReceivedEventArgs( e.Data ) );
            }

            public void Close()
            {
                _streamWatcher.DataAvailible -= StreamWatcherOnDataAvailible;
                _streamWatcher.Stop();
                _streamWatcher = null;

                _socket.Dispose();
            }

            public async void Write( byte[] data )
            {
                await _socket.OutputStream.WriteAsync( data.AsBuffer() );
            }

            public void Dispose()
            {
                if (_socket != null)
                    _socket.Dispose();                    
            }
        }
    }
}