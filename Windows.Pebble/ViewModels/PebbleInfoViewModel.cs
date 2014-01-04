using System;
using System.Threading.Tasks;
using System.Windows.Threading;
using GalaSoft.MvvmLight;
using flint;
using flint.Responses;

namespace Windows.Pebble.ViewModels
{
    public class PebbleInfoViewModel : ViewModelBase
    {
        private readonly DispatcherTimer _timer = new DispatcherTimer( DispatcherPriority.Background );
        private readonly flint.Pebble _pebble;

        public PebbleInfoViewModel( flint.Pebble pebble )
        {
            _pebble = pebble;
            _timer.Tick += ( sender, e ) => UpdateTimes();
            _timer.Interval = TimeSpan.FromMilliseconds( 500 );
        }

        private bool _IsSelected;
        public bool IsSelected
        {
            get { return _IsSelected; }
            set
            {
                if ( Set( () => IsSelected, ref _IsSelected, value ) )
                {
                    if (value)
                    {
                        _timer.Start();
                        LoadFirmwareAsync();
                    }
                    else
                        _timer.Stop();
                }
            }
        }

        private async Task LoadFirmwareAsync()
        {
            if (_pebble.Alive == false)
                return;

            FirmwareResponse firmwareResponse = await _pebble.GetFirmwareVersionAsync();
            if (firmwareResponse.Success)
            {
                Firmware = firmwareResponse.Firmware;
                RecoveryFirmware = firmwareResponse.RecoveryFirmware;
            }
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

        private async void UpdateTimes()
        {
            if ( _pebble.Alive )
            {
                TimeResponse timeResult = await _pebble.GetTimeAsync();
                if ( timeResult.Success )
                    PebbleTime = timeResult.Time;
            }

            RaisePropertyChanged( () => CurrentTime );
        }
    }
}
