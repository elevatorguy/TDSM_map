using System.Drawing;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using Terraria;
using TShockAPI;
using TShock_Map;
using System;

namespace Map
{
	public partial class MapPlugin
	{
		public static Dictionary<int, System.Drawing.Color> tileTypeDefs;
		
		public void mapWorld () 
		{	
            if (!crop)
            {
                x1 = 0;
                x2 = Main.maxTilesX;
                y1 = 0;
                y2 = Main.maxTilesY;
            }
            else //enforce boundaries
            {
                if(x1 < 0)
                    x1 = 0;
                if(y1 < 0)
                    y1 = 0;
                if(x2 > Main.maxTilesX)
                    x2 = Main.maxTilesX;
                if(y2 > Main.maxTilesY)
                    y2 = Main.maxTilesY;
            }
			Stopwatch stopwatch = new Stopwatch ();
            TShock.Log.Info("Saving Image...");
			stopwatch.Start ();

            try
            {
                bmp = new Bitmap((x2 - x1), (y2 - y1), PixelFormat.Format32bppArgb);
            }
            catch (ArgumentException e)
            {
                TShock.Log.Error("<map> ERROR: could not create the Bitmap object.");
                TShock.Log.Info(e.StackTrace.ToString());
                stopwatch.Stop();
                isMapping = false;
                return;
            }

			Graphics graphicsHandle = Graphics.FromImage ((Image)bmp);
			graphicsHandle.FillRectangle (new SolidBrush (Constants.MoreTerra_Color.SKY), 0, 0, bmp.Width, bmp.Height);
			
			using (var prog = new ProgressLogger(x2 - 1, "Saving image data"))
				for (int i = x1; i < x2; i++) {
					prog.Value = i;
					for (int j = y1; j < y2; j++) {		
					
					//TODO: find a more understandable way on these if statements
						if (Main.tile[i, j].wall == 0) {
							if (Main.tile[i, j].active()) {
                                bmp.SetPixel(i - x1, j - y1, tileTypeDefs[Main.tile[i, j].type]);
							} else {

								if (j > Main.worldSurface) {
                                    bmp.SetPixel(i - x1, j - y1, Constants.MoreTerra_Color.WALL_BACKGROUND);
								}
								if (Main.tile[i, j].liquid > 0) {
									if (Main.tile[i, j].lava()) {
                                        bmp.SetPixel(i - x1, j - y1, Constants.MoreTerra_Color.LAVA);
									} else {
                                        bmp.SetPixel(i - x1, j - y1, Constants.MoreTerra_Color.WATER);
									}	
								}
							}
						} else {
                            if (Main.tile[i, j].active())
                            {
                                bmp.SetPixel(i - x1, j - y1, tileTypeDefs[Main.tile[i, j].type]);
							} else {
                                bmp.SetPixel(i - x1, j - y1, tileTypeDefs[Main.tile[i, j].wall + 267]);
							}
						}
						
					}
				}
                TShock.Log.Info("Saving Data...");
				bmp.Save (string.Concat (p, Path.DirectorySeparatorChar, filename));
				stopwatch.Stop ();
                TShock.Log.Info("Save duration: " + stopwatch.Elapsed.Seconds + " Second(s)");
                TShock.Log.Info("Saving Complete.");
				bmp = null;
                isMapping = false;
		}
		
