using System;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using Flint.Core;
using Flint.Core.Bundles;
using Flint.Core.Responses;
using GalaSoft.MvvmLight.Command;
using Windows.Pebble.Messages;
using Microsoft.Win32;


namespace Windows.Pebble.ViewModels
{
    public class PebbleInfoViewModel : PebbleViewModelBase
    {
        private readonly DispatcherTimer _timer = new DispatcherTimer( DispatcherPriority.Background );
        private readonly RelayCommand _syncTimeCommand;
        private readonly RelayCommand _updateFirmwareCommand;

        private TimeSpan? _PebbleTimeOffset;

        public PebbleInfoViewModel()
        {
            _syncTimeCommand = new RelayCommand(OnSyncTime);
            _updateFirmwareCommand = new RelayCommand(OnUpdateFirmware);

            _timer.Tick += ( sender, e ) => UpdateTimes();
            _timer.Interval = TimeSpan.FromSeconds( 1 );
        }

        public ICommand SyncTimeCommand
        {
            get { return _syncTimeCommand; }
        }

        public ICommand UpdateFirmwareCommand
        {
            get { return _updateFirmwareCommand; }
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

        private async void OnUpdateFirmware()
        {
            var openDialog = new OpenFileDialog
            {
                CheckFileExists = true,
                CheckPathExists = true,
                DefaultExt = "*.pbz",
                Filter = "Pebble Firmware|*.pbz|All Files|*",
                RestoreDirectory = true,
                Title = "Pebble Firmware"
            };
            if (openDialog.ShowDialog() == true)
            {
                var bundle = new FirmwareBundle();
                using (var zip = new Zip.Zip())
                {
                    bundle.Load(openDialog.OpenFile(), zip);
                }

                if (_pebble.IsAlive == false)
                    return;
                await _pebble.InstallFirmwareAsync(bundle);
            }
        }

        private async Task LoadPebbleTimeAsync()
        {
            if ( _pebble != null && _pebble.IsAlive )
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
            if ( _pebble == null || _pebble.IsAlive == false )
                return;

            FirmwareVersionResponse firmwareResponse = await _pebble.GetFirmwareVersionAsync();
            if ( firmwareResponse.Success )
            {
                Firmware = firmwareResponse.Firmware;
                RecoveryFirmware = firmwareResponse.RecoveryFirmware;
            }
        }
    }
}
