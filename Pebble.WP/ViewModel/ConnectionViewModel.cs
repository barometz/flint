using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI.Xaml.Controls;
using Pebble.WP.Bluetooth;
using Pebble.WP.Common;

namespace Pebble.WP.ViewModel
{
    public class ConnectionViewModel : ViewModelBase
    {
        private readonly ObservableCollection<Flint.Core.Pebble> _pebbles = new ObservableCollection<Flint.Core.Pebble>();
        private readonly RelayCommand _connectCommand;
        private readonly NavigationHelper _navigationHelper;

        private Flint.Core.Pebble _selectedPebble;

        public ConnectionViewModel(Page currentPage)
        {
            if (currentPage == null) throw new ArgumentNullException("currentPage");
            _navigationHelper = new NavigationHelper(currentPage);
            _connectCommand = new RelayCommand(OnConnect, CanConnect);
        }

        public ObservableCollection<Flint.Core.Pebble> Pebbles
        {
            get { return _pebbles; }
        }

        public ICommand ConnectCommand
        {
            get { return _connectCommand; }
        }

        public Flint.Core.Pebble SelectedPebble
        {
            get { return _selectedPebble; }
            set
            {
                if (Set(ref _selectedPebble, value))
                    _connectCommand.RaiseCanExecuteChanged();
            }
        }

        public async Task ScanForPairedDevicesAsync()
        {
            _pebbles.Clear();
            _pebbles.Clear();
            foreach (var pebble in await PebbleScanner.DetectPebbles())
                _pebbles.Add(pebble);

            if (_pebbles.Count == 1)
                SelectedPebble = _pebbles[0];
        }

        private void OnConnect()
        {
            if (SelectedPebble != null)
                _navigationHelper.Frame.Navigate(typeof(PivotPage), SelectedPebble);
        }

        private bool CanConnect()
        {
            return SelectedPebble != null;
        }
    }
}