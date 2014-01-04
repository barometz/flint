using flint;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

namespace Windows.Pebble.ViewModels
{
    public class PebbleViewModel : ViewModelBase
    {
        private readonly PebbleInfoViewModel _pebbleInfo;
        private readonly PebbleAppsViewModel _pebbleApps;

        private readonly RelayCommand _toggleConnectionCommand;

        private readonly flint.Pebble _pebble;

        public PebbleViewModel( flint.Pebble pebble )
        {
            if ( pebble == null ) throw new ArgumentNullException( "pebble" );
            _pebble = pebble;

            _pebbleInfo = new PebbleInfoViewModel(_pebble);
            _pebbleApps = new PebbleAppsViewModel(_pebble);

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

        public ICommand ToggleConnectionCommand
        {
            get { return _toggleConnectionCommand; }
        }

        private void OnToggleConnect( )
        {
            if (IsConnected)
                _pebble.Disconnect();
            else
                _pebble.Connect();
            IsConnected = !IsConnected;
        }
    }
}