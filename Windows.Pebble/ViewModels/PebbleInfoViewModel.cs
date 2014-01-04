using System;
using System.Windows.Threading;
using GalaSoft.MvvmLight;
using flint;

namespace Windows.Pebble.ViewModels
{
    public class PebbleInfoViewModel : ViewModelBase
    {
        private readonly DispatcherTimer _timer = new DispatcherTimer( DispatcherPriority.Background );
        private readonly flint.Pebble _pebble;

        public PebbleInfoViewModel( flint.Pebble pebble )
        {
            _pebble = pebble;
            _timer.Tick += ( sender, e ) => UpdateTimes();
            _timer.Interval = TimeSpan.FromMilliseconds( 500 );
        }

        private bool _IsSelected;
        public bool IsSelected
        {
            get { return _IsSelected; }
            set
            {
                if ( Set( () => IsSelected, ref _IsSelected, value ) )
                    if ( value )
                        _timer.Start();
                    else
                        _timer.Stop();
            }
        }

        private DateTime? _PebbleTime;
        public DateTime? PebbleTime
        {
            get { return _PebbleTime; }
            private set { Set( () => PebbleTime, ref _PebbleTime, value ); }
        }

        public DateTime? CurrentTime
        {
            get { return  DateTime.Now; }
        }

        private async void UpdateTimes()
        {
            if (_pebble.Alive)
            {
                TimeReceivedEventArgs timeResult = await _pebble.GetTimeAsync();
                if ( timeResult != null )
                    PebbleTime = timeResult.Time;
            }
            
            RaisePropertyChanged( () => CurrentTime );
        }
    }
}