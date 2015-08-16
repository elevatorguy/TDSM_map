map for TDSM
============

This adds an in-game Terraria World Mapper:

Features
--------

+ Faster than downloading a map and mapping
+ It maps from the world in use (RAM=faster)
+ Same color schemes as MoreTerra and Terrafirma
+ Path output configurable
+ Image name configurable
+ Timestamp naming
+ ability to change between color schemes
+ auto mapping ability in a certain interval of minutes
+ mapping only a specified rectangle of the map, faster than mapping and then cropping
+ Can generate files to view map in web browser.

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

to highlight a block
> `map -h [name/id]`

chests can be mapped using
> `map -h chest`

ingame command to change autosaving
> `map -a` (toggles enabled/disabled)

> `map -a -t` (toggles timestamp naming)

> `map -a -n` (toggles output name when not doing timestamp naming)

> `map -a -h [name/id]` (toggles highlighting, and sets the id to that specified)

> `map -x1 500 -x2 600 -y1 500 -y2 600` (maps only a portion of the map, in this case from (500,500) to (600,600))
(x1,y1) must be the top left corner, and (x2,y2) the bottom right corner.

to generate tiles and html file for web viewing
> `map -w` or `map -web`

Updates
-------

+ 0.39.2: new command that generates map tiles for leafletjs mapper in web browser.
+ 0.39.1: updated for TDSM Rebind b5, and .NET 4.5. 
+ 0.39.0: merged code from tshock branch: map API v1. and new tiles/colors for Terrafirma.
+ 0.36.2: mapping chests
+ 0.36.1: mapping is separated into four threads / command spamming fix / op only / highlight option
+ 0.36.1: mapping is separated into four threads / command spamming fix / op only / highlight option
+ 0.36.0: updated for TDSM b36's new API / hellwater fix
+ 0.35.4: blending/fading is now done on plugin load
+ 0.35.3: added liquid blending in Terrafirma color scheme
+ 0.35.2: background fading in Terrafirma color scheme
+ 0.35.1: fix to threading
+ 0.35.0: added threading
+ 0.34.1: added the Terrafirma color scheme as an option
+ 0.34.0: added ingame setting of path
+ 0.33.0: release

Options
-------

The default paths are the server directory where Terraria_Server.exe is located.

The default colorscheme is Terrafirma.

The default auto map interval is every 30 minutes.

The default auto map saves to autosave.png.


Please let me know if I have overlooked any bugs.