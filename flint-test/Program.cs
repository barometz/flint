using System;
using flint;

namespace flint_test
{
    /// <summary> Demonstrates and tests the functionality of the Flint library.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Pebble pebble = Pebble.GetPebble();
            try
            {
                pebble.Connect();
            }
            catch (System.IO.IOException)
            {
                Console.WriteLine("Timeout while connecting.  Press enter to exit.");
                Console.ReadLine();
                return;
            }
            Console.WriteLine(pebble);

            pebble.MessageReceived += pebble_MessageReceived;
            // Subscribe to specific events
            pebble.LogReceived += pebble_LogReceived;
            pebble.PingReceived += pebble_PingReceived;
            pebble.MediaControlReceived += pebble_MediaControlReceived;
            // Subscribe to an event for a particular endpoint
            pebble.RegisterEndpointCallback(Pebble.Endpoints.PING, pingReceived);

            Console.WriteLine("Hi! Welcome to Flint.  Press enter to try a notification");
            Console.ReadLine();
            pebble.NotificationMail("Your pal", "URGENT NOTICE", "You need to do something!");

            Console.WriteLine("Press enter to try a ping.");
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

            Console.WriteLine("Bad ping to trigger log..");
            Console.ReadLine();
            pebble.BadPing();

            Console.WriteLine("Now playing test.  Hit enter and check the music app.");
            Console.ReadLine();
            pebble.NowPlaying("That dude", "That record", "That track");

            Console.ReadLine();
        }

        static void pebble_MediaControlReceived(object sender, MediaControlReceivedEventArgs e)
        {
            Console.WriteLine("Received " + e.Command.ToString());
        }

        static void pebble_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            // Method for testing anything.
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
