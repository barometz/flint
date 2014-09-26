using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Storage.Streams;
using Flint.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Networking;
using Windows.Networking.Proximity;
using Windows.Networking.Sockets;

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
            return (await PeerFinder.FindAllPeersAsync()).Select(
                    x => new Flint.Core.Pebble(new PebbleBluetoothConnection(x), x.DisplayName)).ToList();
        }

        private class PebbleBluetoothConnection : IBluetoothConnection
        {
            public event EventHandler<BytesReceivedEventArgs> DataReceived;

            private readonly PeerInformation _PeerInformation;
            private readonly StreamSocket _socket = new StreamSocket();

            public PebbleBluetoothConnection(PeerInformation peerInformation)
            {
                if (peerInformation == null) throw new ArgumentNullException("peerInformation");
                _PeerInformation = peerInformation;
            }

            public async Task OpenAsync()
            {
                try
                {
                    await _socket.ConnectAsync( _PeerInformation.HostName, "1" );

                    Debug.WriteLine("Success");
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.ToString());
                }
            }

            public void Close()
            {
                _socket.Dispose();
            }

            public void Write(byte[] data)
            {
                _socket.OutputStream.WriteAsync(data.AsBuffer());
            }
        }
    }
}