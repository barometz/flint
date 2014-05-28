using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using Flint.Core;
using GalaSoft.MvvmLight.Command;
using Microsoft.Win32;
using Windows.Pebble.Messages;

namespace Windows.Pebble.ViewModels
{
    public class PebbleAppsViewModel : PebbleViewModelBase
    {
        private readonly BindingList<Flint.Core.App> _apps = new BindingList<Flint.Core.App>();

        private readonly RelayCommand<Flint.Core.App> _removeAppCommand;
        private readonly RelayCommand _installAppCommand;

        public PebbleAppsViewModel()
        {
            _removeAppCommand = new RelayCommand<Flint.Core.App>( OnRemoveApp );
            _installAppCommand = new RelayCommand( OnInstallApp );
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

        private bool _Loading;
        public bool Loading
        {
            get { return _Loading; }
            set { Set( () => Loading, ref _Loading, value ); }
        }

        protected override async void OnPebbleConnected( PebbleConnected pebbleConnected )
        {
            base.OnPebbleConnected(pebbleConnected);

            await LoadAppsAsync();
        }

        private async Task LoadAppsAsync()
        {
            if ( _pebble == null || _pebble.Alive == false )
                return;

            Loading = true;
            var appBankContents = await _pebble.GetAppbankContentsAsync();
            _apps.Clear();
            if ( appBankContents.Success )
                foreach ( var app in appBankContents.AppBank.Apps )
                    _apps.Add( app );
            Loading = false;
        }

        private async void OnRemoveApp( Flint.Core.App app )
        {
            if ( _pebble.Alive == false )
                return;

            await _pebble.RemoveAppAsync( app );
            await LoadAppsAsync();
        }

        private async void OnInstallApp()
        {
            var openDialog = new OpenFileDialog
            {
                CheckFileExists = true,
                CheckPathExists = true,
                DefaultExt = "*.pbw",
                Filter = "Pebble Apps|*.pbw|All Files|*",
                RestoreDirectory = true,
                Title = "Pebble App"
            };
            if ( openDialog.ShowDialog() == true )
            {
                PebbleBundle bundle;
                using (var zip = new Zip.Zip())
                {
                    bundle = new PebbleBundle(openDialog.OpenFile(), zip);    
                }

                if ( _pebble.Alive == false )
                    return;
                await _pebble.InstallAppAsync( bundle );
                await LoadAppsAsync();
            }
        }
    }
}