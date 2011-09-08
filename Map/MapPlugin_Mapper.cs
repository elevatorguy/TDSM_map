using System.Drawing;
using System.Collections.Generic;
using Terraria_Server.Logging;
using Terraria_Server;

namespace MapPlugin
{
	public partial class MapPlugin
	{
		public static Dictionary<int, Color> tileTypeDefs;
		
		public static Bitmap mapWorld (Bitmap bmp) 
		{
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
									bmp.SetPixel (i, j, Constants.Colors.WALL_BACKGROUND);
								}
								if (Main.tile.At (i, j).Liquid > 0) {
									if (Main.tile.At (i, j).Lava) {
										bmp.SetPixel (i, j, Constants.Colors.LAVA);
									} else {
										bmp.SetPixel (i, j, Constants.Colors.WATER);
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
			return bmp;
		}
		
		public class Constants //credits go to the authors of MoreTerra
		{ 
			public static class Colors 
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
			
			tileTypeDefs [0] = Constants.Colors.DIRT;
			tileTypeDefs [1] = Constants.Colors.STONE;
			tileTypeDefs [2] = Constants.Colors.GRASS;
			tileTypeDefs [3] = Constants.Colors.PLANTS;
			tileTypeDefs [4] = Constants.Colors.LIGHT_SOURCE;
			tileTypeDefs [5] = Constants.Colors.WOOD;
			tileTypeDefs [6] = Constants.Colors.IRON;
			tileTypeDefs [7] = Constants.Colors.COPPER;
			tileTypeDefs [8] = Constants.Colors.GOLD;
			tileTypeDefs [9] = Constants.Colors.SILVER;

			tileTypeDefs [10] = Constants.Colors.DECORATIVE;
			tileTypeDefs [11] = Constants.Colors.DECORATIVE;
			tileTypeDefs [12] = Constants.Colors.IMPORTANT;
			tileTypeDefs [13] = Constants.Colors.DECORATIVE;
			tileTypeDefs [14] = Constants.Colors.DECORATIVE;
			tileTypeDefs [15] = Constants.Colors.DECORATIVE;
			tileTypeDefs [16] = Constants.Colors.DECORATIVE;
			tileTypeDefs [17] = Constants.Colors.DECORATIVE;
			tileTypeDefs [18] = Constants.Colors.DECORATIVE;
			tileTypeDefs [19] = Constants.Colors.WOOD;

			tileTypeDefs [20] = Constants.Colors.PLANTS;
			tileTypeDefs [21] = Constants.Colors.IMPORTANT;
			tileTypeDefs [22] = Constants.Colors.DEMONITE;
			tileTypeDefs [23] = Constants.Colors.CORRUPTION_GRASS;
			tileTypeDefs [24] = Constants.Colors.CORRUPTION_GRASS;
			tileTypeDefs [25] = Constants.Colors.EBONSTONE;
			tileTypeDefs [26] = Constants.Colors.IMPORTANT;
			tileTypeDefs [27] = Constants.Colors.PLANTS;
			tileTypeDefs [28] = Constants.Colors.IMPORTANT;
			tileTypeDefs [29] = Constants.Colors.DECORATIVE;

			tileTypeDefs [30] = Constants.Colors.WOOD_BLOCK;
			tileTypeDefs [31] = Constants.Colors.IMPORTANT;
			tileTypeDefs [32] = Constants.Colors.CORRUPTION_VINES;
			tileTypeDefs [33] = Constants.Colors.LIGHT_SOURCE;
			tileTypeDefs [34] = Constants.Colors.LIGHT_SOURCE;
			tileTypeDefs [35] = Constants.Colors.LIGHT_SOURCE;
			tileTypeDefs [36] = Constants.Colors.LIGHT_SOURCE;
			tileTypeDefs [37] = Constants.Colors.METEORITE;
			tileTypeDefs [38] = Constants.Colors.BLOCK;
			tileTypeDefs [39] = Constants.Colors.BLOCK;

			tileTypeDefs [40] = Constants.Colors.CLAY;
			tileTypeDefs [41] = Constants.Colors.DUNGEON_BLUE;
			tileTypeDefs [42] = Constants.Colors.LIGHT_SOURCE;
			tileTypeDefs [43] = Constants.Colors.DUNGEON_GREEN;
			tileTypeDefs [44] = Constants.Colors.DUNGEON_PINK;
			tileTypeDefs [45] = Constants.Colors.BLOCK;
			tileTypeDefs [46] = Constants.Colors.BLOCK;
			tileTypeDefs [47] = Constants.Colors.BLOCK;
			tileTypeDefs [48] = Constants.Colors.SPIKES;
			tileTypeDefs [49] = Constants.Colors.LIGHT_SOURCE;

			tileTypeDefs [50] = Constants.Colors.DECORATIVE;
			tileTypeDefs [51] = Constants.Colors.WEB;
			tileTypeDefs [52] = Constants.Colors.PLANTS;
			tileTypeDefs [53] = Constants.Colors.SAND;
			tileTypeDefs [54] = Constants.Colors.DECORATIVE;
			tileTypeDefs [55] = Constants.Colors.DECORATIVE;
			tileTypeDefs [56] = Constants.Colors.OBSIDIAN;
			tileTypeDefs [57] = Constants.Colors.ASH;
			tileTypeDefs [58] = Constants.Colors.HELLSTONE;
			tileTypeDefs [59] = Constants.Colors.MUD;

			tileTypeDefs [60] = Constants.Colors.UNDERGROUNDJUNGLE_GRASS;
			tileTypeDefs [61] = Constants.Colors.UNDERGROUNDJUNGLE_PLANTS;
			tileTypeDefs [62] = Constants.Colors.UNDERGROUNDJUNGLE_VINES;
			tileTypeDefs [63] = Constants.Colors.GEMS;
			tileTypeDefs [64] = Constants.Colors.GEMS;
			tileTypeDefs [65] = Constants.Colors.GEMS;
			tileTypeDefs [66] = Constants.Colors.GEMS;
			tileTypeDefs [67] = Constants.Colors.GEMS;
			tileTypeDefs [68] = Constants.Colors.GEMS;
			tileTypeDefs [69] = Constants.Colors.UNDERGROUNDJUNGLE_THORNS;

			tileTypeDefs [70] = Constants.Colors.UNDERGROUNDMUSHROOM_GRASS;
			tileTypeDefs [71] = Constants.Colors.UNDERGROUNDMUSHROOM_PLANTS;
			tileTypeDefs [72] = Constants.Colors.UNDERGROUNDMUSHROOM_TREES;
			tileTypeDefs [73] = Constants.Colors.PLANTS;
			tileTypeDefs [74] = Constants.Colors.PLANTS;
			tileTypeDefs [75] = Constants.Colors.BLOCK;
			tileTypeDefs [76] = Constants.Colors.BLOCK;
			tileTypeDefs [77] = Constants.Colors.IMPORTANT;
			tileTypeDefs [78] = Constants.Colors.DECORATIVE;
			tileTypeDefs [79] = Constants.Colors.DECORATIVE;

			tileTypeDefs [80] = Constants.Colors.CACTUS;
			tileTypeDefs [81] = Constants.Colors.CORAL;
			tileTypeDefs [82] = Constants.Colors.HERB;
			tileTypeDefs [83] = Constants.Colors.HERB;
			tileTypeDefs [84] = Constants.Colors.HERB;
			tileTypeDefs [85] = Constants.Colors.TOMBSTONE;
			tileTypeDefs [86] = Constants.Colors.DECORATIVE;
			tileTypeDefs [87] = Constants.Colors.DECORATIVE;
			tileTypeDefs [88] = Constants.Colors.DECORATIVE;
			tileTypeDefs [89] = Constants.Colors.DECORATIVE;

			tileTypeDefs [90] = Constants.Colors.DECORATIVE;
			tileTypeDefs [91] = Constants.Colors.DECORATIVE;
			tileTypeDefs [92] = Constants.Colors.LIGHT_SOURCE;
			tileTypeDefs [93] = Constants.Colors.LIGHT_SOURCE;
			tileTypeDefs [94] = Constants.Colors.DECORATIVE;
			tileTypeDefs [95] = Constants.Colors.LIGHT_SOURCE;
			tileTypeDefs [96] = Constants.Colors.DECORATIVE;
			tileTypeDefs [97] = Constants.Colors.DECORATIVE;
			tileTypeDefs [98] = Constants.Colors.LIGHT_SOURCE;
			tileTypeDefs [99] = Constants.Colors.DECORATIVE;

			tileTypeDefs [100] = Constants.Colors.LIGHT_SOURCE;
			tileTypeDefs [101] = Constants.Colors.DECORATIVE;
			tileTypeDefs [102] = Constants.Colors.DECORATIVE;
			tileTypeDefs [103] = Constants.Colors.DECORATIVE;
			tileTypeDefs [104] = Constants.Colors.DECORATIVE;
			tileTypeDefs [105] = Constants.Colors.DECORATIVE;
			tileTypeDefs [106] = Constants.Colors.DECORATIVE;

			
			for (int i = 107; i < 256; i++) {
				tileTypeDefs [i] = Color.Magenta;
			}

			tileTypeDefs [256] = Constants.Colors.UNKNOWN;
			tileTypeDefs [257] = Constants.Colors.UNKNOWN;
			tileTypeDefs [258] = Constants.Colors.UNKNOWN;
			tileTypeDefs [259] = Constants.Colors.UNKNOWN;
			tileTypeDefs [260] = Constants.Colors.UNKNOWN;
			tileTypeDefs [261] = Constants.Colors.UNKNOWN;
			tileTypeDefs [262] = Constants.Colors.UNKNOWN;
			tileTypeDefs [263] = Constants.Colors.UNKNOWN;
			tileTypeDefs [264] = Constants.Colors.UNKNOWN;

			tileTypeDefs [265] = Constants.Colors.SKY;
			tileTypeDefs [266] = Constants.Colors.WATER;
			tileTypeDefs [267] = Constants.Colors.LAVA;

			// Walls
			tileTypeDefs [268] = Constants.Colors.WALL_STONE;
			tileTypeDefs [269] = Constants.Colors.WALL_DIRT;
			tileTypeDefs [270] = Constants.Colors.WALL_EBONSTONE;
			tileTypeDefs [271] = Constants.Colors.WALL_WOOD;
			tileTypeDefs [272] = Constants.Colors.WALL_BRICK;
			tileTypeDefs [273] = Constants.Colors.WALL_BRICK;
			tileTypeDefs [274] = Constants.Colors.WALL_DUNGEON_BLUE;
			tileTypeDefs [275] = Constants.Colors.WALL_DUNGEON_GREEN;
			tileTypeDefs [276] = Constants.Colors.WALL_DUNGEON_PINK;
			tileTypeDefs [277] = Constants.Colors.WALL_BRICK;
			tileTypeDefs [278] = Constants.Colors.WALL_BRICK;
			tileTypeDefs [279] = Constants.Colors.WALL_BRICK;
			tileTypeDefs [280] = Constants.Colors.WALL_HELLSTONE;
			tileTypeDefs [281] = Constants.Colors.WALL_OBSIDIAN;
			tileTypeDefs [282] = Constants.Colors.WALL_MUD;
			tileTypeDefs [283] = Constants.Colors.WALL_DIRT;
			tileTypeDefs [284] = Constants.Colors.WALL_DUNGEON_BLUE;
			tileTypeDefs [285] = Constants.Colors.WALL_DUNGEON_GREEN;
			tileTypeDefs [286] = Constants.Colors.WALL_DUNGEON_PINK;
			tileTypeDefs [287] = Constants.Colors.WALL_BACKGROUND;
			tileTypeDefs [288] = Constants.Colors.WALL_BACKGROUND;
			
			tileTypeDefs [330] = Constants.Colors.WALL_OBSIDIAN; //my addition
		}
	}
}
