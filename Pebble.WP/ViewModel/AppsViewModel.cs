using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Pebble.WP.Common;
using PebbleWatch = Flint.Core.Pebble;
using PebbleApp = Flint.Core.App;

namespace Pebble.WP.ViewModel
{
    public class AppsViewModel : PebbleViewModel
    {
        private readonly ICommand _uninstallAppCommand;

        public AppsViewModel()
        {
            _uninstallAppCommand = new RelayCommand<PebbleApp?>(OnUninstallApp);
            Apps = new ObservableCollection<PebbleApp>();
        }

        public ObservableCollection<PebbleApp> Apps { get; private set; }

        public ICommand UninstallCommand
        {
            get { return _uninstallAppCommand; }
        }

        public override async Task RefreshAsync()
        {
            await LoadAppsAsync();
        }

        private async Task LoadAppsAsync()
        {
            var appBankContents = await Pebble.GetAppbankContentsAsync();
            Apps.Clear();
            if (appBankContents != null && appBankContents.AppBank != null && appBankContents.AppBank.Apps != null)
            {
                foreach (var app in appBankContents.AppBank.Apps)
                {
                    Apps.Add(app);
                }
            }
        }

        private async void OnUninstallApp( PebbleApp? app )
        {
            if (app == null)
                return;

            var response = await Pebble.RemoveAppAsync(app.Value);
            if (response.Success)
            {
                await LoadAppsAsync();
            }
        }
        
    }
}