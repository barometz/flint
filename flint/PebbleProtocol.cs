using System;
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

        public RawMessageReceivedEventArgs(UInt16 endpoint, byte[] payload)
        {
            this.Endpoint = endpoint;
            this.Payload = payload;
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
        public event EventHandler<RawMessageReceivedEventArgs> RawMessageReceived;
        public event SerialErrorReceivedEventHandler SerialErrorReceived;

        public String Port { get; private set; }

        enum waitingStates
        {
            NewMessage,
            Payload
        }
        waitingStates waitingState;

        SerialPort serialPort;
        UInt16 currentPayloadSize = 0;
        UInt16 currentEndpoint = 0;

        /// <summary> Create a new Pebble connection
        /// </summary>
        /// <param name="port"></param>
        public PebbleProtocol(String port)
        {
            this.Port = port;
            this.serialPort = new SerialPort(port, 19200);
            this.serialPort.ReadTimeout = 500;
            this.serialPort.WriteTimeout = 500;

            serialPort.DataReceived += serialPort_DataReceived;
            serialPort.ErrorReceived += serialPort_ErrorReceived; 
        }

        /// <summary>
        /// Connect to the Pebble.
        /// </summary>
        /// <exception cref="System.IO.IOException">Passed on when no connection can be made.</exception>
        public void Connect()
        {
            serialPort.Open(); 
        }

        /// <summary> Send a message to the connected Pebble.  
        /// The payload should at most be 2048 bytes large.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="payload"></param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the payload is too large.</exception>
        public void sendMessage(UInt16 endpoint, byte[] payload)
        {
            if (payload.Length > 2048)
            {
                throw new ArgumentOutOfRangeException("payload", 
                    "The payload should not be more than 2048 bytes");
            }

            UInt16 length = Convert.ToUInt16(payload.Length);
            byte[] payloadSize = BitConverter.GetBytes(length);
            byte[] _endpoint = BitConverter.GetBytes(endpoint);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(payloadSize);
                Array.Reverse(_endpoint);
            }
#if DEBUG
            Console.WriteLine("Sending message..");
            Console.WriteLine("\tPLS: " + BitConverter.ToString(payloadSize));
            Console.WriteLine("\tEP:  " + BitConverter.ToString(_endpoint));
            Console.WriteLine("\tPL:  " + BitConverter.ToString(payload));
#endif
            serialPort.Write(payloadSize, 0, 2);
            serialPort.Write(_endpoint, 0, 2);
            serialPort.Write(payload, 0, length);
        }

        void RaiseRawMessageReceived(UInt16 endpoint, byte[] payload)
        {
            var temp = RawMessageReceived;
            if (temp != null) 
            {
                temp(this, new RawMessageReceivedEventArgs(endpoint, payload));
            }
        }

        /// <summary> Serial error handler.  Passes stuff on to the next subscriber.
        /// </summary>
        /// <remarks>
        /// For the possible errors, <see cref="System.IO.Ports.SerialError"/>.  
        /// I figure most if not all will be taken care of by the BT layer.
        /// </remarks>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void serialPort_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            var temp = SerialErrorReceived;
            if (temp != null)
            {
                temp(this, e);
            }
        }

        void serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            // Keep reading until there's no complete chunk to be read.
            while (readAndProcess()) {
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
        bool readAndProcess() 
        {
            byte[] endpoint = new byte[2];
            byte[] payloadSize = new byte[2];
            switch (waitingState) 
            {
                case waitingStates.NewMessage:
                    if (serialPort.BytesToRead >= 4) 
                    {
                        // Read new payload size and endpoint
                        serialPort.Read(payloadSize, 0, 2);
                        serialPort.Read(endpoint, 0, 2);
                        if (BitConverter.IsLittleEndian) 
                        {
                            // Data is transmitted big-endian, so flip.
                            Array.Reverse(payloadSize);
                            Array.Reverse(endpoint);
                        }
                        currentPayloadSize = BitConverter.ToUInt16(payloadSize, 0);
                        currentEndpoint = BitConverter.ToUInt16(endpoint, 0);
#if DEBUG
                        Console.WriteLine("Message metadata received:");
                        Console.WriteLine("\tPLS: " + currentPayloadSize.ToString());
                        Console.WriteLine("\tEP:  " + currentEndpoint.ToString());
#endif
                        waitingState = waitingStates.Payload;
                        return true;
                    }
                    break;
                case waitingStates.Payload:
                    if (serialPort.BytesToRead >= currentPayloadSize) 
                    {
                        // All of the payload's been received, so read it.
                        byte[] buffer = new byte[currentPayloadSize];
                        serialPort.Read(buffer, 0, currentPayloadSize);
                        RaiseRawMessageReceived(currentEndpoint, buffer);
                        // Reset state
                        waitingState = waitingStates.NewMessage;
                        currentEndpoint = 0;
                        currentPayloadSize = 0;
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
