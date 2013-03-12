using System;
using flint;

namespace flint_test
{
    /// <summary>
    /// Demonstrates and tests the functionality of the Flint library.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Pebble pebble = new Pebble("COM13");
            // Subscribe to specific events
            pebble.LogReceived += pebble_LogReceived;
            pebble.PingReceived += pebble_PingReceived;
            // Subscribe to an event for a particular endpoint
            pebble.RegisterEndpointCallback(Pebble.Endpoints.PING, pingReceived);

            Console.WriteLine("Hi! Welcome to Flint.  Press hit enter to try a ping.");
            Console.ReadLine();

            try
            {
                pebble.Ping(cookie: 123);
                Console.WriteLine("Pinged :D");
            }
            catch (TimeoutException e)
            {
                Console.WriteLine("Timeout :(");
            }
            Console.ReadLine();
        }

        static void pebble_PingReceived(object sender, PingReceivedEventArgs e)
        {
            Console.WriteLine("Received PING reply: " + e.Cookie.ToString());
        }

        static void pebble_LogReceived(object sender, LogReceivedEventArgs e)
        {
            Console.WriteLine(e);
        }

        static void pingReceived(object sender, MessageReceivedEventArgs e)
        {
            Console.WriteLine("Received a ping through generic endpoint handler");
        }
    }
}
