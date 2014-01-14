using System;
using System.Threading.Tasks;
using System.Windows.Threading;
using Windows.Pebble.Messages;
using flint;
using flint.Responses;

namespace Windows.Pebble.ViewModels
{
    public class PebbleInfoViewModel : PebbleViewModelBase
    {
        private readonly DispatcherTimer _timer = new DispatcherTimer( DispatcherPriority.Background );

        public PebbleInfoViewModel()
        {
            _timer.Tick += ( sender, e ) => UpdateTimes();
            _timer.Interval = TimeSpan.FromSeconds( 1 );
        }

        private DateTime? _PebbleTime;
        public DateTime? PebbleTime
        {
            get { return _PebbleTime; }
            private set { Set( () => PebbleTime, ref _PebbleTime, value ); }
        }

        public DateTime? CurrentTime
        {
            get { return DateTime.Now; }
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
        }

        protected override void OnPebbleDisconnected( PebbleDisconnected pebbleDisconnected )
        {
            base.OnPebbleDisconnected( pebbleDisconnected );
            _timer.Stop();
        }

        private async void UpdateTimes()
        {
            if ( _pebble != null && _pebble.Alive )
            {
                TimeResponse timeResult = await _pebble.GetTimeAsync();
                if ( timeResult.Success )
                    PebbleTime = timeResult.Time;
            }

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
