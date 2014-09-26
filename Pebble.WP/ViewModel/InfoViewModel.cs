using System;
using System.Threading.Tasks;
using Flint.Core.Responses;

namespace Pebble.WP.ViewModel
{
    public class InfoViewModel : ViewModelBase
    {
        private const string TIME_FORMAT = "";

        private string _watchTime;
        public string WatchTime
        {
            get { return _watchTime; }
            set { Set(ref _watchTime, value); }
        }

        public async Task SetPebbleAsync(Flint.Core.Pebble pebble)
        {
            if (pebble == null) throw new ArgumentNullException("pebble");

            if (pebble.Alive)
            {
                TimeResponse timeResponse = await pebble.GetTimeAsync();
                if (timeResponse.Success)
                    WatchTime = timeResponse.Time.ToString("G");
            }
        }
    }
}