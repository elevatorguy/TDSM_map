using System.Drawing;
using System.Collections.Generic;
using Terraria_Server.Logging;
using Terraria_Server;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;

namespace MapPlugin
{
	public partial class MapPlugin
	{
		public static Dictionary<int, Color> tileTypeDefs;
		
		public void mapWorld () 
		{	
			Stopwatch stopwatch = new Stopwatch ();		
			Server.notifyOps("Saving Image...", true);
			stopwatch.Start ();
			bmp = new Bitmap (Main.maxTilesX, Main.maxTilesY, PixelFormat.Format32bppArgb);
			Graphics graphicsHandle = Graphics.FromImage ((Image)bmp);
			graphicsHandle.FillRectangle (new SolidBrush (Constants.MoreTerra_Color.SKY), 0, 0, bmp.Width, bmp.Height);
			
			using (var prog = new ProgressLogger(Main.maxTilesX - 1, "Saving image data"))
				for (int i = 0; i < Main.maxTilesX; i++) {
					prog.Value = i;
					for (int j = 0; j < Main.maxTilesY; j++) {		
					
					//TODO: find a more understandable way on these if statements
						if (Main.tile.At (i, j).Wall == 0) {
							if (Main.tile.At (i, j).Active) {
								bmp.SetPixel (i, j, tileTypeDefs [Main.tile.At (i, j).Type]);
							} else {
								
								if (j > Main.worldSurface) {
									bmp.SetPixel (i, j, Constants.MoreTerra_Color.WALL_BACKGROUND);
								}
								if (Main.tile.At (i, j).Liquid > 0) {
									if (Main.tile.At (i, j).Lava) {
										bmp.SetPixel (i, j, Constants.MoreTerra_Color.LAVA);
									} else {
										bmp.SetPixel (i, j, Constants.MoreTerra_Color.WATER);
									}	
								}
							}
						} else {
							if (Main.tile.At (i, j).Active) {
								bmp.SetPixel (i, j, tileTypeDefs [Main.tile.At (i, j).Type]);
							} else {
								bmp.SetPixel (i, j, tileTypeDefs [Main.tile.At (i, j).Wall + 267]);
							}
						}
						
					}
				}
				Server.notifyOps("Saving Data...", true);
				bmp.Save (string.Concat (p, Path.DirectorySeparatorChar, filename));
				stopwatch.Stop ();
				ProgramLog.Log ("Save duration: " + stopwatch.Elapsed.Seconds + " Second(s)");
				Server.notifyOps("Saving Complete.", true);
				bmp = null;
                isMapping = false;
		}
		
