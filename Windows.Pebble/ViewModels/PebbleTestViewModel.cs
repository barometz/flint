using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Windows.Pebble.Messages;
using flint.Responses;

namespace Windows.Pebble.ViewModels
{
    public class PebbleTestViewModel : ViewModelBase
    {
        private readonly RelayCommand _pingCommand;

        private flint.Pebble _pebble;

        public PebbleTestViewModel()
        {
            _pingCommand = new RelayCommand(OnPing);

            MessengerInstance.Register<PebbleConnected>( this, OnPebbleConnected );
            MessengerInstance.Register<PebbleDisconnected>( this, OnPebbleDisconnected );
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

        private void OnPebbleConnected( PebbleConnected pebbleConnected )
        {
            _pebble = pebbleConnected.Pebble;

        }

        private void OnPebbleDisconnected( PebbleDisconnected pebbleDisconnected )
        {
            if ( pebbleDisconnected.Pebble == _pebble )
            {
                _pebble = null;
            }
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