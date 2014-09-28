using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace Pebble.WP.Utilities
{
    public class StreamWatcher
    {
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();
        private readonly DataReader _reader;

        public event EventHandler<DataAvailibleEventArgs> DataAvailible = delegate { };

        public StreamWatcher( IInputStream stream )
        {
            if ( stream == null ) throw new ArgumentNullException( "stream" );

            ReadSize = 256;

            _reader = new DataReader( stream )
            {
                ByteOrder = ByteOrder.LittleEndian,
                InputStreamOptions = InputStreamOptions.Partial
            };

            Task.Factory.StartNew( CheckForData, _tokenSource.Token, TaskCreationOptions.LongRunning,
                TaskScheduler.Default );
        }

        public uint ReadSize { get; set; }

        private async void CheckForData()
        {
            while ( true )
            {
                if ( _tokenSource.IsCancellationRequested )
                    return;

                var loaded = await _reader.LoadAsync( ReadSize );
                if ( loaded > 0 )
                {
                    var bytes = new byte[loaded];
                    _reader.ReadBytes( bytes );
                    DataAvailible( this, new DataAvailibleEventArgs( bytes ) );
                }

                if ( _tokenSource.IsCancellationRequested )
                    return;
                await Task.Delay( 10 );
            }
        }

        public void Stop()
        {
            _tokenSource.Cancel();
        }
    }

    public class DataAvailibleEventArgs : EventArgs
    {
        private readonly byte[] _data;

        public DataAvailibleEventArgs( byte[] data )
        {
            _data = data;
        }

        public byte[] Data
        {
            get { return _data; }
        }
    }
}