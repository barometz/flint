using System.Windows.Input;
using GalaSoft.MvvmLight.Command;
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
        private readonly RelayCommand _setNowPlayingCommand;

        public PebbleMediaViewModel()
        {
            _setNowPlayingCommand = new RelayCommand(OnSetNowPlaying);
            _commandsReceived = new BindingList<string>();
        }

        public ICollectionView CommandsReceived
        {
            get { return CollectionViewSource.GetDefaultView( _commandsReceived ); }
        }

        public ICommand SetNowPlayingCommand
        {
            get { return _setNowPlayingCommand; }
        }

        private string _Artist;
        public string Artist
        {
            get { return _Artist; }
            set { Set(() => Artist, ref _Artist, value); }
        }

        private string _Album;
        public string Album
        {
            get { return _Album; }
            set { Set(() => Album, ref _Album, value); }
        }

        private string _Track;
        public string Track
        {
            get { return _Track; }
            set { Set(() => Track, ref _Track, value); }
        }

        protected override void OnPebbleConnected( PebbleConnected pebbleConnected )
        {
            base.OnPebbleConnected(pebbleConnected);

            _pebble.RegisterCallback<MusicControlResponse>( OnMusicControlReceived );
        }

        protected override void OnPebbleDisconnected( PebbleDisconnected pebbleDisconnected )
        {
            _pebble.UnregisterCallback<MusicControlResponse>( OnMusicControlReceived );

            base.OnPebbleDisconnected(pebbleDisconnected);
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
                default:
                    AddCommandReceived( response.Command.ToString() );
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


        private async void OnSetNowPlaying()
        {
            await _pebble.SetNowPlayingAsync(Artist ?? "", Album ?? "", Track ?? "");
        }
    }
}