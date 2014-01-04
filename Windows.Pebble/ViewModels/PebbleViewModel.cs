using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Windows.Input;

namespace Windows.Pebble.ViewModels
{
    public class PebbleViewModel : ViewModelBase
    {
        private readonly PebbleInfoViewModel _pebbleInfo;
        private readonly PebbleAppsViewModel _pebbleApps;
        private readonly PebbleTestViewModel _pebbleTest;

        private readonly RelayCommand _toggleConnectionCommand;

        private readonly flint.Pebble _pebble;

        public PebbleViewModel( flint.Pebble pebble )
        {
            if ( pebble == null ) throw new ArgumentNullException( "pebble" );
            _pebble = pebble;

            _pebbleInfo = new PebbleInfoViewModel(_pebble);
            _pebbleApps = new PebbleAppsViewModel(_pebble);
            _pebbleTest = new PebbleTestViewModel(_pebble);

            _toggleConnectionCommand = new RelayCommand( OnToggleConnect );
        }

        public string PebbleId
        {
            get { return _pebble.PebbleID; }
        }

        private bool _IsConnected;
        public bool IsConnected
        {
            get { return _IsConnected; }
            set { Set(() => IsConnected, ref _IsConnected, value); }
        }

        public PebbleInfoViewModel PebbleInfo
        {
            get { return _pebbleInfo; }
        }

        public PebbleAppsViewModel PebbleApps
        {
            get { return _pebbleApps; }
        }

        public PebbleTestViewModel PebbleTest
        {
            get { return _pebbleTest; }
        }

        public ICommand ToggleConnectionCommand
        {
            get { return _toggleConnectionCommand; }
        }

        private async void OnToggleConnect( )
        {
            if (IsConnected)
            {
                _pebble.Disconnect();
                await PebbleInfo.OnDisconnectedAsync();
                await PebbleApps.OnDisconnectedAsync();
            }
            else
            {
                _pebble.Connect();
                await PebbleInfo.OnConnectedAsync();
                await PebbleApps.OnConnectedAsync();
            }
            IsConnected = !IsConnected;
        }
    }
}