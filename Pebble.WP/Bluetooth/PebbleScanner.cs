using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Networking.Proximity;
using Flint.Core;

namespace Pebble.WP.Bluetooth
{
    public static class PebbleScanner
    {
        /// <summary> 
        /// Detect all Pebble bluetooth connections that have been paired with this system.
        /// </summary>
        /// <returns></returns>
        public static Task<IList<Flint.Core.Pebble>> DetectPebbles()
        {
            return Task.FromResult<IList<Flint.Core.Pebble>>(new[] { new Flint.Core.Pebble(new PebbleBluetoothConnection(null), "Test") });

            // Configure PeerFinder to search for all paired devices.
            //PeerFinder.AlternateIdentities["Bluetooth:Paired"] = "";
            //return (await PeerFinder.FindAllPeersAsync()).Select(
            //        x => new Flint.Core.Pebble(new PebbleBluetoothConnection(x), x.DisplayName)).ToList();

            // Select a paired device. In this example, just pick the first one.
            //PeerInformation selectedDevice = pairedDevices[0];
            // Attempt a connection
            //StreamSocket socket = new StreamSocket();
            // Make sure ID_CAP_NETWORKING is enabled in your WMAppManifest.xml, or the next 
            // line will throw an Access Denied exception.
            // In this example, the second parameter of the call to ConnectAsync() is the RFCOMM port number, and can range 
            // in value from 1 to 30.
            //await socket.ConnectAsync(selectedDevice.HostName, "1");
        }

        private class PebbleBluetoothConnection : IBluetoothConnection
        {
            public event EventHandler<BytesReceivedEventArgs> DataReceived;

            private readonly PeerInformation _PeerInformation;

            public PebbleBluetoothConnection(PeerInformation peerInformation)
            {
                //if (peerInformation == null) throw new ArgumentNullException("peerInformation");
                _PeerInformation = peerInformation;
            }

            public void Open()
            {
                throw new NotImplementedException();
            }

            public void Close()
            {
                throw new NotImplementedException();
            }

            public void Write(byte[] data)
            {
                throw new NotImplementedException();
            }
        }
    }
}