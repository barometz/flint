using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556
using Pebble.WP.ViewModel;

namespace Pebble.WP.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ConnectionPage
    {
        public ConnectionPage()
        {
            ViewModel = new ConnectionViewModel(this);
            InitializeComponent();
        }

        public ConnectionViewModel ViewModel { get; private set; }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            await ViewModel.ScanForPairedDevicesAsync();
        }
    }
}
