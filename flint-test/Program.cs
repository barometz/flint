using System;
using flint;

namespace flint_test
{
    class Program
    {
        static void Main(string[] args)
        {
            Pebble pebble = new Pebble("COM13");
            pebble.LogReceived += pebble_LogReceived;
            pebble.PingReceived += pebble_PingReceived;

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
    }
}
