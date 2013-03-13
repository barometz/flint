Flint
=====

A .NET library for talking to Pebble smartwatches. Based on documentation at http://pebbledev.org/wiki/Main_Page 
and the implementation at https://github.com/Hexxeh/libpebble plus its various forks.

Basic communication works.  My personal priorities right now are to make the basic Pebble usage work, such as media
events and notifications.

Contributions are absolutely welcome.  

Dependencies
---
Flint relies on 32feet.NET to get the friendly names of known bluetooth devices. It can be found at 
http://32feet.codeplex.com/ or through NuGet.  Seems like this should be possible through WMI, but for the time
being it'll do.
