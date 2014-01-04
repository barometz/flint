using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using flint.Responses;

namespace Windows.Pebble.ViewModels
{
    public class PebbleTestViewModel : ViewModelBase
    {
        private readonly flint.Pebble _pebble;
        private readonly RelayCommand _pingCommand;

        public PebbleTestViewModel( flint.Pebble pebble )
        {
            _pebble = pebble;

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
            if (_pebble.Alive == false)
                return;

            PingResponse pingResponse = await _pebble.PingAsync();
            PingResponse = pingResponse.Success ? "Success" : pingResponse.ErrorMessage;
        }
    }
}