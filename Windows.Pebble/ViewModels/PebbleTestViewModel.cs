using flint.Responses;
using GalaSoft.MvvmLight.Command;
using System.Windows.Input;

namespace Windows.Pebble.ViewModels
{
    public class PebbleTestViewModel : PebbleViewModelBase
    {
        private readonly RelayCommand _pingCommand;

        public PebbleTestViewModel()
        {
            _pingCommand = new RelayCommand(OnPing);
        }

        public ICommand PingCommand
        {
            get { return _pingCommand; }
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
    }
}