		public partial class Constants //credits go to the authors of MoreTerra
		{ 
			public static class MoreTerra_Color 
			{
				public static Color DIRT = Color.FromArgb (175, 131, 101);
				public static Color STONE = Color.FromArgb (128, 128, 128);
				public static Color GRASS = Color.FromArgb (28, 216, 94);
				public static Color PLANTS = Color.FromArgb (13, 101, 36);
				public static Color LIGHT_SOURCE = Color.FromArgb (253, 62, 3);
				public static Color IRON = Color.FromArgb (189, 159, 139);
				public static Color COPPER = Color.FromArgb (255, 149, 50);
				public static Color GOLD = Color.FromArgb (185, 164, 23);
				public static Color WOOD = Color.FromArgb (86, 62, 44);
				public static Color WOOD_BLOCK = Color.FromArgb (168, 121, 87);
				public static Color SILVER = Color.FromArgb (217, 223, 223);
				public static Color DECORATIVE = Color.FromArgb (0, 255, 242);
				public static Color IMPORTANT = Color.FromArgb (255, 0, 0);
				public static Color DEMONITE = Color.FromArgb (98, 95, 167);
				public static Color CORRUPTION_GRASS = Color.FromArgb (141, 137, 223);
				public static Color EBONSTONE = Color.FromArgb (75, 74, 130);
				public static Color CORRUPTION_VINES = Color.FromArgb (122, 97, 143);
				public static Color BLOCK = Color.FromArgb (178, 0, 255);
				public static Color METEORITE = Color.Magenta;//Color.FromArgb(223, 159, 137);
				public static Color CLAY = Color.FromArgb (216, 115, 101);
				public static Color DUNGEON_GREEN = Color.FromArgb (26, 136, 34);
				public static Color DUNGEON_PINK = Color.FromArgb (169, 49, 117);
				public static Color DUNGEON_BLUE = Color.FromArgb (66, 69, 194);
				public static Color SPIKES = Color.FromArgb (109, 109, 109);
				public static Color WEB = Color.FromArgb (255, 255, 255);
				public static Color SAND = Color.FromArgb (255, 218, 56);
				public static Color OBSIDIAN = Color.FromArgb (87, 81, 173);
				public static Color ASH = Color.FromArgb (68, 68, 76);
				public static Color HELLSTONE = Color.FromArgb (102, 34, 34);
				public static Color MUD = Color.FromArgb (92, 68, 73);
				public static Color UNDERGROUNDJUNGLE_GRASS = Color.FromArgb (143, 215, 29);
				public static Color UNDERGROUNDJUNGLE_PLANTS = Color.FromArgb (143, 215, 29);
				public static Color UNDERGROUNDJUNGLE_VINES = Color.FromArgb (138, 206, 28);
				public static Color UNDERGROUNDJUNGLE_THORNS = Color.FromArgb (94, 48, 55);
				public static Color GEMS = Color.FromArgb (42, 130, 250);
				public static Color CACTUS = Color.DarkGreen;
				public static Color CORAL = Color.LightPink;
				public static Color HERB = Color.OliveDrab;
				public static Color TOMBSTONE = Color.DimGray;
				public static Color UNDERGROUNDMUSHROOM_GRASS = Color.FromArgb (93, 127, 255);
				public static Color UNDERGROUNDMUSHROOM_PLANTS = Color.FromArgb (177, 174, 131);
				public static Color UNDERGROUNDMUSHROOM_TREES = Color.FromArgb (150, 143, 110);
				public static Color LAVA = Color.FromArgb (255, 72, 0);
				public static Color WATER = Color.FromArgb (0, 12, 255);
				public static Color SKY = Color.FromArgb (155, 209, 255);
				public static Color WALL_STONE = Color.FromArgb (66, 66, 66);
				public static Color WALL_DIRT = Color.FromArgb (88, 61, 46);
				public static Color WALL_EBONSTONE = Color.FromArgb (61, 58, 78);
				public static Color WALL_WOOD = Color.FromArgb (73, 51, 36);
				public static Color WALL_BRICK = Color.FromArgb (60, 60, 60);
				public static Color WALL_BACKGROUND = Color.FromArgb (50, 50, 60);
				public static Color WALL_DUNGEON_PINK = Color.FromArgb (84, 25, 60);
				public static Color WALL_DUNGEON_BLUE = Color.FromArgb (29, 31, 72);
				public static Color WALL_DUNGEON_GREEN = Color.FromArgb (14, 68, 16);
				public static Color WALL_MUD = Color.FromArgb (61, 46, 49);
				public static Color WALL_HELLSTONE = Color.FromArgb (48, 21, 21);
				public static Color WALL_OBSIDIAN = Color.FromArgb (87, 81, 173);
				public static Color UNKNOWN = Color.Magenta;
			}
		}
		
		public static void InitializeMapperDefs () //Credits go to the authors of MoreTerra
		{	
			tileTypeDefs = new Dictionary<int, Color> (255);
			
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

			
			for (int i = 107; i < 256; i++) {
				tileTypeDefs [i] = Color.Magenta;
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
			tileTypeDefs [287] = Constants.MoreTerra_Color.WALL_BACKGROUND;
			tileTypeDefs [288] = Constants.MoreTerra_Color.WALL_BACKGROUND;
			
			tileTypeDefs [330] = Constants.MoreTerra_Color.WALL_OBSIDIAN; //my addition
		}
	}
}
