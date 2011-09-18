map for TDSM
============

This adds an in-game Terraria World Mapper:

Features
--------

+ Faster than downloading a map and mapping
+ It maps from the world in use (RAM=faster)
+ Path output configurable
+ Image name configurable
+ Timestamp naming
+ Two colorschemes to choose from: MoreTerra, Terrafirma

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

To specify path ingame you use (doesn't save)
> `map -p /path/to/output`

or in windows
> `map -p C:\\path\\to\\output` (and yes you have to do two slashes because of how the tokenizer works)

To specify a path ingame and also save it to map.properties
> `map -s -p /path/to/output`

or in windows
> `map -s -p C:\\path\\to\\output` (and yes you have to do two slashes because of how the tokenizer works)

to change colorscheme
> `map -s -c MoreTerra` or > `map -s -c Terrafirma`

Updates
-------

+ 0.35.3: added liquid blending in Terrafirma color scheme
+ 0.35.2: background fading in Terrafirma color scheme
+ 0.35.1: fix to threading
+ 0.35.0: added threading
+ 0.34.1: added the Terrafirma color scheme as an option
+ 0.34.0: added ingame setting of path
+ 0.33.0: release

Options
-------

The default path is the server directory where Terraria_Server.exe is located.
The default colorscheme is Terrafirma.


Please let me know if I have overlooked any bugs.