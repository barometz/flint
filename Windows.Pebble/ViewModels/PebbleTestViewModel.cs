using Flint.Core.Responses;
using GalaSoft.MvvmLight.Command;
using System.Windows.Input;

namespace Windows.Pebble.ViewModels
{
    public class PebbleTestViewModel : PebbleViewModelBase
    {
        private readonly RelayCommand _pingCommand;
        private readonly RelayCommand _badPingCommand;

        public PebbleTestViewModel()
        {
            _pingCommand = new RelayCommand(OnPing);
            _badPingCommand = new RelayCommand(OnBadPing);
        }

        public ICommand PingCommand
        {
            get { return _pingCommand; }
        }

        public ICommand BadPingCommand
        {
            get { return _badPingCommand; }
        }

        private string _PingResponse;
        public string PingResponse
        {
            get { return _PingResponse; }
            set { Set(() => PingResponse, ref _PingResponse, value); }
        }

        private async void OnPing()
        {
            if (_pebble == null || _pebble.Alive == false)
                return;

            PingResponse pingResponse = await _pebble.PingAsync();
            PingResponse = pingResponse.Success ? "Success" : pingResponse.ErrorMessage;
        }

        private async void OnBadPing()
        {
            if ( _pebble == null || _pebble.Alive == false )
                return;

            PingResponse pingResponse = await _pebble.BadPingAsync();
            PingResponse = pingResponse.ErrorMessage;
        }
    }
}