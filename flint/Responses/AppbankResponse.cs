using System.Collections.Generic;

namespace flint.Responses
{
    [Endpoint(Endpoint.AppManager, 1)]
    public class AppbankResponse : ResponseBase
    {
        public AppBank AppBank { get; private set; }
        public IList<UUID> AppUUIDs { get; private set; }
        public string Message { get; private set; }
        public UUID UUID { get; private set; }

        public AppbankResponseType ResponseType { get; private set; }

        public AppbankResponse()
        {
            AppUUIDs = new List<UUID>();
        }

        protected override void Load( byte[] payload )
        {
            if ( payload == null || payload.Length == 0 )
            {
                SetError( string.Format( "Payload bytes were not set for {0}", GetType().Name ) );
                return;
            }

            ResponseType = Util.GetEnum<AppbankResponseType>( payload[0] );

            switch ( ResponseType )
            {
                case AppbankResponseType.Apps:
                    AppBank = new AppBank( payload );
                    break;
                case AppbankResponseType.AppUUIDs:
                    uint installedApps = Util.GetUInt32( payload, 1 );
                    if ( CheckExpectedSize( payload, (int)( installedApps * 16 + 5 ) ) )
                    {
                        for ( int i = 5; i < payload.Length; i += 16 )
                        {
                            UUID uuid = Util.GetUUID( payload, i );
                            if ( uuid != null )
                                AppUUIDs.Add( uuid );
                        }
                    }
                    break;
                case AppbankResponseType.AppVersionInformation:
                    if ( CheckExpectedSize( payload, 67 ) )
                    {
                        ushort version = Util.GetUInt16( payload, 1 );
                        string name = Util.GetString( payload, 3, 32 );
                        string company = Util.GetString( payload, 35, 32 );
                        //var app = new AppBank.App();
                        //app.Name = name;
                        //app.Company = company;
                        //app.Version = version;
                    }
                    break;
                case AppbankResponseType.AppUUID:
                    if ( CheckExpectedSize( payload, 17 ) )
                    {
                        UUID = Util.GetUUID( payload, 1 );
                    }
                    break;
            }
        }

        private bool CheckExpectedSize( byte[] payload, int expectedSize )
        {
            if ( expectedSize != payload.Length )
            {
                SetError( string.Format( "Received {0} with type {1}. Expected size is {2} bytes but was {3} bytes",
                    GetType().Name, ResponseType, expectedSize, payload.Length ) );
                return false;
            }
            return true;
        }

        public enum AppbankResponseType : byte
        {
            Unknown = 0,
            Apps = 1,
            AppUUIDs = 5,
            AppVersionInformation = 6,
            AppUUID = 7
        }
    }
}