		public partial class Constants //credits go to the authors of MoreTerra
		{ 
			public static class MoreTerra_Color 
			{
				public static System.Drawing.Color DIRT = System.Drawing.Color.FromArgb (175, 131, 101);
				public static System.Drawing.Color STONE = System.Drawing.Color.FromArgb (128, 128, 128);
				public static System.Drawing.Color GRASS = System.Drawing.Color.FromArgb (28, 216, 94);
				public static System.Drawing.Color PLANTS = System.Drawing.Color.FromArgb (13, 101, 36);
				public static System.Drawing.Color LIGHT_SOURCE = System.Drawing.Color.FromArgb (253, 62, 3);
				public static System.Drawing.Color IRON = System.Drawing.Color.FromArgb (189, 159, 139);
				public static System.Drawing.Color COPPER = System.Drawing.Color.FromArgb (255, 149, 50);
				public static System.Drawing.Color GOLD = System.Drawing.Color.FromArgb (185, 164, 23);
				public static System.Drawing.Color WOOD = System.Drawing.Color.FromArgb (86, 62, 44);
				public static System.Drawing.Color WOOD_BLOCK = System.Drawing.Color.FromArgb (168, 121, 87);
				public static System.Drawing.Color SILVER = System.Drawing.Color.FromArgb (217, 223, 223);
				public static System.Drawing.Color DECORATIVE = System.Drawing.Color.FromArgb (0, 255, 242);
				public static System.Drawing.Color IMPORTANT = System.Drawing.Color.FromArgb (255, 0, 0);
				public static System.Drawing.Color DEMONITE = System.Drawing.Color.FromArgb (98, 95, 167);
				public static System.Drawing.Color CORRUPTION_GRASS = System.Drawing.Color.FromArgb (141, 137, 223);
				public static System.Drawing.Color EBONSTONE = System.Drawing.Color.FromArgb (75, 74, 130);
				public static System.Drawing.Color CORRUPTION_VINES = System.Drawing.Color.FromArgb (122, 97, 143);
				public static System.Drawing.Color BLOCK = System.Drawing.Color.FromArgb (178, 0, 255);
				public static System.Drawing.Color METEORITE = System.Drawing.Color.Magenta;//System.Drawing.Color.FromArgb(223, 159, 137);
				public static System.Drawing.Color CLAY = System.Drawing.Color.FromArgb (216, 115, 101);
				public static System.Drawing.Color DUNGEON_GREEN = System.Drawing.Color.FromArgb (26, 136, 34);
				public static System.Drawing.Color DUNGEON_PINK = System.Drawing.Color.FromArgb (169, 49, 117);
				public static System.Drawing.Color DUNGEON_BLUE = System.Drawing.Color.FromArgb (66, 69, 194);
				public static System.Drawing.Color SPIKES = System.Drawing.Color.FromArgb (109, 109, 109);
				public static System.Drawing.Color WEB = System.Drawing.Color.FromArgb (255, 255, 255);
				public static System.Drawing.Color SAND = System.Drawing.Color.FromArgb (255, 218, 56);
				public static System.Drawing.Color OBSIDIAN = System.Drawing.Color.FromArgb (87, 81, 173);
				public static System.Drawing.Color ASH = System.Drawing.Color.FromArgb (68, 68, 76);
				public static System.Drawing.Color HELLSTONE = System.Drawing.Color.FromArgb (102, 34, 34);
				public static System.Drawing.Color MUD = System.Drawing.Color.FromArgb (92, 68, 73);
				public static System.Drawing.Color UNDERGROUNDJUNGLE_GRASS = System.Drawing.Color.FromArgb (143, 215, 29);
				public static System.Drawing.Color UNDERGROUNDJUNGLE_PLANTS = System.Drawing.Color.FromArgb (143, 215, 29);
				public static System.Drawing.Color UNDERGROUNDJUNGLE_VINES = System.Drawing.Color.FromArgb (138, 206, 28);
				public static System.Drawing.Color UNDERGROUNDJUNGLE_THORNS = System.Drawing.Color.FromArgb (94, 48, 55);
				public static System.Drawing.Color GEMS = System.Drawing.Color.FromArgb (42, 130, 250);
				public static System.Drawing.Color CACTUS = System.Drawing.Color.DarkGreen;
				public static System.Drawing.Color CORAL = System.Drawing.Color.LightPink;
				public static System.Drawing.Color HERB = System.Drawing.Color.OliveDrab;
                public static System.Drawing.Color TOMBSTONE = System.Drawing.Color.DimGray;

                public static System.Drawing.Color COBALT = System.Drawing.Color.FromArgb(11, 80, 143);
                public static System.Drawing.Color MYTHRIL = System.Drawing.Color.FromArgb(91, 169, 169);
                public static System.Drawing.Color ADAMANTITE = System.Drawing.Color.FromArgb(128, 26, 52);
                public static System.Drawing.Color EBONSAND = System.Drawing.Color.FromArgb(89, 83, 83);
                public static System.Drawing.Color PEARLSAND = System.Drawing.Color.FromArgb(238, 225, 218);
                public static System.Drawing.Color PEARLSTONE = System.Drawing.Color.FromArgb(181, 172, 190);
                public static System.Drawing.Color SILT = System.Drawing.Color.FromArgb(103, 98, 122);
                public static System.Drawing.Color CRYSTALS = System.Drawing.Color.FromArgb(120, 90, 217);
                public static System.Drawing.Color SNOWBLOCK = System.Drawing.Color.White;
                public static System.Drawing.Color HALLOWED_PLANTS = System.Drawing.Color.FromArgb(78, 192, 226);
                public static System.Drawing.Color HALLOWED_VINES = System.Drawing.Color.FromArgb(37, 184, 227);

