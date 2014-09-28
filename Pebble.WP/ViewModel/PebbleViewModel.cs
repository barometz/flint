
using System.Threading.Tasks;
using PebbleWatch = Flint.Core.Pebble;

namespace Pebble.WP.ViewModel
{
    public abstract class PebbleViewModel : ViewModelBase
    {
        public PebbleWatch Pebble { get; set; }

        public abstract Task RefreshAsync();
    }
}