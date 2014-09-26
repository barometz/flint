
using System;
using System.Threading.Tasks;

namespace Pebble.WP.ViewModel
{
    public class PivotPageViewModel : ViewModelBase
    {
        public PivotPageViewModel()
        {
            Info = new InfoViewModel();
        }

        private InfoViewModel _info;
        public InfoViewModel Info
        {
            get { return _info; }
            private set { Set(ref _info, value); }
        }

        public async Task SetPebbleAsync(Flint.Core.Pebble pebble)
        {
            if (pebble == null) throw new ArgumentNullException("pebble");
            if (pebble.IsAlive == false)
            {
                await pebble.ConnectAsync();
            }

            await Info.SetPebbleAsync(pebble);
        }
    }
}