                // TODO: update this color
                public static System.Drawing.Color MECHANICAL = System.Drawing.Color.White;

				public static System.Drawing.Color UNDERGROUNDMUSHROOM_GRASS = System.Drawing.Color.FromArgb (93, 127, 255);
				public static System.Drawing.Color UNDERGROUNDMUSHROOM_PLANTS = System.Drawing.Color.FromArgb (177, 174, 131);
				public static System.Drawing.Color UNDERGROUNDMUSHROOM_TREES = System.Drawing.Color.FromArgb (150, 143, 110);
				public static System.Drawing.Color LAVA = System.Drawing.Color.FromArgb (255, 64, 0);
				public static System.Drawing.Color WATER = System.Drawing.Color.FromArgb (0, 12, 255);
				public static System.Drawing.Color SKY = System.Drawing.Color.FromArgb (155, 209, 255);
				public static System.Drawing.Color WALL_STONE = System.Drawing.Color.FromArgb (66, 66, 66);
				public static System.Drawing.Color WALL_DIRT = System.Drawing.Color.FromArgb (88, 61, 46);
				public static System.Drawing.Color WALL_EBONSTONE = System.Drawing.Color.FromArgb (61, 58, 78);
				public static System.Drawing.Color WALL_WOOD = System.Drawing.Color.FromArgb (73, 51, 36);
				public static System.Drawing.Color WALL_BRICK = System.Drawing.Color.FromArgb (60, 60, 60);
				public static System.Drawing.Color WALL_BACKGROUND = System.Drawing.Color.FromArgb (50, 50, 60);
				public static System.Drawing.Color WALL_DUNGEON_PINK = System.Drawing.Color.FromArgb (84, 25, 60);
				public static System.Drawing.Color WALL_DUNGEON_BLUE = System.Drawing.Color.FromArgb (29, 31, 72);
				public static System.Drawing.Color WALL_DUNGEON_GREEN = System.Drawing.Color.FromArgb (14, 68, 16);
				public static System.Drawing.Color WALL_MUD = System.Drawing.Color.FromArgb (61, 46, 49);
				public static System.Drawing.Color WALL_HELLSTONE = System.Drawing.Color.FromArgb (48, 21, 21);
				public static System.Drawing.Color WALL_OBSIDIAN = System.Drawing.Color.FromArgb (87, 81, 173);
				public static System.Drawing.Color UNKNOWN = System.Drawing.Color.Magenta;
			}
		}
		
