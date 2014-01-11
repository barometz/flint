using System;
using System.Runtime.InteropServices;

namespace Windows.Pebble.Util
{
    //Based on an example from: http://stackoverflow.com/questions/18839510/virtualkeycode-media-play-pause-not-working
    public static class NativeMethods
    {
        [DllImport( "user32.dll" )]
        private static extern IntPtr SendMessageW( IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam );

        private const int WM_APPCOMMAND = 0x0319;

        private static readonly IntPtr _windowHandle;
        
        static NativeMethods()
        {
            _windowHandle = App.MainWindowHandle;
            if (_windowHandle == IntPtr.Zero)
                throw new Exception("MainWindowHandle not set");
        }

        public static void SendMessage( AppCommandCode command )
        {
            if (command == AppCommandCode.None)
                throw new ArgumentException("A command is required", "command");

            var commandId = (IntPtr)( (int) command << 16 );
            SendMessageW( _windowHandle, WM_APPCOMMAND, _windowHandle, commandId);
        }
    }

    public enum AppCommandCode : uint
    {
        None = 0,
        BassBoost = 20,
        BassDown = 19,
        BassUp = 21,
        BrowserBackward = 1,
        BrowserFavorites = 6,
        BrowserForward = 2,
        BrowserHome = 7,
        BrowserRefresh = 3,
        BrowserSearch = 5,
        BrowserStop = 4,
        LaunchApp1 = 17,
        LaunchApp2 = 18,
        LaunchMail = 15,
        LaunchMediaSelect = 16,
        MediaNextTrack = 11,
        MediaPlayPause = 14,
        MediaPreviousTrack = 12,
        MediaStop = 13,
        TrebleDown = 22,
        TrebleUp = 23,
        VolumeDown = 9,
        VolumeMute = 8,
        VolumeUp = 10,
        MicrophoneVolumeMute = 24,
        MicrophoneVolumeDown = 25,
        MicrophoneVolumeUp = 26,
        Close = 31,
        Copy = 36,
        CorrectionList = 45,
        Cut = 37,
        DictateOrCommandControlToggle = 43,
        Find = 28,
        ForwardMail = 40,
        Help = 27,
        MediaChannelDown = 52,
        MediaChannelUp = 51,
        MediaFastForward = 49,
        MediaPause = 47,
        MediaPlay = 46,
        MediaRecord = 48,
        MediaRewind = 50,
        MicOnOffToggle = 44,
        New = 29,
        Open = 30,
        Paste = 38,
        Print = 33,
        Redo = 35,
        ReplyToMail = 39,
        Save = 32,
        SendMail = 41,
        SpellCheck = 42,
        Undo = 34,
        Delete = 53,
        DwmFlip3D = 54
    }
}