map for TDSM
============

This adds an in-game Terraria World Mapper:

Features
--------

+ Faster than downloading a Map and using MoreTerra
+ It maps from the world in use (RAM=faster)
+ Path output configurable
+ Image name configurable
+ Timestamp naming

Usage
-----

Mapping to "world-now.png"
> `map`

Map to a file in the format "terraria-2011-09-07_15-23-00.png"
> `map -t`

Map to a specified imagename
> `map -n imagename.png`

Reload mapoutput-path from map.properties
> `map -L`

Options
-------

The default path is the server directory where Terraria_Server.exe is located.

