using Flint.Core.Bundles;
using Flint.Core.Responses;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Flint.Core
{
    /// <summary>
    ///     Represents a (connection to a) Pebble.
    ///     PebbleProtocol is blissfully unaware of the *meaning* of anything,
    ///     all that is handled here.
    /// </summary>
    public class Pebble
    {
        public enum SessionCaps : uint
        {
            GAMMA_RAY = 0x80000000
        }

        public const byte PEBBLE_CLIENT_VERSION = 2;

        private readonly PebbleProtocol _PebbleProt;

        private readonly Dictionary<Type, List<CallbackContainer>> _callbackHandlers;
        private readonly ResponseManager _responseManager = new ResponseManager();
        private uint _RemoteCaps = (uint)(RemoteCaps.Telephony | RemoteCaps.SMS | RemoteCaps.Android);
        private uint _SessionCaps = (uint)SessionCaps.GAMMA_RAY;

        /// <summary>
        ///     Create a new Pebble
        /// </summary>
        /// <param name="connection">The port to use to connect to the pebble</param>
        /// <param name="pebbleId">
        ///     The four-character Pebble ID, based on its BT address.
        ///     Nothing explodes when it's incorrect, it's merely used for identification.
        /// </param>
        public Pebble(IBluetoothConnection connection, string pebbleId)
        {
            ResponseTimeout = TimeSpan.FromSeconds(5);
            PebbleID = pebbleId;

            _callbackHandlers = new Dictionary<Type, List<CallbackContainer>>();

            _PebbleProt = new PebbleProtocol(connection);
            _PebbleProt.RawMessageReceived += RawMessageReceived;
        }

        /// <summary>
        ///     The four-char ID for the Pebble, based on its BT address.
        /// </summary>
        public string PebbleID { get; private set; }

        /// <summary>
        ///     The port the Pebble is on.
        /// </summary>
        public IBluetoothConnection Connection
        {
            get { return _PebbleProt.Connection; }
        }

        public bool IsAlive { get; private set; }
        public TimeSpan ResponseTimeout { get; set; }

        //TODO: This should be pushed to the platform code
        /// <summary>
        ///     Returns one of the paired Pebbles, or a specific one
        ///     when a four-character ID is provided.  Convenience function for
        ///     when you know there's only one, mostly.
        /// </summary>
        /// <param name="pebbleId"></param>
        /// <returns></returns>
        /// <exception cref="PebbleNotFoundException">
        ///     When no Pebble or no Pebble of the
        ///     specified id was found.
        /// </exception>
        //public static Pebble GetPebble( string pebbleId = null )
        //{
        //    List<Pebble> pebbleList = DetectPebbles();
        //
        //    if (pebbleList.Count == 0)
        //    {
        //        throw new PebbleNotFoundException("No paired Pebble found.");
        //    }
        //
        //    if (pebbleId == null)
        //    {
        //        return pebbleList[0];
        //    }
        //
        //    Pebble ret = pebbleList.FirstOrDefault(peb => peb.PebbleID == pebbleId);
        //    if (ret == null)
        //    {
        //        throw new PebbleNotFoundException(pebbleId);
        //    }
        //    return ret;
        //}

        //TODO: This should be pushed to the platform code
        //public static List<Pebble> DetectPebbles()
        //{
        //    var client = new BluetoothClient();
        //    
        //    // A list of all BT devices that are paired, in range, and named "Pebble *" 
        //    var bluetoothDevices = client.DiscoverDevices(20, true, false, false).
        //                                  Where(bdi => bdi.DeviceName.StartsWith("Pebble "));
        //    
        //    // A list of all available serial ports with some metadata including the PnP device ID,
        //    // which in turn contains a BT device address we can search for.
        //    var portListCollection = ( new ManagementObjectSearcher("SELECT * FROM Win32_SerialPort") ).Get();
        //    var portList = new ManagementBaseObject[portListCollection.Count];
        //    portListCollection.CopyTo(portList, 0);
        //    
        //    // Match bluetooth devices and serial ports, then create Pebbles out of them.
        //    // Seems like a LINQ join should do this much more cleanly. 
        //    
        //    return ( from device in bluetoothDevices
        //             from port in portList
        //             where ( (string) port["PNPDeviceID"] ).Contains(device.DeviceAddress.ToString())
        //             select new Pebble(port["DeviceID"] as string, device.DeviceName.Substring(7)) ).ToList();
        //    return null;
        //}

        /// <summary>
        ///     Set the capabilities you want to tell the Pebble about.
        ///     Should be called before connecting.
        /// </summary>
        /// <param name="sessionCap"></param>
        /// <param name="remoteCaps"></param>
        public void SetCaps(uint? sessionCap = null, uint? remoteCaps = null)
        {
            if (sessionCap != null)
            {
                _SessionCaps = (uint)sessionCap;
            }

            if (remoteCaps != null)
            {
                _RemoteCaps = (uint)remoteCaps;
            }
        }

        /// <summary>
        ///     Connect with the Pebble.
        /// </summary>
        /// <exception cref="System.IO.IOException">Passed on when no connection can be made.</exception>
        public async Task ConnectAsync()
        {
            PhoneVersionResponse response;
            //PhoneVersionResponse is received immediately after connecting, and we must respond to it before making any other calls
            using (IResponseTransaction<PhoneVersionResponse> responseTransaction =
                    _responseManager.GetTransaction<PhoneVersionResponse>())
            {
                await _PebbleProt.ConnectAsync();

                response = responseTransaction.AwaitResponse(ResponseTimeout);
            }

            if (response != null)
            {
                byte[] prefix = { PEBBLE_CLIENT_VERSION, 0xFF, 0xFF, 0xFF, 0xFF };
                byte[] session = Util.GetBytes(_SessionCaps);
                byte[] remote = Util.GetBytes(_RemoteCaps);

                byte[] msg = Util.CombineArrays(prefix, session, remote);
                await SendMessageNoResponseAsync(Endpoint.PhoneVersion, msg);
                IsAlive = true;
            }
            else
            {
                Disconnect();
            }
        }

        /// <summary>
        ///     Disconnect from the Pebble, if a connection existed.
        /// </summary>
        public void Disconnect()
        {
            try
            {
                _PebbleProt.Close();
            }
            finally
            {
                // If closing the serial port didn't work for some reason we're still effectively 
                // disconnected, although the port will probably be in an invalid state.  Need to 
                // find a good way to handle that.
                IsAlive = false;
            }
        }

        public void RegisterCallback<T>(Action<T> callback) where T : IResponse, new()
        {
            if (callback == null) throw new ArgumentNullException("callback");

            List<CallbackContainer> callbacks;
            if (_callbackHandlers.TryGetValue(typeof(T), out callbacks) == false)
                _callbackHandlers[typeof(T)] = callbacks = new List<CallbackContainer>();

            callbacks.Add(CallbackContainer.Create(callback));
        }

        public bool UnregisterCallback<T>(Action<T> callback) where T : IResponse
        {
            if (callback == null) throw new ArgumentNullException("callback");
            List<CallbackContainer> callbacks;
            if (_callbackHandlers.TryGetValue(typeof(T), out callbacks))
                return callbacks.Remove(callbacks.FirstOrDefault(x => x.IsMatch(callback)));
            return false;
        }

        /// <summary> Send the Pebble a ping. </summary>
        /// <param name="pingData"></param>
        public async Task<PingResponse> PingAsync(uint pingData = 0)
        {
            // No need to worry about endianness as it's sent back byte for byte anyway.
            byte[] data = Util.CombineArrays(new byte[] { 0 }, Util.GetBytes(pingData));

            return await SendMessageAsync<PingResponse>(Endpoint.Ping, data);
        }

        /// <summary> Generic notification support.  Shouldn't have to use this, but feel free. </summary>
        /// <param name="type">Notification type.  So far we've got 0 for mail, 1 for SMS.</param>
        /// <param name="parts">Message parts will be clipped to 255 bytes.</param>
        private async Task NotificationAsync(byte type, params string[] parts)
        {
            string timeStamp = Util.GetTimestampFromDateTime(DateTime.Now).ToString(CultureInfo.InvariantCulture);

            //TODO: This needs to be refactored
            parts = parts.Take(2).Concat(new[] { timeStamp }).Concat(parts.Skip(2)).ToArray();

            byte[] data = { type };
            data = parts.Aggregate(data, (current, part) => current.Concat(Util.GetBytes(part)).ToArray());
            await SendMessageNoResponseAsync(Endpoint.Notification, data);
        }

        /// <summary>
        ///     Send an email notification.  The message parts are clipped to 255 bytes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        public async Task NotificationMailAsync(string sender, string subject, string body)
        {
            await NotificationAsync(0, sender, body, subject);
        }

        /// <summary>
        ///     Send an SMS notification.  The message parts are clipped to 255 bytes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="body"></param>
        public async Task NotificationSMSAsync(string sender, string body)
        {
            await NotificationAsync(1, sender, body);
        }

        public async Task NotificationFacebookAsync(string sender, string body)
        {
            await NotificationAsync(2, sender, body);
        }

        public async Task NotificationTwitterAsync(string sender, string body)
        {
            await NotificationAsync(3, sender, body);
        }

        /// <summary>
        ///     Send "Now playing.." metadata to the Pebble.
        ///     The track, album and artist should each not be longer than 255 bytes.
        /// </summary>
        /// <param name="track"></param>
        /// <param name="album"></param>
        /// <param name="artist"></param>
        public async Task SetNowPlayingAsync(string artist, string album, string track)
        {
            byte[] artistBytes = Util.GetBytes(artist);
            byte[] albumBytes = Util.GetBytes(album);
            byte[] trackBytes = Util.GetBytes(track);

            byte[] data = Util.CombineArrays(new byte[] { 16 }, artistBytes, albumBytes, trackBytes);

            await SendMessageNoResponseAsync(Endpoint.MusicControl, data);
        }

        /// <summary> Set the time on the Pebble. Mostly convenient for syncing. </summary>
        /// <param name="dateTime">The desired DateTime.  Doesn't care about timezones.</param>
        public async Task SetTimeAsync(DateTime dateTime)
        {
            byte[] timestamp = Util.GetBytes(Util.GetTimestampFromDateTime(dateTime));
            byte[] data = Util.CombineArrays(new byte[] { 2 }, timestamp);
            await SendMessageNoResponseAsync(Endpoint.Time, data);
        }

        /// <summary> Send a malformed ping (to trigger a LOGS response) </summary>
        public async Task<PingResponse> BadPingAsync()
        {
            byte[] cookie = { 1, 2, 3, 4, 5, 6, 7 };
            return await SendMessageAsync<PingResponse>(Endpoint.Ping, cookie);
        }

        public async Task InstallAppAsync(AppBundle bundle, IProgress<ProgressValue> progress = null)
        {
            if (bundle == null)
                throw new ArgumentNullException("bundle");

            if (progress != null)
                progress.Report(new ProgressValue("Removing previous install(s) of the app if they exist", 1));
            ApplicationMetadata metaData = bundle.AppMetadata;
            UUID uuid = metaData.UUID;

            AppbankInstallResponse appbankInstallResponse = await RemoveAppByUUID(uuid);
            if (appbankInstallResponse.Success == false)
                return;

            if (progress != null)
                progress.Report(new ProgressValue("Getting current apps", 20));
            AppbankResponse appBankResult = await GetAppbankContentsAsync();

            if (appBankResult.Success == false)
                throw new PebbleException("Could not obtain app list; try again");
            AppBank appBank = appBankResult.AppBank;

            byte firstFreeIndex = 1;
            foreach (App app in appBank.Apps)
                if (app.Index == firstFreeIndex)
                    firstFreeIndex++;
            if (firstFreeIndex == appBank.Size)
                throw new PebbleException("All app banks are full");

            if (progress != null)
                progress.Report(new ProgressValue("Transferring app to Pebble", 40));

            if (await PutBytes(bundle.App, firstFreeIndex, TransferType.Binary) == false)
                throw new PebbleException("Failed to send application binary pebble-app.bin");

            if (bundle.HasResources)
            {
                if (progress != null)
                    progress.Report(new ProgressValue("Transferring app resources to Pebble", 60));
                if (await PutBytes(bundle.Resources, firstFreeIndex, TransferType.Resources) == false)
                    throw new PebbleException("Failed to send application resources app_resources.pbpack");
            }

            if (progress != null)
                progress.Report(new ProgressValue("Adding app", 80));
            await AddApp(firstFreeIndex);
            if (progress != null)
                progress.Report(new ProgressValue("Done", 100));
        }

        public async Task<bool> InstallFirmwareAsync(FirmwareBundle bundle, IProgress<ProgressValue> progress = null)
        {
            if (bundle == null) throw new ArgumentNullException("bundle");

            if (progress != null)
                progress.Report(new ProgressValue("Starting firmware install", 1));
            if ((await SendSystemMessageAsync(SystemMessage.FirmwareStart)).Success == false)
            {
                return false;
            }

            if (bundle.HasResources)
            {
                if (progress != null)
                    progress.Report(new ProgressValue("Transfering firmware resources", 25));
                if (await PutBytes(bundle.Resources, 0, TransferType.SysResources) == false)
                {
                    return false;
                }
            }

            if (progress != null)
                progress.Report(new ProgressValue("Transfering firmware", 50));
            if (await PutBytes(bundle.Firmware, 0, TransferType.Firmware) == false)
            {
                return false;
            }

            if (progress != null)
                progress.Report(new ProgressValue("Completing firmware install", 75));
            bool success = (await SendSystemMessageAsync(SystemMessage.FirmwareComplete)).Success;
            
            if (progress != null)
                progress.Report(new ProgressValue("Done installing firmware", 100));

            return success;
        }

        public async Task<FirmwareVersionResponse> GetFirmwareVersionAsync()
        {
            return await SendMessageAsync<FirmwareVersionResponse>(Endpoint.FirmwareVersion, new byte[] { 0 });
        }

        /// <summary>
        ///     Get the time from the connected Pebble.
        /// </summary>
        /// <returns>A TimeReceivedEventArgs with the time, or null.</returns>
        public async Task<TimeResponse> GetTimeAsync()
        {
            return await SendMessageAsync<TimeResponse>(Endpoint.Time, new byte[] { 0 });
        }

        /// <summary>
        ///     Fetch the contents of the Appbank.
        /// </summary>
        /// <returns></returns>
        public async Task<AppbankResponse> GetAppbankContentsAsync()
        {
            return await SendMessageAsync<AppbankResponse>(Endpoint.AppManager, new byte[] { 1 });
        }

        /// <summary>
        ///     Remove an app from the Pebble, using an App instance retrieved from the Appbank.
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public async Task<AppbankInstallResponse> RemoveAppAsync(App app)
        {
            byte[] msg = Util.CombineArrays(new byte[] { 2 },
                                            Util.GetBytes(app.ID),
                                            Util.GetBytes(app.Index));

            return await SendMessageAsync<AppbankInstallResponse>(Endpoint.AppManager, msg);
        }

        private async Task<SystemMessageResponse> SendSystemMessageAsync(SystemMessage message)
        {
            byte[] data = { 0, (byte)message };
            return await SendMessageAsync<SystemMessageResponse>(Endpoint.SystemMessage, data);
        }

        private async Task<T> SendMessageAsync<T>(Endpoint endpoint, byte[] payload)
            where T : class, IResponse, new()
        {
            return await Task.Run(() =>
                                      {
                                          try
                                          {
                                              lock (_PebbleProt)
                                              {
                                                  using (
                                                      IResponseTransaction<T> responseTransaction =
                                                          _responseManager.GetTransaction<T>())
                                                  {
                                                      _PebbleProt.SendMessage((ushort)endpoint, payload);
                                                      return responseTransaction.AwaitResponse(ResponseTimeout);
                                                  }
                                              }
                                          }
                                          catch (TimeoutException)
                                          {
                                              var result = new T();
                                              result.SetError("TimeoutException occurred");
                                              Disconnect();
                                              return result;
                                          }
                                          catch (Exception e)
                                          {
                                              var result = new T();
                                              result.SetError(e.Message);
                                              return result;
                                          }
                                      });
        }

        private Task SendMessageNoResponseAsync(Endpoint endpoint, byte[] payload)
        {
            return Task.Run(() =>
                                {
                                    try
                                    {
                                        lock (_PebbleProt)
                                        {
                                            _PebbleProt.SendMessage((ushort)endpoint, payload);
                                        }
                                    }
                                    catch (TimeoutException)
                                    {
                                        Disconnect();
                                    }
                                    catch (Exception)
                                    {
                                    }
                                });
        }

        private void RawMessageReceived(object sender, RawMessageReceivedEventArgs e)
        {
            Debug.WriteLine("Received Message for Endpoint: {0}", (Endpoint)e.Endpoint);

            IResponse response = _responseManager.HandleResponse((Endpoint)e.Endpoint, e.Payload);

            if (response != null)
            {
                //Check for callbacks
                List<CallbackContainer> callbacks;
                if (_callbackHandlers.TryGetValue(response.GetType(), out callbacks))
                {
                    foreach (CallbackContainer callback in callbacks)
                        callback.Invoke(response);
                }
            }
        }

        public override string ToString()
        {
            return string.Format("Pebble {0} on {1}", PebbleID, Connection);
        }

        private async Task<AppbankInstallResponse> RemoveAppByUUID(UUID uuid)
        {
            byte[] data = Util.CombineArrays(new byte[] { 2 }, uuid.Data);
            return await SendMessageAsync<AppbankInstallResponse>(Endpoint.AppManager, data);
        }

        private async Task<bool> PutBytes(byte[] binary, byte index, TransferType transferType)
        {
            byte[] length = Util.GetBytes(binary.Length);

            //Get token
            byte[] header = Util.CombineArrays(new byte[] { 1 }, length, new[] { (byte)transferType, index });

            var rawMessageArgs = await SendMessageAsync<PutBytesResponse>(Endpoint.PutBytes, header);
            if (rawMessageArgs.Success == false)
                return false;

            byte[] tokenResult = rawMessageArgs.Response;
            byte[] token = tokenResult.Skip(1).ToArray();

            const int BUFFER_SIZE = 2000;
            //Send at most 2000 bytes at a time
            for (int i = 0; i <= binary.Length / BUFFER_SIZE; i++)
            {
                byte[] data = binary.Skip(BUFFER_SIZE * i).Take(BUFFER_SIZE).ToArray();
                byte[] dataHeader = Util.CombineArrays(new byte[] { 2 }, token, Util.GetBytes(data.Length));
                var result = await SendMessageAsync<PutBytesResponse>(Endpoint.PutBytes, Util.CombineArrays(dataHeader, data));
                if (result.Success == false)
                {
                    await AbortPutBytesAsync(token);
                    return false;
                }
            }

            //Send commit message
            uint crc = Crc32.Calculate(binary);            
            byte[] crcBytes = Util.GetBytes(crc);
            byte[] commitMessage = Util.CombineArrays(new byte[] { 3 }, token, crcBytes);
            var commitResult = await SendMessageAsync<PutBytesResponse>(Endpoint.PutBytes, commitMessage);
            if (commitResult.Success == false)
            {
                await AbortPutBytesAsync(token);
                return false;
            }


            //Send complete message
            byte[] completeMessage = Util.CombineArrays(new byte[] { 5 }, token);
            var completeResult = await SendMessageAsync<PutBytesResponse>(Endpoint.PutBytes, completeMessage);
            if (completeResult.Success == false)
            {
                await AbortPutBytesAsync(token);
            }
            return completeResult.Success;
        }

        private async Task<PutBytesResponse> AbortPutBytesAsync(byte[] token)
        {
            if (token == null) throw new ArgumentNullException("token");

            byte[] data = Util.CombineArrays(new byte[] {4}, token);

            return await SendMessageAsync<PutBytesResponse>(Endpoint.PutBytes, data);
        }

        private async Task AddApp(byte index)
        {
            byte[] data = Util.CombineArrays(new byte[] { 3 }, Util.GetBytes((uint)index));
            await SendMessageNoResponseAsync(Endpoint.AppManager, data);
        }

        private class CallbackContainer
        {
            private readonly Delegate _delegate;

            private CallbackContainer(Delegate @delegate)
            {
                _delegate = @delegate;
            }

            public bool IsMatch<T>(Action<T> callback)
            {
                return _delegate == (Delegate)callback;
            }

            public void Invoke(IResponse response)
            {
                _delegate.DynamicInvoke(response);
            }

            public static CallbackContainer Create<T>(Action<T> callback) where T : IResponse, new()
            {
                return new CallbackContainer(callback);
            }
        }

        private enum TransferType : byte
        {
            Firmware = 1,
            Recovery = 2,
            SysResources = 3,
            Resources = 4,
            Binary = 5
        }

        private enum SystemMessage : byte
        {
            FirmwareAvailible = 0,
            FirmwareStart = 1,
            FirmwareComplete = 2,
            FirmwareFail = 3,
            FirmwareUpToDate = 4,
            FirmwareOutOfDate = 5,
            BluetoothStartDiscoverable = 6,
            BluetoothEndDiscoverable = 7
        }
    }
}