		public static void InitializeMapperDefs () //Credits go to the authors of MoreTerra
		{	
			tileTypeDefs = new Dictionary<int, System.Drawing.Color> (255);
			
			tileTypeDefs [0] = Constants.MoreTerra_Color.DIRT;
			tileTypeDefs [1] = Constants.MoreTerra_Color.STONE;
			tileTypeDefs [2] = Constants.MoreTerra_Color.GRASS;
			tileTypeDefs [3] = Constants.MoreTerra_Color.PLANTS;
			tileTypeDefs [4] = Constants.MoreTerra_Color.LIGHT_SOURCE;
			tileTypeDefs [5] = Constants.MoreTerra_Color.WOOD;
			tileTypeDefs [6] = Constants.MoreTerra_Color.IRON;
			tileTypeDefs [7] = Constants.MoreTerra_Color.COPPER;
			tileTypeDefs [8] = Constants.MoreTerra_Color.GOLD;
			tileTypeDefs [9] = Constants.MoreTerra_Color.SILVER;

			tileTypeDefs [10] = Constants.MoreTerra_Color.DECORATIVE;
			tileTypeDefs [11] = Constants.MoreTerra_Color.DECORATIVE;
			tileTypeDefs [12] = Constants.MoreTerra_Color.IMPORTANT;
			tileTypeDefs [13] = Constants.MoreTerra_Color.DECORATIVE;
			tileTypeDefs [14] = Constants.MoreTerra_Color.DECORATIVE;
			tileTypeDefs [15] = Constants.MoreTerra_Color.DECORATIVE;
			tileTypeDefs [16] = Constants.MoreTerra_Color.DECORATIVE;
			tileTypeDefs [17] = Constants.MoreTerra_Color.DECORATIVE;
			tileTypeDefs [18] = Constants.MoreTerra_Color.DECORATIVE;
			tileTypeDefs [19] = Constants.MoreTerra_Color.WOOD;

			tileTypeDefs [20] = Constants.MoreTerra_Color.PLANTS;
			tileTypeDefs [21] = Constants.MoreTerra_Color.IMPORTANT;
			tileTypeDefs [22] = Constants.MoreTerra_Color.DEMONITE;
			tileTypeDefs [23] = Constants.MoreTerra_Color.CORRUPTION_GRASS;
			tileTypeDefs [24] = Constants.MoreTerra_Color.CORRUPTION_GRASS;
			tileTypeDefs [25] = Constants.MoreTerra_Color.EBONSTONE;
			tileTypeDefs [26] = Constants.MoreTerra_Color.IMPORTANT;
			tileTypeDefs [27] = Constants.MoreTerra_Color.PLANTS;
			tileTypeDefs [28] = Constants.MoreTerra_Color.IMPORTANT;
			tileTypeDefs [29] = Constants.MoreTerra_Color.DECORATIVE;

			tileTypeDefs [30] = Constants.MoreTerra_Color.WOOD_BLOCK;
			tileTypeDefs [31] = Constants.MoreTerra_Color.IMPORTANT;
			tileTypeDefs [32] = Constants.MoreTerra_Color.CORRUPTION_VINES;
			tileTypeDefs [33] = Constants.MoreTerra_Color.LIGHT_SOURCE;
			tileTypeDefs [34] = Constants.MoreTerra_Color.LIGHT_SOURCE;
			tileTypeDefs [35] = Constants.MoreTerra_Color.LIGHT_SOURCE;
			tileTypeDefs [36] = Constants.MoreTerra_Color.LIGHT_SOURCE;
			tileTypeDefs [37] = Constants.MoreTerra_Color.METEORITE;
			tileTypeDefs [38] = Constants.MoreTerra_Color.BLOCK;
			tileTypeDefs [39] = Constants.MoreTerra_Color.BLOCK;

			tileTypeDefs [40] = Constants.MoreTerra_Color.CLAY;
			tileTypeDefs [41] = Constants.MoreTerra_Color.DUNGEON_BLUE;
			tileTypeDefs [42] = Constants.MoreTerra_Color.LIGHT_SOURCE;
			tileTypeDefs [43] = Constants.MoreTerra_Color.DUNGEON_GREEN;
			tileTypeDefs [44] = Constants.MoreTerra_Color.DUNGEON_PINK;
			tileTypeDefs [45] = Constants.MoreTerra_Color.BLOCK;
			tileTypeDefs [46] = Constants.MoreTerra_Color.BLOCK;
			tileTypeDefs [47] = Constants.MoreTerra_Color.BLOCK;
			tileTypeDefs [48] = Constants.MoreTerra_Color.SPIKES;
			tileTypeDefs [49] = Constants.MoreTerra_Color.LIGHT_SOURCE;

			tileTypeDefs [50] = Constants.MoreTerra_Color.DECORATIVE;
			tileTypeDefs [51] = Constants.MoreTerra_Color.WEB;
			tileTypeDefs [52] = Constants.MoreTerra_Color.PLANTS;
			tileTypeDefs [53] = Constants.MoreTerra_Color.SAND;
			tileTypeDefs [54] = Constants.MoreTerra_Color.DECORATIVE;
			tileTypeDefs [55] = Constants.MoreTerra_Color.DECORATIVE;
			tileTypeDefs [56] = Constants.MoreTerra_Color.OBSIDIAN;
			tileTypeDefs [57] = Constants.MoreTerra_Color.ASH;
			tileTypeDefs [58] = Constants.MoreTerra_Color.HELLSTONE;
			tileTypeDefs [59] = Constants.MoreTerra_Color.MUD;

			tileTypeDefs [60] = Constants.MoreTerra_Color.UNDERGROUNDJUNGLE_GRASS;
			tileTypeDefs [61] = Constants.MoreTerra_Color.UNDERGROUNDJUNGLE_PLANTS;
			tileTypeDefs [62] = Constants.MoreTerra_Color.UNDERGROUNDJUNGLE_VINES;
			tileTypeDefs [63] = Constants.MoreTerra_Color.GEMS;
			tileTypeDefs [64] = Constants.MoreTerra_Color.GEMS;
			tileTypeDefs [65] = Constants.MoreTerra_Color.GEMS;
			tileTypeDefs [66] = Constants.MoreTerra_Color.GEMS;
			tileTypeDefs [67] = Constants.MoreTerra_Color.GEMS;
			tileTypeDefs [68] = Constants.MoreTerra_Color.GEMS;
			tileTypeDefs [69] = Constants.MoreTerra_Color.UNDERGROUNDJUNGLE_THORNS;

			tileTypeDefs [70] = Constants.MoreTerra_Color.UNDERGROUNDMUSHROOM_GRASS;
			tileTypeDefs [71] = Constants.MoreTerra_Color.UNDERGROUNDMUSHROOM_PLANTS;
			tileTypeDefs [72] = Constants.MoreTerra_Color.UNDERGROUNDMUSHROOM_TREES;
			tileTypeDefs [73] = Constants.MoreTerra_Color.PLANTS;
			tileTypeDefs [74] = Constants.MoreTerra_Color.PLANTS;
			tileTypeDefs [75] = Constants.MoreTerra_Color.BLOCK;
			tileTypeDefs [76] = Constants.MoreTerra_Color.BLOCK;
			tileTypeDefs [77] = Constants.MoreTerra_Color.IMPORTANT;
			tileTypeDefs [78] = Constants.MoreTerra_Color.DECORATIVE;
			tileTypeDefs [79] = Constants.MoreTerra_Color.DECORATIVE;

			tileTypeDefs [80] = Constants.MoreTerra_Color.CACTUS;
			tileTypeDefs [81] = Constants.MoreTerra_Color.CORAL;
			tileTypeDefs [82] = Constants.MoreTerra_Color.HERB;
			tileTypeDefs [83] = Constants.MoreTerra_Color.HERB;
			tileTypeDefs [84] = Constants.MoreTerra_Color.HERB;
			tileTypeDefs [85] = Constants.MoreTerra_Color.TOMBSTONE;
			tileTypeDefs [86] = Constants.MoreTerra_Color.DECORATIVE;
			tileTypeDefs [87] = Constants.MoreTerra_Color.DECORATIVE;
			tileTypeDefs [88] = Constants.MoreTerra_Color.DECORATIVE;
			tileTypeDefs [89] = Constants.MoreTerra_Color.DECORATIVE;

			tileTypeDefs [90] = Constants.MoreTerra_Color.DECORATIVE;
			tileTypeDefs [91] = Constants.MoreTerra_Color.DECORATIVE;
			tileTypeDefs [92] = Constants.MoreTerra_Color.LIGHT_SOURCE;
			tileTypeDefs [93] = Constants.MoreTerra_Color.LIGHT_SOURCE;
			tileTypeDefs [94] = Constants.MoreTerra_Color.DECORATIVE;
			tileTypeDefs [95] = Constants.MoreTerra_Color.LIGHT_SOURCE;
			tileTypeDefs [96] = Constants.MoreTerra_Color.DECORATIVE;
			tileTypeDefs [97] = Constants.MoreTerra_Color.DECORATIVE;
			tileTypeDefs [98] = Constants.MoreTerra_Color.LIGHT_SOURCE;
			tileTypeDefs [99] = Constants.MoreTerra_Color.DECORATIVE;

			tileTypeDefs [100] = Constants.MoreTerra_Color.LIGHT_SOURCE;
			tileTypeDefs [101] = Constants.MoreTerra_Color.DECORATIVE;
			tileTypeDefs [102] = Constants.MoreTerra_Color.DECORATIVE;
			tileTypeDefs [103] = Constants.MoreTerra_Color.DECORATIVE;
			tileTypeDefs [104] = Constants.MoreTerra_Color.DECORATIVE;
			tileTypeDefs [105] = Constants.MoreTerra_Color.DECORATIVE;
			tileTypeDefs [106] = Constants.MoreTerra_Color.DECORATIVE;

            tileTypeDefs[107] = Constants.MoreTerra_Color.COBALT;
            tileTypeDefs[108] = Constants.MoreTerra_Color.MYTHRIL;
            tileTypeDefs[109] = Constants.MoreTerra_Color.HALLOWED_PLANTS;
            tileTypeDefs[110] = Constants.MoreTerra_Color.HALLOWED_PLANTS;
            tileTypeDefs[111] = Constants.MoreTerra_Color.ADAMANTITE;
            tileTypeDefs[112] = Constants.MoreTerra_Color.EBONSAND;
            tileTypeDefs[113] = Constants.MoreTerra_Color.HALLOWED_PLANTS;
            tileTypeDefs[114] = Constants.MoreTerra_Color.DECORATIVE;
            tileTypeDefs[115] = Constants.MoreTerra_Color.HALLOWED_VINES;
            tileTypeDefs[116] = Constants.MoreTerra_Color.PEARLSAND;
            tileTypeDefs[117] = Constants.MoreTerra_Color.PEARLSTONE;
            tileTypeDefs[118] = Constants.MoreTerra_Color.BLOCK;
            tileTypeDefs[119] = Constants.MoreTerra_Color.BLOCK;
            tileTypeDefs[120] = Constants.MoreTerra_Color.UNKNOWN;
            tileTypeDefs[121] = Constants.MoreTerra_Color.BLOCK;
            tileTypeDefs[122] = Constants.MoreTerra_Color.BLOCK;
            tileTypeDefs[123] = Constants.MoreTerra_Color.SILT;
            tileTypeDefs[124] = Constants.MoreTerra_Color.WOOD;
            tileTypeDefs[125] = Constants.MoreTerra_Color.DECORATIVE;
            tileTypeDefs[126] = Constants.MoreTerra_Color.DECORATIVE;
            tileTypeDefs[127] = Constants.MoreTerra_Color.BLOCK;
            tileTypeDefs[128] = Constants.MoreTerra_Color.DECORATIVE;
            tileTypeDefs[129] = Constants.MoreTerra_Color.CRYSTALS;
            tileTypeDefs[130] = Constants.MoreTerra_Color.MECHANICAL;
            tileTypeDefs[131] = Constants.MoreTerra_Color.MECHANICAL;
            tileTypeDefs[132] = Constants.MoreTerra_Color.MECHANICAL;
            tileTypeDefs[133] = Constants.MoreTerra_Color.DECORATIVE;
            tileTypeDefs[134] = Constants.MoreTerra_Color.DECORATIVE;
            tileTypeDefs[135] = Constants.MoreTerra_Color.MECHANICAL;
            tileTypeDefs[136] = Constants.MoreTerra_Color.MECHANICAL;
            tileTypeDefs[137] = Constants.MoreTerra_Color.MECHANICAL;
            tileTypeDefs[138] = Constants.MoreTerra_Color.DECORATIVE;
            tileTypeDefs[139] = Constants.MoreTerra_Color.DECORATIVE;
            tileTypeDefs[140] = Constants.MoreTerra_Color.BLOCK;
            tileTypeDefs[141] = Constants.MoreTerra_Color.MECHANICAL;
            tileTypeDefs[142] = Constants.MoreTerra_Color.MECHANICAL;
            tileTypeDefs[143] = Constants.MoreTerra_Color.MECHANICAL;
            tileTypeDefs[144] = Constants.MoreTerra_Color.MECHANICAL;
            tileTypeDefs[145] = Constants.MoreTerra_Color.BLOCK;
            tileTypeDefs[146] = Constants.MoreTerra_Color.BLOCK;
            tileTypeDefs[147] = Constants.MoreTerra_Color.SNOWBLOCK;
            tileTypeDefs[148] = Constants.MoreTerra_Color.BLOCK;
            tileTypeDefs[149] = Constants.MoreTerra_Color.LIGHT_SOURCE;
			
			for (int i = 150; i < 256; i++) {
				tileTypeDefs [i] = System.Drawing.Color.Magenta;
			}

			tileTypeDefs [256] = Constants.MoreTerra_Color.UNKNOWN;
			tileTypeDefs [257] = Constants.MoreTerra_Color.UNKNOWN;
			tileTypeDefs [258] = Constants.MoreTerra_Color.UNKNOWN;
			tileTypeDefs [259] = Constants.MoreTerra_Color.UNKNOWN;
			tileTypeDefs [260] = Constants.MoreTerra_Color.UNKNOWN;
			tileTypeDefs [261] = Constants.MoreTerra_Color.UNKNOWN;
			tileTypeDefs [262] = Constants.MoreTerra_Color.UNKNOWN;
			tileTypeDefs [263] = Constants.MoreTerra_Color.UNKNOWN;
			tileTypeDefs [264] = Constants.MoreTerra_Color.UNKNOWN;

			tileTypeDefs [265] = Constants.MoreTerra_Color.SKY;
			tileTypeDefs [266] = Constants.MoreTerra_Color.WATER;
			tileTypeDefs [267] = Constants.MoreTerra_Color.LAVA;

			// Walls
			tileTypeDefs [268] = Constants.MoreTerra_Color.WALL_STONE;
			tileTypeDefs [269] = Constants.MoreTerra_Color.WALL_DIRT;
			tileTypeDefs [270] = Constants.MoreTerra_Color.WALL_EBONSTONE;
			tileTypeDefs [271] = Constants.MoreTerra_Color.WALL_WOOD;
			tileTypeDefs [272] = Constants.MoreTerra_Color.WALL_BRICK;
			tileTypeDefs [273] = Constants.MoreTerra_Color.WALL_BRICK;
			tileTypeDefs [274] = Constants.MoreTerra_Color.WALL_DUNGEON_BLUE;
			tileTypeDefs [275] = Constants.MoreTerra_Color.WALL_DUNGEON_GREEN;
			tileTypeDefs [276] = Constants.MoreTerra_Color.WALL_DUNGEON_PINK;
			tileTypeDefs [277] = Constants.MoreTerra_Color.WALL_BRICK;
			tileTypeDefs [278] = Constants.MoreTerra_Color.WALL_BRICK;
			tileTypeDefs [279] = Constants.MoreTerra_Color.WALL_BRICK;
			tileTypeDefs [280] = Constants.MoreTerra_Color.WALL_HELLSTONE;
			tileTypeDefs [281] = Constants.MoreTerra_Color.WALL_OBSIDIAN;
			tileTypeDefs [282] = Constants.MoreTerra_Color.WALL_MUD;
			tileTypeDefs [283] = Constants.MoreTerra_Color.WALL_DIRT;
			tileTypeDefs [284] = Constants.MoreTerra_Color.WALL_DUNGEON_BLUE;
			tileTypeDefs [285] = Constants.MoreTerra_Color.WALL_DUNGEON_GREEN;
			tileTypeDefs [286] = Constants.MoreTerra_Color.WALL_DUNGEON_PINK;
			tileTypeDefs [287] = Constants.MoreTerra_Color.WALL_DIRT;
			tileTypeDefs [288] = Constants.MoreTerra_Color.WALL_BRICK;
            tileTypeDefs[289] = Constants.MoreTerra_Color.WALL_BRICK;
            tileTypeDefs[290] = Constants.MoreTerra_Color.WALL_BRICK;
            tileTypeDefs[291] = Constants.MoreTerra_Color.WALL_BRICK;
            tileTypeDefs[292] = Constants.MoreTerra_Color.WALL_BRICK;
            tileTypeDefs[293] = Constants.MoreTerra_Color.WALL_BRICK;
            tileTypeDefs[294] = Constants.MoreTerra_Color.WALL_WOOD;
            tileTypeDefs[295] = Constants.MoreTerra_Color.WALL_BRICK;
            tileTypeDefs[296] = Constants.MoreTerra_Color.WALL_BRICK;
            tileTypeDefs[297] = Constants.MoreTerra_Color.WALL_BRICK;
            tileTypeDefs[298] = Constants.MoreTerra_Color.WALL_BRICK;
			
			tileTypeDefs [330] = Constants.MoreTerra_Color.WALL_OBSIDIAN; //my addition
		}
	}
}
