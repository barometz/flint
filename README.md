flint
=====

A .NET library for talking to Pebble smartwatches. Based on documentation at http://pebbledev.org/wiki/Main_Page 
and the implementation at https://github.com/Hexxeh/libpebble plus its various forks.

Basic communication works.  My personal priorities right now are to make the basic Pebble usage work, such as media
events and notifications.  Next on the list is autodetecting Pebbles on Windows, probably through WMI
(http://msdn.microsoft.com/en-us/library/aa394587%28v=vs.85%29.aspx).  Linux/Mono support is somewhat secondary because
Mono's SerialPort implementation lacks some features, although I do have a dirtyfix for that lying around in another
project.

Contributions are absolutely welcome.  
