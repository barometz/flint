using System;
using System.Collections.Generic;
using System.Linq;
using Flint.Core.Dependencies;

namespace Flint.Core
{
    /// <summary>
    ///     Handles the basic protocol structure for Pebble communication.
    ///     Essentially handles the SerialPort and translates the stream to
    ///     endpoint,payload pairs and vv.  Does and should not handle anything
    ///     regarding the *meaning* of that data.
    /// </summary>
    internal class PebbleProtocol
    {
        private readonly IBluetoothPort _BlueToothPort;
        private readonly List<byte> _byteStream = new List<byte>();
        private ushort _CurrentEndpoint;
        private ushort _CurrentPayloadSize;
        private WaitingStates _WaitingState;

        /// <summary> Create a new Pebble connection </summary>
        /// <param name="port"></param>
        public PebbleProtocol( IBluetoothPort port )
        {
            _BlueToothPort = port;

            _BlueToothPort.DataReceived += serialPortDataReceived;
            //TODO: Push this on to the clients.... do we even care if there is an error?
            //_BlueToothPort.ErrorReceived += serialPortErrorReceived;
        }

        public IBluetoothPort Port
        {
            get { return _BlueToothPort; }
        }

        public event EventHandler<RawMessageReceivedEventArgs> RawMessageReceived = delegate { };

        /// <summary> Connect to the Pebble. </summary>
        /// <exception cref="System.IO.IOException">Passed on when no connection can be made.</exception>
        public void Connect()
        {
            _BlueToothPort.Open();
        }

        public void Close()
        {
            _BlueToothPort.Close();
        }

        /// <summary>
        ///     Send a message to the connected Pebble.
        ///     The payload should at most be 2048 bytes large.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="payload"></param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the payload is too large.</exception>
        public void SendMessage( ushort endpoint, byte[] payload )
        {
            if ( payload.Length > 2048 )
            {
                throw new ArgumentOutOfRangeException( "payload",
                                                      "The payload cannot not be more than 2048 bytes" );
            }

            UInt16 length = Convert.ToUInt16( payload.Length );
            byte[] payloadSize = Util.GetBytes( length );
            byte[] endPoint = Util.GetBytes( endpoint );

            //Debug.WriteLine( "Sending message.." );
            //Debug.WriteLine( "\tPLS: " + BitConverter.ToString( payloadSize ) );
            //Debug.WriteLine( "\tEP:  " + BitConverter.ToString( _endPoint ) );
            //Debug.WriteLine( "\tPL:  " + BitConverter.ToString( payload ) );

            _BlueToothPort.Write(Util.CombineArrays(payloadSize, endPoint, payload));
        }
        
        private void serialPortDataReceived( object sender, BytesReceivedEventArgs e )
        {
            lock ( _byteStream )
            {
                _byteStream.AddRange( e.Bytes );
                // Keep reading until there's no complete chunk to be read.
                while ( ReadAndProcessBytes() )
                { }
            }
        }

        /// <summary>
        ///     Read from the serial line if a useful chunk is present.
        /// </summary>
        /// <remarks>
        ///     In this case a "useful chunk" means that either the payload size
        ///     and endpoint of a new message or the complete payload of a message
        ///     are present.
        /// </remarks>
        /// <returns>
        ///     True if there was enough data to read, otherwise false.
        /// </returns>
        private bool ReadAndProcessBytes()
        {
            switch ( _WaitingState )
            {
                case WaitingStates.NewMessage:
                    if ( _byteStream.Count >= 4 )
                    {
                        // Read new payload size and endpoint
                        _CurrentPayloadSize = Util.GetUInt16(ReadBytes(2));
                        _CurrentEndpoint = Util.GetUInt16(ReadBytes(2));

                        //Debug.WriteLine( "Message metadata received:" );
                        //Debug.WriteLine( "\tPLS: " + _CurrentPayloadSize.ToString() );
                        //Debug.WriteLine( "\tEP:  " + _CurrentEndpoint.ToString() );

                        _WaitingState = WaitingStates.Payload;
                        return true;
                    }
                    break;
                case WaitingStates.Payload:
                    if ( _byteStream.Count >= _CurrentPayloadSize )
                    {
                        // All of the payload's been received, so read it.
                        var buffer = ReadBytes(_CurrentPayloadSize);
                        RawMessageReceived( this, new RawMessageReceivedEventArgs( _CurrentEndpoint, buffer ) );
                        // Reset state
                        _WaitingState = WaitingStates.NewMessage;
                        _CurrentEndpoint = 0;
                        _CurrentPayloadSize = 0;
                        return true;
                    }
                    break;
            }
            // If this hasn't returned yet there wasn't anything interesting to read.
            return false;
        }

        private byte[] ReadBytes( int count )
        {
            var rv = _byteStream.Take( count ).ToArray();
            _byteStream.RemoveRange( 0, count );
            return rv;
        }

        private enum WaitingStates
        {
            NewMessage,
            Payload
        }
    }
}