using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Navigation;

namespace Windows.Pebble
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        public static IntPtr MainWindowHandle { get; private set; }
        
        protected override void OnActivated( EventArgs e )
        {
            base.OnActivated( e );
            if (MainWindowHandle == IntPtr.Zero)
            {
                MainWindowHandle = new WindowInteropHelper( MainWindow ).Handle;
            }
        }
    }
}
