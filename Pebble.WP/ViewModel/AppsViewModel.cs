using System.Collections.ObjectModel;
using System.Threading.Tasks;
using PebbleWatch = Flint.Core.Pebble;
using PebbleApp = Flint.Core.App;

namespace Pebble.WP.ViewModel
{
    public class AppsViewModel : PebbleViewModel
    {
        public AppsViewModel()
        {
            Apps = new ObservableCollection<PebbleApp>();
        }

        public ObservableCollection<PebbleApp> Apps { get; private set; } 

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

        
    }
}