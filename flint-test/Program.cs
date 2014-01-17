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
            menu.Add(() => pebble.NotificationSMSAsync("+3278051200", "It's time.").Wait(), "Send an SMS notification");
            menu.Add(() => pebble.NotificationMailAsync("Your pal", "URGENT NOTICE", "There is a thing you need to do. Urgently.").Wait(),
                "Send an email notification");
            menu.Add(() => pebble.SetNowPlayingAsync("That dude", "That record", "That track").Wait(), "Send some metadata to the music app");
            menu.Add(() => pebble.BadPingAsync().Wait(), "Send a bad ping to trigger a LOGS response");
            menu.Add( () => Console.WriteLine( pebble.GetTimeAsync().Result.Time ), "Get the time from the Pebble" );
            menu.Add(() => pebble.SetTime(DateTime.Now), "Sync Pebble time");
            menu.Add(() => Console.WriteLine(pebble.GetAppbankContentsAsync().Result.AppBank), "Get the contents of the app bank");
            menu.Add(() => DeleteApp(pebble), "Delete an app from the Pebble");
            menu.Add(() => pebble.Disconnect(), "Exit");

            // Subscribe to specific events
            pebble.RegisterCallback<MusicControlResponse>( pebble_MediaControlReceived );
            // Subscribe to an event for a particular endpoint
            pebble.RegisterCallback<PingResponse>(pingReceived);

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
            AppbankInstallResponse ev = await pebble.RemoveAppAsync(result);
            Console.WriteLine(ev.MsgType);
        }

        static void pebble_MediaControlReceived(MusicControlResponse response)
        {
            Console.WriteLine("Received " + response.Command.ToString());
        }

        static void pingReceived(PingResponse pingResponse)
        {
            Console.WriteLine("Received a ping through generic endpoint handler");
        }
    }
}
