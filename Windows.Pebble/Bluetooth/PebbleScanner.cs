using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Management;
using Flint.Core;
using InTheHand.Net.Sockets;

namespace Windows.Pebble.Bluetooth
{
    public class PebbleScanner
    {
        /// <summary> 
        /// Detect all Pebble bluetooth connections that have been paired with this system.
        /// </summary>
        /// <returns></returns>
        public static List<Flint.Core.Pebble> DetectPebbles()
        {
            var client = new BluetoothClient();

            // A list of all BT devices that are paired, in range, and named "Pebble *" 
            var bluetoothDevices = client.DiscoverDevices(20, true, false, false).
                Where(bdi => bdi.DeviceName.StartsWith("Pebble "));

            // A list of all available serial ports with some metadata including the PnP device ID,
            // which in turn contains a BT device address we can search for.
            var portListCollection = (new ManagementObjectSearcher("SELECT * FROM Win32_SerialPort")).Get();
            var portList = new ManagementBaseObject[portListCollection.Count];
            portListCollection.CopyTo(portList, 0);
            
            return (from device in bluetoothDevices
                    from port in portList
                    where ((string)port["PNPDeviceID"]).Contains(device.DeviceAddress.ToString())
                    select new Flint.Core.Pebble(new PebbleBluetoothConnection((string)port["DeviceID"]), device.DeviceName.Substring(7))).ToList();
        }

        private class PebbleBluetoothConnection : IBluetoothConnection
        {
            private readonly SerialPort _SerialPort;
            public event EventHandler<BytesReceivedEventArgs> DataReceived = delegate { };

            public PebbleBluetoothConnection(string port)
            {
                _SerialPort = new SerialPort(port, 19200);
                _SerialPort.ReadTimeout = 500;
                _SerialPort.WriteTimeout = 500;

                _SerialPort.DataReceived += SerialPortOnDataReceived;
            }

            public void Open()
            {
                _SerialPort.Open();
            }

            public void Close()
            {
                _SerialPort.Close();
            }

            public void Write(byte[] data)
            {
                _SerialPort.Write(data, 0, data.Length);
            }

            private void SerialPortOnDataReceived(object sender, SerialDataReceivedEventArgs e)
            {
                int bytesToRead = _SerialPort.BytesToRead;
                if (bytesToRead > 0)
                {
                    var bytes = new byte[bytesToRead];
                    _SerialPort.Read(bytes, 0, bytesToRead);
                    DataReceived(this, new BytesReceivedEventArgs(bytes));
                }
            }
        }
    }
}