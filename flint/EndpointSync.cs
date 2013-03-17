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
        Pebble pebble;
        Pebble.Endpoints endpoint;

        public T Result { get; private set; }
        public Boolean Triggered { get; private set; }

        public EndpointSync(Pebble pebble, Pebble.Endpoints endpoint) 
        {
            this.pebble = pebble;
            this.endpoint = endpoint;
            Triggered = false;
            pebble.RegisterEndpointCallback(endpoint, trigger);
        }

        /// <summary> Block until the request has returned. </summary>
        /// <param name="delay">The minimum delay between checks in milliseconds.</param>
        /// <param name="timeout">The time to wait until giving up entirely, at 
        /// which point a TimeoutException is raised.</param>
        /// <returns></returns>
        public T WaitAndReturn(int delay = 15, int timeout = 10000)
        {
            DateTime start = DateTime.Now;
            while (!this.Triggered)
            {
                if ((DateTime.Now - start).TotalMilliseconds > timeout)
                {
                    throw new TimeoutException();
                }
                Thread.Sleep(delay);
            }
            return Result;
        }

        void trigger(object sender, MessageReceivedEventArgs e)
        {
            pebble.DeregisterEndpointCallback(endpoint, trigger);
            Result = (T)Activator.CreateInstance(typeof(T), endpoint, e.Payload);
            Triggered = true;
        }
    }
}
