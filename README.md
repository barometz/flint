Flint
=====

A .NET library for talking to Pebble smartwatches. Based on documentation at http://pebbledev.org/wiki/Main_Page 
and the implementation at https://github.com/Hexxeh/libpebble plus its various forks.

Basic communication works.  My personal priorities right now are to make the basic Pebble usage work, such as media
events and notifications.

Contributions are absolutely welcome.  

Dependencies
---
Flint relies on 32feet.NET to get the friendly names of known bluetooth devices.  Seems like this should be 
possible through WMI, but the relevant Win32_PNPDevice and Win32_SerialPort objects don't contain anything to 
identify them as belonging to a Pebble. More information about 32feet.NET and its license can be found at 
http://32feet.codeplex.com/
