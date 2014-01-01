using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Win32;
using flint;

namespace Windows.Pebble.ViewModels
{
    public class PebbleViewModel : ViewModelBase
    {
        private readonly DispatcherTimer _timer = new DispatcherTimer(DispatcherPriority.Background);
        private readonly List<AppBank.App> _apps = new List<AppBank.App>();

        private readonly RelayCommand<AppBank.App> _removeAppCommand;
        private readonly RelayCommand _installAppCommand; 

        private readonly flint.Pebble _pebble;
        public PebbleViewModel( flint.Pebble pebble )
        {
            if ( pebble == null ) throw new ArgumentNullException( "pebble" );
            _pebble = pebble;

            _removeAppCommand = new RelayCommand<AppBank.App>(OnRemoveApp);
            _installAppCommand = new RelayCommand(OnInstallApp);

            LoadApps();

            _timer.Tick += ( sender, e ) => UpdateTimes();
            _timer.Interval = TimeSpan.FromMilliseconds(500);
            _timer.Start();

            //RemoveAllApps();
            //TestInstall();
        }

        public string PebbleId
        {
            get { return _pebble.PebbleID; }
        }

        public flint.Pebble GetPebble()
        {
            return _pebble;
        }

        private DateTime? _PebbleTime;
        public DateTime? PebbleTime
        {
            get { return _PebbleTime; }
            private set { Set( () => PebbleTime, ref _PebbleTime, value ); }
        }

        public DateTime CurrentTime
        {
            get { return DateTime.Now; }
        }

        public ICollectionView Apps
        {
            get { return CollectionViewSource.GetDefaultView( _apps ); }
        }

        public ICommand RemoveAppCommand
        {
            get { return _removeAppCommand; }
        }

        public ICommand InstallAppCommand
        {
            get { return _installAppCommand; }
        }

        private void UpdateTimes()
        {
            if ( _pebble.Alive == false )
                _pebble.Connect();

            PebbleTime = _pebble.GetTime().Time;

            RaisePropertyChanged( () => CurrentTime );
        }

        private void OnRemoveApp( AppBank.App app )
        {
            if ( _pebble.Alive == false )
                _pebble.Connect();

            _timer.Stop();
            _pebble.RemoveApp(app);
            //LoadApps();
            _timer.Start();
        }

        private void LoadApps()
        {
            if ( _pebble.Alive == false )
                _pebble.Connect();

            var appBankContents = _pebble.GetAppbankContents();
            _apps.Clear();
            _apps.AddRange( appBankContents.AppBank.Apps );
        }


        private void OnInstallApp()
        {
            var openDialog = new OpenFileDialog
                                 {
                                     CheckFileExists = true,
                                     CheckPathExists = true,
                                     DefaultExt = "*.pbw",
                                     Filter = "Pebble Apps|*.pbw",
                                     RestoreDirectory = true,
                                     Title = "Pebble App"
                                 };
            if (openDialog.ShowDialog() == true)
            {
                _timer.Stop();
                var bundle = new PebbleBundle( openDialog.FileName );

                if ( _pebble.Alive == false )
                    _pebble.Connect();
                _pebble.InstallApp( bundle );
                //LoadApps();
                _timer.Start();
            }
        }
    }
}