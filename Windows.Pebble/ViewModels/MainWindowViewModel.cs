using System.Linq;
using Windows.Pebble.Bluetooth;
using GalaSoft.MvvmLight;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Data;

namespace Windows.Pebble.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly IList<PebbleViewModel> _pebbleDevices;

        public MainWindowViewModel()
        {
            if ( IsInDesignMode == false )
            {
                _pebbleDevices = new List<PebbleViewModel>( PebbleScanner.DetectPebbles().Select( x => new PebbleViewModel( x ) ) );
            }
        }

        public ICollectionView PebbleDevices
        {
            get { return CollectionViewSource.GetDefaultView( _pebbleDevices ); }
        }
    }
}