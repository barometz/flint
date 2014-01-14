using flint;
using flint.Responses;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using Windows.Pebble.Messages;
using Windows.Pebble.Util;

namespace Windows.Pebble.ViewModels
{
    public class PebbleMediaViewModel : PebbleViewModelBase
    {
        private readonly BindingList<string> _commandsReceived;

        public PebbleMediaViewModel()
        {
            _commandsReceived = new BindingList<string>();
        }

        public ICollectionView CommandsReceived
        {
            get { return CollectionViewSource.GetDefaultView( _commandsReceived ); }
        }

        protected override void OnPebbleConnected( PebbleConnected pebbleConnected )
        {
            base.OnPebbleConnected(pebbleConnected);

            _pebble.RegisterCallback<MusicControlResponse>( OnMusicControlReceived );
        }

        protected override void OnPebbleDisconnected( PebbleDisconnected pebbleDisconnected )
        {
            base.OnPebbleDisconnected(pebbleDisconnected);

            _pebble.UnregisterCallback<MusicControlResponse>( OnMusicControlReceived );
        }

        private void OnMusicControlReceived( MusicControlResponse response )
        {
            switch ( response.Command )
            {
                case MediaControls.PlayPause:
                    NativeMethods.SendMessage( AppCommandCode.MediaPlayPause );
                    AddCommandReceived( "Play/Pause" );
                    break;
                case MediaControls.Next:
                    NativeMethods.SendMessage( AppCommandCode.MediaNextTrack );
                    AddCommandReceived( "Next Track" );
                    break;
                case MediaControls.Previous:
                    NativeMethods.SendMessage( AppCommandCode.MediaPreviousTrack );
                    AddCommandReceived( "Previous Track" );
                    break;
            }
        }

        private void AddCommandReceived( string command )
        {
            var dispatcher = Application.Current.Dispatcher;
            if ( dispatcher.CheckAccess() == false)
                dispatcher.Invoke( () => _commandsReceived.Add( command ) );
            else
                _commandsReceived.Add( command );
        }
    }
}