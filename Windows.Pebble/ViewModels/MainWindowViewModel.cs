using System;
using System.Linq;
using System.Windows.Threading;
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
                _pebbleDevices = new List<PebbleViewModel>( flint.Pebble.DetectPebbles().Select( x => new PebbleViewModel( x ) ) );
            }
        }

        public ICollectionView PebbleDevices
        {
            get { return CollectionViewSource.GetDefaultView( _pebbleDevices ); }
        }


        //private flint.Pebble GetCurrentPebble()
        //{
        //    flint.Pebble currentPebble = null;
        //    var pebbleViewModel = (PebbleViewModel)PebbleDevices.CurrentItem;
        //    if ( pebbleViewModel != null )
        //    {
        //        currentPebble = pebbleViewModel.GetPebble();
        //
        //        if ( currentPebble != null && currentPebble.Alive == false )
        //            currentPebble.Connect();
        //    }
        //    return currentPebble;
        //}
        //
        //private void UpdateTimes( object sender, EventArgs e )
        //{
        //    var pebble = GetCurrentPebble();
        //
        //    if ( pebble != null )
        //        PebbleTime = pebble.GetTime().Time;
        //    else
        //        PebbleTime = null;
        //
        //    RaisePropertyChanged(() => CurrentTime);
        //}

    }
}