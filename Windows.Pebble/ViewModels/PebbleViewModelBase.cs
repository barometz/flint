using GalaSoft.MvvmLight;
using Windows.Pebble.Messages;

namespace Windows.Pebble.ViewModels
{
    public class PebbleViewModelBase : ViewModelBase
    {
        protected Flint.Core.Pebble _pebble;

        protected PebbleViewModelBase()
        {
            MessengerInstance.Register<PebbleConnected>( this, OnPebbleConnected );
            MessengerInstance.Register<PebbleDisconnected>( this, OnPebbleDisconnected );
        }

        protected virtual void OnPebbleConnected( PebbleConnected pebbleConnected )
        {
            _pebble = pebbleConnected.Pebble;
        }

        protected virtual void OnPebbleDisconnected( PebbleDisconnected pebbleDisconnected )
        {
            if ( pebbleDisconnected.Pebble == _pebble )
            {
                _pebble = null;
            }
        }
    }
}