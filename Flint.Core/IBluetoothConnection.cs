using System;
using System.Threading.Tasks;

namespace Flint.Core
{
    public interface IBluetoothConnection
    {
        event EventHandler<BytesReceivedEventArgs> DataReceived;
        Task OpenAsync();
        void Close();
        void Write( byte[] data );
    }
}