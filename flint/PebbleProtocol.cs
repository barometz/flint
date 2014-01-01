using System;
using System.Diagnostics;
using System.IO.Ports;

namespace flint
{
    /// <summary> Args for the event of a message being received. 
    /// Possibly a little excessive as the relevant event will most likely have 
    /// exactly one subscriber, but it's a small effort.
    /// </summary>
    internal class RawMessageReceivedEventArgs : EventArgs
    {
        public UInt16 Endpoint { get; private set; }
        public byte[] Payload { get; private set; }

        public RawMessageReceivedEventArgs( UInt16 endpoint, byte[] payload )
        {
            Endpoint = endpoint;
            Payload = payload;
        }
    }

    /// <summary> Handles the basic protocol structure for Pebble communication.
    /// Essentially handles the SerialPort and translates the stream to 
    /// endpoint,payload pairs and vv.  Does and should not handle anything 
    /// regarding the *meaning* of that data.
    /// </summary>
    internal class PebbleProtocol
    {
        // TODO: Exception handling
        public event EventHandler<RawMessageReceivedEventArgs> RawMessageReceived = delegate { };
        public event SerialErrorReceivedEventHandler SerialErrorReceived = delegate { };

        public string Port { get; private set; }

        private enum WaitingStates
        {
            NewMessage,
            Payload
        }
        private WaitingStates _WaitingState;

        private readonly SerialPort _SerialPort;
        private UInt16 _CurrentPayloadSize;
        private UInt16 _CurrentEndpoint;

        /// <summary> Create a new Pebble connection </summary>
        /// <param name="port"></param>
        public PebbleProtocol( string port )
        {
            Port = port;
            _SerialPort = new SerialPort( port, 19200 );
            _SerialPort.ReadTimeout = 500;
            _SerialPort.WriteTimeout = 500;



            _SerialPort.DataReceived += serialPortDataReceived;
            _SerialPort.ErrorReceived += serialPortErrorReceived;
        }

        /// <summary> Connect to the Pebble. </summary>
        /// <exception cref="System.IO.IOException">Passed on when no connection can be made.</exception>
        public void Connect()
        {
            _SerialPort.Open();
        }

        public void Close()
        {
            _SerialPort.Close();
        }

        /// <summary> Send a message to the connected Pebble.  
        /// The payload should at most be 2048 bytes large.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="payload"></param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the payload is too large.</exception>
        public void SendMessage( ushort endpoint, byte[] payload )
        {
            if ( payload.Length > 2048 )
            {
                throw new ArgumentOutOfRangeException( "payload",
                                                      "The payload should not be more than 2048 bytes" );
            }

            UInt16 length = Convert.ToUInt16( payload.Length );
            byte[] payloadSize = BitConverter.GetBytes( length );
            byte[] _endPoint = BitConverter.GetBytes( endpoint );
            if ( BitConverter.IsLittleEndian )
            {
                Array.Reverse( payloadSize );
                Array.Reverse( _endPoint );
            }
#if DEBUG
            Console.WriteLine( "Sending message.." );
            Console.WriteLine( "\tPLS: " + BitConverter.ToString( payloadSize ) );
            Console.WriteLine( "\tEP:  " + BitConverter.ToString( _endPoint ) );
            Console.WriteLine( "\tPL:  " + BitConverter.ToString( payload ) );
#endif
            _SerialPort.Write( payloadSize, 0, 2 );
            _SerialPort.Write( _endPoint, 0, 2 );
            _SerialPort.Write( payload, 0, length );
        }

        private void RaiseRawMessageReceived( UInt16 endpoint, byte[] payload )
        {
            RawMessageReceived( this, new RawMessageReceivedEventArgs( endpoint, payload ) );
        }

        /// <summary> Serial error handler.  Passes stuff on to the next subscriber.
        /// </summary>
        /// <remarks>
        /// For the possible errors, <see cref="System.IO.Ports.SerialError"/>.  
        /// I figure most if not all will be taken care of by the BT layer.
        /// </remarks>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void serialPortErrorReceived( object sender, SerialErrorReceivedEventArgs e )
        {
            SerialErrorReceived( this, e );
        }

        private void serialPortDataReceived( object sender, SerialDataReceivedEventArgs e )
        {
            // Keep reading until there's no complete chunk to be read.
            while ( readAndProcess() )
            {
            }
        }

        /// <summary> Read from the serial line if a useful chunk is present.
        /// </summary>
        /// <remarks>
        /// In this case a "useful chunk" means that either the payload size 
        /// and endpoint of a new message or the complete payload of a message 
        /// are present.
        /// </remarks>
        /// <returns>
        /// True if there was enough data to read, otherwise false.
        /// </returns>
        private bool readAndProcess()
        {
            var endpoint = new byte[2];
            var payloadSize = new byte[2];
            switch ( _WaitingState )
            {
                case WaitingStates.NewMessage:
                    if ( _SerialPort.BytesToRead >= 4 )
                    {
                        // Read new payload size and endpoint
                        _SerialPort.Read( payloadSize, 0, 2 );
                        _SerialPort.Read( endpoint, 0, 2 );
                        if ( BitConverter.IsLittleEndian )
                        {
                            // Data is transmitted big-endian, so flip.
                            Array.Reverse( payloadSize );
                            Array.Reverse( endpoint );
                        }
                        _CurrentPayloadSize = BitConverter.ToUInt16( payloadSize, 0 );
                        _CurrentEndpoint = BitConverter.ToUInt16( endpoint, 0 );
#if DEBUG
                        Debug.WriteLine( "Message metadata received:" );
                        Debug.WriteLine( "\tPLS: " + _CurrentPayloadSize.ToString() );
                        Debug.WriteLine( "\tEP:  " + _CurrentEndpoint.ToString() );
#endif
                        _WaitingState = WaitingStates.Payload;
                        return true;
                    }
                    break;
                case WaitingStates.Payload:
                    if ( _SerialPort.BytesToRead >= _CurrentPayloadSize )
                    {
                        // All of the payload's been received, so read it.
                        var buffer = new byte[_CurrentPayloadSize];
                        _SerialPort.Read( buffer, 0, _CurrentPayloadSize );
                        RaiseRawMessageReceived( _CurrentEndpoint, buffer );
                        // Reset state
                        _WaitingState = WaitingStates.NewMessage;
                        _CurrentEndpoint = 0;
                        _CurrentPayloadSize = 0;
                        return true;
                    }
                    break;
            }
            // If this hasn't returned yet there wasn't anything interesting to
            // read.
            return false;
        }
    }
}
