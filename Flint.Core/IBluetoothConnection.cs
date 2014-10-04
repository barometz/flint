using System;
using System.Threading.Tasks;

namespace Flint.Core
{
    public interface IBluetoothConnection : IDisposable
    {
        event EventHandler<BytesReceivedEventArgs> DataReceived;
        Task OpenAsync();
        void Close();
        void Write( byte[] data );
    }
}