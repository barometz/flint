using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Flint.Core.Responses;
using Pebble.WP.Common;

namespace Pebble.WP.ViewModel
{
    public class InfoViewModel : PebbleViewModel
    {
        private readonly ICommand _setTimeCommand;

        public InfoViewModel()
        {
            _setTimeCommand = new RelayCommand(OnSetTime);
            TimeDisplay = "Waiting for Pebble";
        }

        public ICommand SetTimeCommand
        {
            get { return _setTimeCommand; }
        }

        private string _timeDisplay;
        public string TimeDisplay
        {
            get { return _timeDisplay; }
            set { Set(ref _timeDisplay, value); }
        }

        public override async Task RefreshAsync()
        {
            await GetPebbleTimeAsyc();
        }

        private async void OnSetTime()
        {
            if (Pebble != null)
            {
                TimeDisplay = "Setting Pebble time";
                await Pebble.SetTimeAsync(DateTime.Now);
                await GetPebbleTimeAsyc();
            }
        }

        private async Task GetPebbleTimeAsyc()
        {
            TimeDisplay = "Getting curret Pebble time";
            TimeResponse timeResponse = await Pebble.GetTimeAsync();
            var current = DateTime.Now;
            if ( timeResponse.Success )
            {
                var differece = timeResponse.Time - current;
                if (differece < TimeSpan.FromSeconds(2))
                {
                    TimeDisplay = "Pebble time is in sync with the phone";
                }
                else
                {
                    TimeDisplay = string.Format("Pebble is {0} {1} than the phone", differece.ToString(@"h\:mm\:ss"),
                        timeResponse.Time > current ? "faster" : "slower");
                }
            }
            else
            {
                TimeDisplay = "Failed to get time from Pebble: " + timeResponse.ErrorMessage;
            }
        }
    }
}