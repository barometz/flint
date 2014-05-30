
namespace Pebble.WP.ViewModel
{
    public class PivotPageViewModel : ViewModelBase
    {
        public PivotPageViewModel()
        {
            Info = new InfoViewModel();
        }

        private InfoViewModel _info;
        public InfoViewModel Info
        {
            get { return _info; }
            private set { Set(ref _info, value); }
        }
    }
}