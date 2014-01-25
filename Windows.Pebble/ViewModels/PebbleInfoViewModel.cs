using System;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using GalaSoft.MvvmLight.Command;
using Windows.Pebble.Messages;
using flint;
using flint.Responses;

namespace Windows.Pebble.ViewModels
{
    public class PebbleInfoViewModel : PebbleViewModelBase
    {
        private readonly DispatcherTimer _timer = new DispatcherTimer( DispatcherPriority.Background );
        private readonly RelayCommand _syncTimeCommand;

        private TimeSpan? _PebbleTimeOffset;

        public PebbleInfoViewModel()
        {
            _syncTimeCommand = new RelayCommand(OnSyncTime);

            _timer.Tick += ( sender, e ) => UpdateTimes();
            _timer.Interval = TimeSpan.FromSeconds( 1 );
        }

        public ICommand SyncTimeCommand
        {
            get { return _syncTimeCommand; }
        }

        public DateTime? CurrentTime
        {
            get { return DateTime.Now; }
        }

        private DateTime? _PebbleTime;
        public DateTime? PebbleTime
        {
            get { return _PebbleTime; }
            private set { Set( () => PebbleTime, ref _PebbleTime, value ); }
        }

        private FirmwareVersion _Firmware;
        public FirmwareVersion Firmware
        {
            get { return _Firmware; }
            set { Set( () => Firmware, ref _Firmware, value ); }
        }

        private FirmwareVersion _RecoveryFirmware;
        public FirmwareVersion RecoveryFirmware
        {
            get { return _RecoveryFirmware; }
            set { Set( () => RecoveryFirmware, ref _RecoveryFirmware, value ); }
        }

        protected override async void OnPebbleConnected( PebbleConnected pebbleConnected )
        {
            base.OnPebbleConnected( pebbleConnected );
            await LoadFirmwareAsync();
            await LoadPebbleTimeAsync();
            _timer.Start();
        }

        protected override void OnPebbleDisconnected( PebbleDisconnected pebbleDisconnected )
        {
            base.OnPebbleDisconnected( pebbleDisconnected );
            _timer.Stop();
        }

        private async void OnSyncTime()
        {
            await _pebble.SetTimeAsync(DateTime.Now);
            await LoadPebbleTimeAsync();
        }

        private async Task LoadPebbleTimeAsync()
        {
            if ( _pebble != null && _pebble.Alive )
            {
                TimeResponse timeResult = await _pebble.GetTimeAsync();
                if ( timeResult.Success )
                {
                    _PebbleTimeOffset = timeResult.Time - DateTime.Now;
                }
            }
        }

        private void UpdateTimes()
        {
            if ( _PebbleTimeOffset != null )
                PebbleTime = DateTime.Now + _PebbleTimeOffset.Value;
            RaisePropertyChanged( () => CurrentTime );
        }

        private async Task LoadFirmwareAsync()
        {
            if ( _pebble == null || _pebble.Alive == false )
                return;

            FirmwareResponse firmwareResponse = await _pebble.GetFirmwareVersionAsync();
            if ( firmwareResponse.Success )
            {
                Firmware = firmwareResponse.Firmware;
                RecoveryFirmware = firmwareResponse.RecoveryFirmware;
            }
        }
    }
}
