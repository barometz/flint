using System;

namespace Flint.Core.Dependencies
{
    public interface IBluetoothPort
    {
        event EventHandler<BytesReceivedEventArgs> DataReceived;
        void Open();
        void Close();
        void Write( byte[] data );
    }
}