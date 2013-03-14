using System;
using flint;
using SharpMenu;

namespace flint_test
{
    /// <summary> Demonstrates and tests the functionality of the Flint library.
    /// </summary>
    class Program
    {
        static Pebble pebble;

        static void Main(string[] args)
        {
            Boolean alive = true;
            SharpMenu<Action> menu = new SharpMenu<Action>();
            menu.Add(() => pebble.Ping(235), "Send the Pebble a ping");
            menu.Add(() => pebble.NotificationSMS("+3278051200", "It's time."), "Send an SMS notification");
            menu.Add(() => pebble.NotificationMail("Your pal", "URGENT NOTICE", "There is a thing you need to do. Urgently."),
                "Send an email notification");
            menu.Add(() => pebble.NowPlaying("That dude", "That record", "That track"), "Send some metadata to the music app");
            menu.Add(() => pebble.BadPing(), "Send a bad ping to trigger a LOGS response");
            menu.Add(() => alive = false, "Exit");

            Console.WriteLine("Welcome to the Flint test environment.  "
                + "Please remain seated and press enter to autodetect a paired Pebble.");
            Console.ReadLine();

            try
            {
                pebble = Pebble.GetPebble();
                pebble.Connect();
            }
            catch (PlatformNotSupportedException e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Press enter to exit.");
                Console.ReadLine();
                return;
            }
            catch (System.IO.IOException e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Press enter to exit.");
                Console.ReadLine();
                return;
            }
            Console.WriteLine("Successfully connected!");
            Console.WriteLine(pebble);

            pebble.MessageReceived += pebble_MessageReceived;
            // Subscribe to specific events
            pebble.LogReceived += pebble_LogReceived;
            pebble.PingReceived += pebble_PingReceived;
            pebble.MediaControlReceived += pebble_MediaControlReceived;
            // Subscribe to an event for a particular endpoint
            pebble.RegisterEndpointCallback(Pebble.Endpoints.PING, pingReceived);

            pebble.GetVersion();
            Console.WriteLine(pebble.Firmware);
            Console.WriteLine(pebble.RecoveryFirmware);

            while (alive)
            {
                menu.WriteMenu();
                menu.Prompt()();
            }
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
