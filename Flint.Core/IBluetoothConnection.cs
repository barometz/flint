using System;

namespace Flint.Core
{
    public interface IBluetoothConnection
    {
        event EventHandler<BytesReceivedEventArgs> DataReceived;
        void Open();
        void Close();
        void Write( byte[] data );
    }
}