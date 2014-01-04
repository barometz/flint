using System;
using System.Threading.Tasks;
using flint;
using SharpMenu;
using System.Collections.Generic;
using flint.Responses;

namespace flint_test
{
    /// <summary> Demonstrates and tests the functionality of the Flint library.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Pebble pebble;
            SharpMenu<Action> menu;
            SharpMenu<Pebble> pebblemenu;

            Console.WriteLine("Welcome to the Flint test environment.  "
                + "Please remain seated and press enter to autodetect paired Pebbles.");
            Console.ReadLine();

            try 
            {
                List<Pebble> peblist = Pebble.DetectPebbles();
                switch (peblist.Count)
                {
                    case 0:
                        Console.WriteLine("No Pebbles found.  Press enter to exit.");
                        Console.ReadLine();
                        return;
                    case 1: 
                        pebble = peblist[0];
                        break;
                    default:
                        pebblemenu = new SharpMenu<Pebble>();
                        foreach (Pebble peb in Pebble.DetectPebbles()) 
                        {
                            pebblemenu.Add(peb);
                        }
                        pebblemenu.WriteMenu();
                        pebble = pebblemenu.Prompt();
                        break;
                }
            }
            catch (PlatformNotSupportedException e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Press enter to exit.");
                Console.ReadLine();
                return;
            }
            
            try
            {
                pebble.Connect();
            }
            catch (System.IO.IOException e)
            {
                Console.Write("Connection failed: ");
                Console.WriteLine(e.Message);
                Console.WriteLine("Press enter to exit.");
                Console.ReadLine();
                return;
            }
            Console.WriteLine("Successfully connected!");
            Console.WriteLine(pebble);

            menu = new SharpMenu<Action>();
            menu.Add(() => pebble.PingAsync(235).Wait(), "Send the Pebble a ping");
            menu.Add(() => pebble.NotificationSMS("+3278051200", "It's time."), "Send an SMS notification");
            menu.Add(() => pebble.NotificationMail("Your pal", "URGENT NOTICE", "There is a thing you need to do. Urgently."),
                "Send an email notification");
            menu.Add(() => pebble.SetNowPlaying("That dude", "That record", "That track"), "Send some metadata to the music app");
            menu.Add(() => pebble.BadPing(), "Send a bad ping to trigger a LOGS response");
            menu.Add( () => Console.WriteLine( pebble.GetTimeAsync().Result.Time ), "Get the time from the Pebble" );
            menu.Add(() => pebble.SetTime(DateTime.Now), "Sync Pebble time");
            menu.Add(() => Console.WriteLine(pebble.GetAppbankContentsAsync().Result.AppBank), "Get the contents of the app bank");
            menu.Add(() => DeleteApp(pebble), "Delete an app from the Pebble");
            menu.Add(() => pebble.Disconnect(), "Exit");

            pebble.MessageReceived += pebble_MessageReceived;
            // Subscribe to specific events
            pebble.LogReceived += pebble_LogReceived;
            pebble.MediaControlReceived += pebble_MediaControlReceived;
            // Subscribe to an event for a particular endpoint
            pebble.RegisterEndpointCallback(Pebble.Endpoints.Ping, pingReceived);

            FirmwareResponse firmwareResponse = pebble.GetFirmwareVersionAsync().Result;
            if (firmwareResponse.Success)
            {
                Console.WriteLine(firmwareResponse.Firmware);
                Console.WriteLine(firmwareResponse.RecoveryFirmware);
            }
            while (pebble.Alive)
            {
                menu.WriteMenu();
                Action act = menu.Prompt();
                // To account for disconnects during the prompt:
                if (pebble.Alive) act();
            }
        }

        static async Task DeleteApp(Pebble pebble)
        {
            var applist = (await pebble.GetAppbankContentsAsync()).AppBank.Apps;
            Console.WriteLine("Choose an app to remove");
            AppBank.App result = SharpMenu<AppBank.App>.WriteAndPrompt(applist);
            AppbankInstallMessageEventArgs ev = await pebble.RemoveAppAsync(result);
            Console.WriteLine(ev.MsgType);
        }

        static void pebble_MediaControlReceived(object sender, MediaControlReceivedEventArgs e)
        {
            Console.WriteLine("Received " + e.Command.ToString());
        }

        static void pebble_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            // Method for testing anything.
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
