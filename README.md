map for tshock
============

This adds an in-game Terraria World Mapper:
I would like to thank the authors of MoreTerra and Terrafirma for their work on the color codes, without them this plugin would not be possible.
Features
--------

+ Faster than downloading the World and using MoreTerra/Terrafirma on it
+ Same color schemes as MoreTerra and Terrafirma
+ Path output configurable
+ Image name configurable
+ Timestamp naming
+ ability to change between color schemes
+ auto mapping ability in a certain interval of minutes
+ mapping only a specified rectangle of the map, faster than mapping and then cropping

Permissions
-----------

+ `map.create` gives player access to the `map` command.

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

Updates
-------
+ 4.2.3.0727: update to API 16. Permissions is now "map.create" instead of "map".
+ 4.2.2.0422: update to API 15 and added tiles/walls for terraria 1.2.3.1 (Terrafirma only for now)
+ 4.2: update to API 14, and terraria 1.2 (TerraFirma colorscheme updated only)
+ 4.1.0.0929: crash fix when Bitmap object can't be created.
+ 4.1.0.0926: update to API 13
+ 4.0.5.0: added mapping a subset of the map, by specifying coordinates for a rectangle
+ 4.0.0.0: chest highlight bug fix
+ 3.9.0.0: update to API 12
+ 3.8.0.1: fixed the MoreTerra color scheme
+ 3.8.0.0: small update for TShock 3.8.0
+ 3.5.1.0: update to API 11
+ 3.4.5.1: auto-save settings for highlighting, timestamp naming, and ingame command
+ 3.4.5.0: auto-save feature, TShock release
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