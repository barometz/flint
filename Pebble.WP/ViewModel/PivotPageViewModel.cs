
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Pebble.WP.Common;

namespace Pebble.WP.ViewModel
{
    public class PivotPageViewModel : ViewModelBase
    {
        private readonly ICommand _refreshCommand;
        private readonly InfoViewModel _info;
        private readonly AppsViewModel _apps;

        public PivotPageViewModel()
        {
            _info = new InfoViewModel();
            _apps = new AppsViewModel();
            _refreshCommand = new RelayCommand(OnRefresh);
        }

        public InfoViewModel Info
        {
            get { return _info; }
        }

        public AppsViewModel Apps
        {
            get { return _apps; }
        }

        public ICommand RefreshCommand
        {
            get { return _refreshCommand; }
        }

        public async Task SetPebbleAsync(Flint.Core.Pebble pebble)
        {
            if (pebble == null) throw new ArgumentNullException("pebble");
            if (pebble.IsAlive == false)
            {
                await pebble.ConnectAsync();
            }

            Info.Pebble = Apps.Pebble = pebble;
            await Info.RefreshAsync();
            await Apps.RefreshAsync();
        }

        private async void OnRefresh()
        {
            await Info.RefreshAsync();
            await Apps.RefreshAsync();
        }
    }
}