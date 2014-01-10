using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using GalaSoft.MvvmLight;
using Windows.Pebble.Messages;
using Windows.Pebble.Util;
using flint;
using flint.Responses;

namespace Windows.Pebble.ViewModels
{
    public class PebbleMediaViewModel : ViewModelBase
    {
        private flint.Pebble _pebble;
        private readonly ObservableCollection<string> _commandsReceived; 

        public PebbleMediaViewModel()
        {
            _commandsReceived = new ObservableCollection<string>();

            MessengerInstance.Register<PebbleConnected>(this, OnPebbleConnected);
            MessengerInstance.Register<PebbleDisconnected>(this, OnPebbleDisconnected);
        }

        public ICollectionView CommandsReceived
        {
            get { return CollectionViewSource.GetDefaultView(_commandsReceived); }
        }

        private async void OnPebbleConnected( PebbleConnected pebbleConnected )
        {
            _pebble = pebbleConnected.Pebble;

            _pebble.RegisterCallback<MusicControlResponse>( OnMusicControlReceived );
            var response  = await _pebble.SetNowPlaying( "Kevin", "Album", "Track 1" );
        }

        private void OnPebbleDisconnected( PebbleDisconnected pebbleDisconnected )
        {
            if (pebbleDisconnected.Pebble == _pebble)
            {
                _pebble.UnregisterCallback<MusicControlResponse>(OnMusicControlReceived);

                _pebble = null;
            }
        }
        
        private void OnMusicControlReceived( MusicControlResponse response )
        {
            switch ( response.Command )
            {
                case MediaControls.PlayPause:
                    NativeMethods.SendMessage( AppCommandCode.MediaPlayPause );
                    _commandsReceived.Add("Play/Pause");
                    break;
                case MediaControls.Next:
                    NativeMethods.SendMessage( AppCommandCode.MediaNextTrack );
                    _commandsReceived.Add("Next Track");
                    break;
                case MediaControls.Previous:
                    NativeMethods.SendMessage( AppCommandCode.MediaPreviousTrack );
                    _commandsReceived.Add("Previous Track");
                    break;
            }
        }
    }
}