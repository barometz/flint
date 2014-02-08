using System;

namespace flint
{
    /// <summary> Media control instructions as understood by Pebble </summary>
    public enum MediaControl : byte
    {
        None= 0,
        PlayPause = 1,
        Pause = 2,
        Play = 3,
        Next = 4,
        Previous = 5,
        VolumeUp = 6,
        VolumeDown = 7,
        GetNowPlaying = 8,
        SendNowPlaying = 9
    }

    /// <summary> Endpoints (~"commands") used by Pebble to indicate particular instructions 
    /// or instruction types.
    /// </summary>
    public enum Endpoint : ushort
    {
        Firmware = 1,
        Time = 11,
        FirmwareVersion = 16,
        PhoneVersion = 17,
        SystemMessage = 18,
        MusicControl = 32,
        PhoneControl = 33,
        ApplicationMessage = 48,
        Launcher = 49,
        Logs = 2000,
        Ping = 2001,
        LogDump = 2002,
        Reset = 2003,
        App = 2004,
        Notification = 3000,
        SysReg = 5000,
        FctReg = 5001,
        AppManager = 6000,
        RunKeeper = 7000,
        PutBytes = 48879,
        MaxEndpoint = 65535 //ushort.MaxValue
    }

    [Flags]
    public enum RemoteCaps : uint
    {
        Unknown = 0,
        IOS = 1,
        Android = 2,
        OSX = 3,
        Linux = 4,
        Windows = 5,
        Telephony = 16,
        SMS = 32,
        GPS = 64,
        BTLE = 128,
        // 240? No, that doesn't make sense.  But it's apparently true.
        CameraFront = 240,
        CameraRear = 256,
        Accelerometer = 512,
        Gyro = 1024,
        Compass = 2048
    }

    public enum LogLevel
    {
        Unknown = -1,
        All = 0,
        Error = 1,
        Warning = 50,
        Information = 100,
        Debug = 200,
        Verbose = 250
    }
}