Flint
=====

A .NET library for talking to Pebble smartwatches. Based on documentation at http://pebbledev.org/wiki/Main_Page 
and the implementation at https://github.com/Hexxeh/libpebble plus its various forks.

Basic communication works.  My personal priorities right now are to make the basic Pebble usage work, such as media
events and notifications.

Contributions are absolutely welcome.  

Flintlock
---
[Flintlock][] is the Windows desktop application that Flint was initially created for.

Dependencies
---
Flint relies on [32feet.NET][] to get the friendly names of known Bluetooth devices.  Seems like this should be 
possible through [WMI][], but the relevant ```Win32_PNPDevice``` and ```Win32_SerialPort``` objects don't contain 
anything to identify them as belonging to a Pebble. 

Flint uses [DotNetZip][] to open Pebble bundles (.pbw files) - unlike [```System.IO.Compression.ZipArchive```][ziparch] 
(which requires .NET 4.5) it works from .NET 2.0 and up, and unlike [```System.IO.Packaging.Package```][package] it's
not a pain to work with - quite the opposite, in fact.

All of these are included in binary form.

[Flintlock]: http://barometz.github.io/flintlock/ "Flintlock"
[32Feet.NET]: http://32feet.codeplex.com/ "32feet.NET - Home"
[WMI]: http://msdn.microsoft.com/en-us/library/windows/desktop/aa394582(v=vs.85).aspx "Windows Management Instrumentation (Windows)"
[DotNetZip]: http://dotnetzip.codeplex.com/ "DotNetZip Library - Home"
[ziparch]: http://msdn.microsoft.com/en-us/library/system.io.compression.ziparchive.aspx "ZipArchive Class (System.IO.Compression)"
[package]: http://msdn.microsoft.com/en-us/library/system.io.packaging.package.aspx "Package Class (System.IO.Packaging)"
