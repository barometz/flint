using System;
using System.Threading.Tasks;
using System.Windows.Threading;
using GalaSoft.MvvmLight;
using Windows.Pebble.Messages;
using flint;
using flint.Responses;

namespace Windows.Pebble.ViewModels
{
    public class PebbleInfoViewModel : ViewModelBase
    {
        private readonly DispatcherTimer _timer = new DispatcherTimer( DispatcherPriority.Background );
        private flint.Pebble _pebble;

        public PebbleInfoViewModel( )
        {
            _timer.Tick += ( sender, e ) => UpdateTimes();
            _timer.Interval = TimeSpan.FromSeconds( 1 );

            MessengerInstance.Register<PebbleConnected>( this, OnPebbleConnected );
            MessengerInstance.Register<PebbleDisconnected>( this, OnPebbleDisconnected );
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
            set { Set(() => Firmware, ref _Firmware, value); }
        }

        private FirmwareVersion _RecoveryFirmware;
        public FirmwareVersion RecoveryFirmware
        {
            get { return _RecoveryFirmware; }
            set { Set(() => RecoveryFirmware, ref _RecoveryFirmware, value); }
        }

        private async void OnPebbleConnected( PebbleConnected pebbleConnected )
        {
            _pebble = pebbleConnected.Pebble;

            if ( _pebble != null && _pebble.Alive )
            {
                _timer.Start();
                await LoadFirmwareAsync();
            }
        }

        private void OnPebbleDisconnected( PebbleDisconnected pebbleDisconnected )
        {
            if ( pebbleDisconnected.Pebble == _pebble )
            {
                _pebble = null;
                _timer.Stop();
            }
        }

        private async void UpdateTimes()
        {
            if (_pebble != null && _pebble.Alive)
            {
                TimeResponse timeResult = await _pebble.GetTimeAsync();
                if (timeResult.Success)
                    PebbleTime = timeResult.Time;
            }

            RaisePropertyChanged(() => CurrentTime);
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
