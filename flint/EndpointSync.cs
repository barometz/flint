using System;
using System.Threading;

namespace flint
{
    /// <summary> Provides an "immediate" return value for requests 
    /// that would otherwise be answered through an event.
    /// </summary>
    /// <remarks>
    /// Seems like there should be a tidier way using System.Threading, but I
    /// don't see it. An important detail is that if the request is created in 
    /// the thread where messages are received, waiting for this reply may/will
    /// block and ruin everything.
    /// </remarks>
    public class EndpointSync<T> where T : MessageReceivedEventArgs
    {
        private readonly Pebble _Pebble;
        private readonly Pebble.Endpoints _Endpoint;
        private readonly ManualResetEvent _mre;

        public T Result { get; private set; }

        public EndpointSync( Pebble pebble, Pebble.Endpoints endpoint )
        {
            _Pebble = pebble;
            _Endpoint = endpoint;
            _mre = new ManualResetEvent( false );
            pebble.RegisterEndpointCallback( endpoint, trigger );
        }

        /// <summary> Block until the request has returned. </summary>
        /// <param name="delay">The minimum delay between checks in milliseconds.</param>
        /// <param name="timeout">The time to wait until giving up entirely, at 
        /// which point a TimeoutException is raised.</param>
        /// <returns></returns>
        public T WaitAndReturn( int timeout = 10000 )
        {
            if ( _mre.WaitOne( timeout ) )
                return Result;
            throw new TimeoutException();
        }

        private void trigger( object sender, MessageReceivedEventArgs e )
        {
            _Pebble.UnregisterEndpointCallback( _Endpoint, trigger );
            Result = (T)Activator.CreateInstance( typeof( T ), _Endpoint, e.Payload );
            _mre.Set();
        }
    }
}
