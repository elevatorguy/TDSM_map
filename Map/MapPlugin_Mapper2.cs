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
		public static Dictionary<int, Color> tileTypeDefs2;
		public static Bitmap bmp;
		
		public void mapWorld2 () 
		{	
			Stopwatch stopwatch = new Stopwatch ();		
			Program.server.notifyOps("Saving Image...", true);
			stopwatch.Start ();
			bmp = new Bitmap (Main.maxTilesX, Main.maxTilesY, PixelFormat.Format32bppArgb);
			Graphics graphicsHandle = Graphics.FromImage ((Image)bmp);
			graphicsHandle.FillRectangle (new SolidBrush (Constants.Terrafirma_Color.SKY), 0, 0, bmp.Width, bmp.Height);
			
			using (var prog = new ProgressLogger(Main.maxTilesX - 1, "Saving image data"))
				for (int i = 0; i < Main.maxTilesX; i++) {
					prog.Value = i;
					for (int j = 0; j < Main.maxTilesY; j++) {		
					
					//TODO: find a more understandable way on these if statements
						if (Main.tile.At (i, j).Wall == 0) {
							if (Main.tile.At (i, j).Active) {
								bmp.SetPixel (i, j, tileTypeDefs2 [Main.tile.At (i, j).Type]);
							} else {
								
								if (j > Main.worldSurface) {
									bmp.SetPixel (i, j, Constants.Terrafirma_Color.ROCK);
								}
								if (Main.tile.At (i, j).Liquid > 0) {
									if (Main.tile.At (i, j).Lava) {
										bmp.SetPixel (i, j, Constants.Terrafirma_Color.LAVA);
									} else {
										bmp.SetPixel (i, j, Constants.Terrafirma_Color.WATER);
									}	
								}
							}
						} else {
							if (Main.tile.At (i, j).Active) {
								bmp.SetPixel (i, j, tileTypeDefs2 [Main.tile.At (i, j).Type]);
							} else {
								bmp.SetPixel (i, j, tileTypeDefs2 [Main.tile.At (i, j).Wall + 267]);
							}
						}
						
					}
				}
				Program.server.notifyOps("Saving Data...", true);
				bmp.Save (string.Concat (p, Path.DirectorySeparatorChar, filename));
				stopwatch.Stop ();
				ProgramLog.Log ("Save duration: " + stopwatch.Elapsed.Seconds + " Second(s)");
				Program.server.notifyOps("Saving Complete.", true);
				bmp = null;
		}
		
		public partial class Constants //credits go to the authors of Terrafirma ..damn that xml took awhile to manually convert :(
								
		{ 
			public static class Terrafirma_Color 
			{
				//tiles
				public static Color DIRT = ColorTranslator.FromHtml("#976B4B");
				public static Color STONE = ColorTranslator.FromHtml("#808080");
				public static Color GRASS = ColorTranslator.FromHtml("#1CD85E");
				public static Color WEEDS = ColorTranslator.FromHtml("#1E9648");
				public static Color TORCH = ColorTranslator.FromHtml("#FDDD03");
				public static Color TREE = ColorTranslator.FromHtml("#976B4B");
				public static Color IRON_ORE = ColorTranslator.FromHtml("#B5A495");
				public static Color COPPER_ORE = ColorTranslator.FromHtml("#964316");
				public static Color GOLD_ORE = ColorTranslator.FromHtml("#B9A417");
				public static Color SILVER_ORE = ColorTranslator.FromHtml("#D9DFDF");
				public static Color CLOSED_DOOR = ColorTranslator.FromHtml("#BF8F6F");
				public static Color OPEN_DOOR = ColorTranslator.FromHtml("#946B50");
				public static Color HEARTSTONE = ColorTranslator.FromHtml("#B61239");
				public static Color BOTTLE = ColorTranslator.FromHtml("#4EC5FC");
				public static Color TABLE = ColorTranslator.FromHtml("#7F5C45");
				public static Color CHAIR = ColorTranslator.FromHtml("#A2785C");
				public static Color ANVIL = ColorTranslator.FromHtml("#505050");
				public static Color FURNACE = ColorTranslator.FromHtml("#636363");
				public static Color WORKBENCH = ColorTranslator.FromHtml("#7F5C45");
				public static Color WOODEN_PLATFORM = ColorTranslator.FromHtml("#B18567");
				public static Color SAPLING = ColorTranslator.FromHtml("#1E9648");
				public static Color CHEST = ColorTranslator.FromHtml("#946B50");
				public static Color DEMONITE_ORE = ColorTranslator.FromHtml("#625FA7");
				public static Color CORRUPTED_GRASS = ColorTranslator.FromHtml("#8D89DF");
				public static Color CORRUPTED_WEEDS = ColorTranslator.FromHtml("#6D6AAE");
				public static Color EBONSTONE = ColorTranslator.FromHtml("#7D7991");
				public static Color DEMON_ALTAR = ColorTranslator.FromHtml("#5E5561");
				public static Color SUNFLOWER = ColorTranslator.FromHtml("#E3B903");
				public static Color POT = ColorTranslator.FromHtml("#796E61");
				public static Color PIGGY_BANK = ColorTranslator.FromHtml("#9C546C");
				public static Color WOOD = ColorTranslator.FromHtml("#A97D5D");
				public static Color SHADOW_ORB = ColorTranslator.FromHtml("#674D62");
				public static Color CORRUPTED_VINES = ColorTranslator.FromHtml("#7A618F");
				public static Color CANDLE = ColorTranslator.FromHtml("#FDDD03");
				public static Color COPPER_CHANDELIER = ColorTranslator.FromHtml("#B75819");
				public static Color SILVER_CHANDELIER = ColorTranslator.FromHtml("#C1CACB");
				public static Color GOLD_CHANDELIER = ColorTranslator.FromHtml("#B9A417");
				public static Color METEORITE = ColorTranslator.FromHtml("#685654");
				public static Color GRAY_BRICK = ColorTranslator.FromHtml("#8C8C8C");
				public static Color CLAY_BRICK = ColorTranslator.FromHtml("#C37057");
				public static Color CLAY = ColorTranslator.FromHtml("#925144");
				public static Color BLUE_BRICK = ColorTranslator.FromHtml("#6365C9");
				public static Color LIGHT_GLOBE = ColorTranslator.FromHtml("#F99851");
				public static Color GREEN_BRICK = ColorTranslator.FromHtml("#3FA931");
				public static Color PINK_BRICK = ColorTranslator.FromHtml("#A93175");
				public static Color GOLD_BRICK = ColorTranslator.FromHtml("#CCB548");
				public static Color SILVER_BRICK = ColorTranslator.FromHtml("#AEC1C2");
				public static Color COPPER_BRICK = ColorTranslator.FromHtml("#CD7D47");
				public static Color SPIKES = ColorTranslator.FromHtml("#AFAFAF");
				public static Color BLUE_CANDLE = ColorTranslator.FromHtml("#0B2EFF");
				public static Color BOOKS = ColorTranslator.FromHtml("#3095AA");
				public static Color COBWEBS = ColorTranslator.FromHtml("#9EADAE");
				public static Color VINES = ColorTranslator.FromHtml("#1E9648");
				public static Color SAND = ColorTranslator.FromHtml("#D3C66F");
				public static Color GLASS = ColorTranslator.FromHtml("#C8F6FE");
				public static Color SIGN = ColorTranslator.FromHtml("#7F5C45");
				public static Color OBSIDIAN = ColorTranslator.FromHtml("#5751AD");
				public static Color ASH = ColorTranslator.FromHtml("#44444C");
				public static Color HELLSTONE = ColorTranslator.FromHtml("#8E4242");
				public static Color MUD = ColorTranslator.FromHtml("#5C4449");
				public static Color JUNGLE_GRASS = ColorTranslator.FromHtml("#8FD71D");
				public static Color JUNGLE_WEEDS = ColorTranslator.FromHtml("#63971F");
				public static Color JUNGLE_VINES = ColorTranslator.FromHtml("#28650D");
				public static Color SAPPHIRE = ColorTranslator.FromHtml("#2A82FA");
				public static Color RUBY = ColorTranslator.FromHtml("#FA2A51");
				public static Color EMERALD = ColorTranslator.FromHtml("#05C95D");
				public static Color TOPAZ = ColorTranslator.FromHtml("#C78B09");
				public static Color AMETHYST = ColorTranslator.FromHtml("#A30BD5");
				public static Color DIAMOND = ColorTranslator.FromHtml("#19D1E7");
				public static Color JUNGLE_THORN = ColorTranslator.FromHtml("#855141");
				public static Color MUSHROOM_GRASS = ColorTranslator.FromHtml("#5D7FFF");
				public static Color MUSHROOM = ColorTranslator.FromHtml("#B1AE83");
				public static Color MUSHROOM_TREE = ColorTranslator.FromHtml("#968F6E");
				public static Color WEEDS_73 = ColorTranslator.FromHtml("#0D6524");
				public static Color WEEDS_74 = ColorTranslator.FromHtml("#28650D");
				public static Color OBSIDIAN_BRICK = ColorTranslator.FromHtml("#665CC2");
				public static Color HELLSTONE_BRICK = ColorTranslator.FromHtml("#8E4242");
				public static Color HELLFORGE = ColorTranslator.FromHtml("#EE6646");
				public static Color CLAY_POT = ColorTranslator.FromHtml("#796E61");
				public static Color BED = ColorTranslator.FromHtml("#5C6298");
				public static Color CACTUS = ColorTranslator.FromHtml("#497811");
				public static Color CORAL = ColorTranslator.FromHtml("#e5533f");
				public static Color HERB_SPROUTS = ColorTranslator.FromHtml("#fe5402");
				public static Color HERB_STALKS = ColorTranslator.FromHtml("#fe5402");
				public static Color HERBS = ColorTranslator.FromHtml("#fe5402");
				public static Color TOMBSTONE = ColorTranslator.FromHtml("#c0c0c0");
				public static Color LOOM = ColorTranslator.FromHtml("#7F5C45");
				public static Color PIANO = ColorTranslator.FromHtml("#584430");
				public static Color DRESSER = ColorTranslator.FromHtml("#906850");
				public static Color BENCH = ColorTranslator.FromHtml("#B18567");
				public static Color BATHTUB = ColorTranslator.FromHtml("#606060");
				public static Color BANNER = ColorTranslator.FromHtml("#188008");
				public static Color LAMP_POST = ColorTranslator.FromHtml("#323232");
				public static Color TIKI_TORCH = ColorTranslator.FromHtml("#503B2F");
				public static Color KEG = ColorTranslator.FromHtml("#A87858");
				public static Color CHINESE_LANTERN = ColorTranslator.FromHtml("#F87800");
				public static Color COOKING_POT = ColorTranslator.FromHtml("#606060");
				public static Color SAFE = ColorTranslator.FromHtml("#808080");
				public static Color SKULL_LANTERN = ColorTranslator.FromHtml("#B2B28A");
				public static Color TRASH_CAN = ColorTranslator.FromHtml("#808080");
				public static Color CANDELABRA = ColorTranslator.FromHtml("#CCB548");
				public static Color BOOKCASE = ColorTranslator.FromHtml("#B08460");
				public static Color THRONE = ColorTranslator.FromHtml("#780C08");
				public static Color BOWL = ColorTranslator.FromHtml("#8D624D");
				public static Color GRANDFATHER_CLOCK = ColorTranslator.FromHtml("#946B50");
				public static Color STATUE = ColorTranslator.FromHtml("#282828");
				
				//walls
				public static Color STONE_WALL = ColorTranslator.FromHtml("#343434");
				public static Color DIRT_WALL = ColorTranslator.FromHtml("#583D2E");
				public static Color STONE_WALL2 = ColorTranslator.FromHtml("#3D3A4E"); //not sure why there was two of these
				public static Color WOOD_WALL = ColorTranslator.FromHtml("#523C2D");
				public static Color BRICK_WALL = ColorTranslator.FromHtml("#464646");
				public static Color RED_BRICK_WALL = ColorTranslator.FromHtml("#5B1E1E");
				public static Color BLUE_BRICK_WALL = ColorTranslator.FromHtml("#212462");
				public static Color GREEN_BRICK_WALL = ColorTranslator.FromHtml("#0E4410");
				public static Color PINK_BRICK_WALL = ColorTranslator.FromHtml("#440E31");
				public static Color GOLD_BRICK_WALL = ColorTranslator.FromHtml("#4A3E0C");
				public static Color SILVER_BRICK_WALL = ColorTranslator.FromHtml("#576162");
				public static Color COPPER_BRICK_WALL = ColorTranslator.FromHtml("#4B200B");
				public static Color HELLSTONE_BRICK_WALL = ColorTranslator.FromHtml("#301515");
				public static Color OBSIDIAN_WALL = ColorTranslator.FromHtml("#332F60");
				public static Color MUD_WALL = ColorTranslator.FromHtml("#31282B");
				public static Color DIRT_WALL2 = ColorTranslator.FromHtml("#583D2E"); //not sure why there was two of these
				public static Color DARK_BLUE_BRICK_WALL = ColorTranslator.FromHtml("#2A2D48");
				public static Color DARK_GREEN_BRICK_WALL = ColorTranslator.FromHtml("#4F4F43");
				public static Color DARK_PINK_BRICK_WALL = ColorTranslator.FromHtml("#543E40");
				public static Color DARK_OBSIDIAN_WALL = ColorTranslator.FromHtml("#332F60");
				
				//global
				public static Color SKY = ColorTranslator.FromHtml("#84AAF8");
				public static Color EARTH = ColorTranslator.FromHtml("#583D2E");
				public static Color ROCK = ColorTranslator.FromHtml("#4A433C");
				public static Color HELL = ColorTranslator.FromHtml("#000000");
				public static Color LAVA = ColorTranslator.FromHtml("#fd2003");
				public static Color WATER = ColorTranslator.FromHtml("#093dbf");

			}
		}
		
		public static void InitializeMapperDefs2 () //Credits go to the authors of MoreTerra
		{	
			tileTypeDefs2 = new Dictionary<int, Color> (255);
			
			//tiles
			tileTypeDefs2 [0] = Constants.Terrafirma_Color.DIRT;
			tileTypeDefs2 [1] = Constants.Terrafirma_Color.STONE;
			tileTypeDefs2 [2] = Constants.Terrafirma_Color.GRASS;
			tileTypeDefs2 [3] = Constants.Terrafirma_Color.WEEDS;
			tileTypeDefs2 [4] = Constants.Terrafirma_Color.TORCH;
			tileTypeDefs2 [5] = Constants.Terrafirma_Color.TREE;
			tileTypeDefs2 [6] = Constants.Terrafirma_Color.IRON_ORE;
			tileTypeDefs2 [7] = Constants.Terrafirma_Color.COPPER_ORE;
			tileTypeDefs2 [8] = Constants.Terrafirma_Color.GOLD_ORE;
			tileTypeDefs2 [9] = Constants.Terrafirma_Color.SILVER_ORE;
			tileTypeDefs2 [10] = Constants.Terrafirma_Color.CLOSED_DOOR;
			tileTypeDefs2 [11] = Constants.Terrafirma_Color.OPEN_DOOR;
			tileTypeDefs2 [12] = Constants.Terrafirma_Color.HEARTSTONE;
			tileTypeDefs2 [13] = Constants.Terrafirma_Color.BOTTLE;
			tileTypeDefs2 [14] = Constants.Terrafirma_Color.TABLE;
			tileTypeDefs2 [15] = Constants.Terrafirma_Color.CHAIR;
			tileTypeDefs2 [16] = Constants.Terrafirma_Color.ANVIL;
			tileTypeDefs2 [17] = Constants.Terrafirma_Color.FURNACE;
			tileTypeDefs2 [18] = Constants.Terrafirma_Color.WORKBENCH;
			tileTypeDefs2 [19] = Constants.Terrafirma_Color.WOODEN_PLATFORM;
			tileTypeDefs2 [20] = Constants.Terrafirma_Color.SAPLING;
			tileTypeDefs2 [21] = Constants.Terrafirma_Color.CHEST;
			tileTypeDefs2 [22] = Constants.Terrafirma_Color.DEMONITE_ORE;
			tileTypeDefs2 [23] = Constants.Terrafirma_Color.CORRUPTED_GRASS;
			tileTypeDefs2 [24] = Constants.Terrafirma_Color.CORRUPTED_WEEDS;
			tileTypeDefs2 [25] = Constants.Terrafirma_Color.EBONSTONE;
			tileTypeDefs2 [26] = Constants.Terrafirma_Color.DEMON_ALTAR;
			tileTypeDefs2 [27] = Constants.Terrafirma_Color.SUNFLOWER;
			tileTypeDefs2 [28] = Constants.Terrafirma_Color.POT;
			tileTypeDefs2 [29] = Constants.Terrafirma_Color.PIGGY_BANK;
			tileTypeDefs2 [30] = Constants.Terrafirma_Color.WOOD;
			tileTypeDefs2 [31] = Constants.Terrafirma_Color.SHADOW_ORB;
			tileTypeDefs2 [32] = Constants.Terrafirma_Color.CORRUPTED_VINES;
			tileTypeDefs2 [33] = Constants.Terrafirma_Color.CANDLE;
			tileTypeDefs2 [34] = Constants.Terrafirma_Color.COPPER_CHANDELIER;
			tileTypeDefs2 [35] = Constants.Terrafirma_Color.SILVER_CHANDELIER;
			tileTypeDefs2 [36] = Constants.Terrafirma_Color.GOLD_CHANDELIER;
			tileTypeDefs2 [37] = Constants.Terrafirma_Color.METEORITE;
			tileTypeDefs2 [38] = Constants.Terrafirma_Color.GRAY_BRICK;
			tileTypeDefs2 [39] = Constants.Terrafirma_Color.CLAY_BRICK;
			tileTypeDefs2 [40] = Constants.Terrafirma_Color.CLAY;
			tileTypeDefs2 [41] = Constants.Terrafirma_Color.BLUE_BRICK;
			tileTypeDefs2 [42] = Constants.Terrafirma_Color.LIGHT_GLOBE;
			tileTypeDefs2 [43] = Constants.Terrafirma_Color.GREEN_BRICK;
			tileTypeDefs2 [44] = Constants.Terrafirma_Color.PINK_BRICK;
			tileTypeDefs2 [45] = Constants.Terrafirma_Color.GOLD_BRICK;
			tileTypeDefs2 [46] = Constants.Terrafirma_Color.SILVER_BRICK;
			tileTypeDefs2 [47] = Constants.Terrafirma_Color.COPPER_BRICK;
			tileTypeDefs2 [48] = Constants.Terrafirma_Color.SPIKES;
			tileTypeDefs2 [49] = Constants.Terrafirma_Color.BLUE_CANDLE;
			tileTypeDefs2 [50] = Constants.Terrafirma_Color.BOOKS;
			tileTypeDefs2 [51] = Constants.Terrafirma_Color.COBWEBS;
			tileTypeDefs2 [52] = Constants.Terrafirma_Color.VINES;
			tileTypeDefs2 [53] = Constants.Terrafirma_Color.SAND;
			tileTypeDefs2 [54] = Constants.Terrafirma_Color.GLASS;
			tileTypeDefs2 [55] = Constants.Terrafirma_Color.SIGN;
			tileTypeDefs2 [56] = Constants.Terrafirma_Color.OBSIDIAN;
			tileTypeDefs2 [57] = Constants.Terrafirma_Color.ASH;
			tileTypeDefs2 [58] = Constants.Terrafirma_Color.HELLSTONE;
			tileTypeDefs2 [59] = Constants.Terrafirma_Color.MUD;
			tileTypeDefs2 [60] = Constants.Terrafirma_Color.JUNGLE_GRASS;
			tileTypeDefs2 [61] = Constants.Terrafirma_Color.JUNGLE_WEEDS;
			tileTypeDefs2 [62] = Constants.Terrafirma_Color.JUNGLE_VINES;
			tileTypeDefs2 [63] = Constants.Terrafirma_Color.SAPPHIRE;
			tileTypeDefs2 [64] = Constants.Terrafirma_Color.RUBY;
			tileTypeDefs2 [65] = Constants.Terrafirma_Color.EMERALD;
			tileTypeDefs2 [66] = Constants.Terrafirma_Color.TOPAZ;
			tileTypeDefs2 [67] = Constants.Terrafirma_Color.AMETHYST;
			tileTypeDefs2 [68] = Constants.Terrafirma_Color.DIAMOND;
			tileTypeDefs2 [69] = Constants.Terrafirma_Color.JUNGLE_THORN;
			tileTypeDefs2 [70] = Constants.Terrafirma_Color.MUSHROOM_GRASS;
			tileTypeDefs2 [71] = Constants.Terrafirma_Color.MUSHROOM;
			tileTypeDefs2 [72] = Constants.Terrafirma_Color.MUSHROOM_TREE;
			tileTypeDefs2 [73] = Constants.Terrafirma_Color.WEEDS_73;
			tileTypeDefs2 [74] = Constants.Terrafirma_Color.WEEDS_74;
			tileTypeDefs2 [75] = Constants.Terrafirma_Color.OBSIDIAN_BRICK;
			tileTypeDefs2 [76] = Constants.Terrafirma_Color.HELLSTONE_BRICK;
			tileTypeDefs2 [77] = Constants.Terrafirma_Color.HELLFORGE;
			tileTypeDefs2 [78] = Constants.Terrafirma_Color.CLAY_POT;
			tileTypeDefs2 [79] = Constants.Terrafirma_Color.BED;
			tileTypeDefs2 [80] = Constants.Terrafirma_Color.CACTUS;
			tileTypeDefs2 [81] = Constants.Terrafirma_Color.CORAL;
			tileTypeDefs2 [82] = Constants.Terrafirma_Color.HERB_SPROUTS;
			tileTypeDefs2 [83] = Constants.Terrafirma_Color.HERB_STALKS;
			tileTypeDefs2 [84] = Constants.Terrafirma_Color.HERBS;
			tileTypeDefs2 [85] = Constants.Terrafirma_Color.TOMBSTONE;
			tileTypeDefs2 [86] = Constants.Terrafirma_Color.LOOM;
			tileTypeDefs2 [87] = Constants.Terrafirma_Color.PIANO;
			tileTypeDefs2 [88] = Constants.Terrafirma_Color.DRESSER;
			tileTypeDefs2 [89] = Constants.Terrafirma_Color.BENCH;
			tileTypeDefs2 [90] = Constants.Terrafirma_Color.BATHTUB;
			tileTypeDefs2 [91] = Constants.Terrafirma_Color.BANNER;
			tileTypeDefs2 [92] = Constants.Terrafirma_Color.LAMP_POST;
			tileTypeDefs2 [93] = Constants.Terrafirma_Color.TIKI_TORCH;
			tileTypeDefs2 [94] = Constants.Terrafirma_Color.KEG;
			tileTypeDefs2 [95] = Constants.Terrafirma_Color.CHINESE_LANTERN;
			tileTypeDefs2 [96] = Constants.Terrafirma_Color.COOKING_POT;
			tileTypeDefs2 [97] = Constants.Terrafirma_Color.SAFE;
			tileTypeDefs2 [98] = Constants.Terrafirma_Color.SKULL_LANTERN;
			tileTypeDefs2 [99] = Constants.Terrafirma_Color.TRASH_CAN;
			tileTypeDefs2 [100] = Constants.Terrafirma_Color.CANDELABRA;
			tileTypeDefs2 [101] = Constants.Terrafirma_Color.BOOKCASE;
			tileTypeDefs2 [102] = Constants.Terrafirma_Color.THRONE;
			tileTypeDefs2 [103] = Constants.Terrafirma_Color.BOWL;
			tileTypeDefs2 [104] = Constants.Terrafirma_Color.GRANDFATHER_CLOCK;
			tileTypeDefs2 [105] = Constants.Terrafirma_Color.STATUE;
			
			for (int i = 106; i < 265; i++) {
				tileTypeDefs2 [i] = Color.Magenta;
			}
			
			//global
			tileTypeDefs2 [265] = Constants.Terrafirma_Color.SKY;
			//tileTypeDefs2 [] = Constants.Terrafirma_Color.EARTH;
			//tileTypeDefs2 [] = Constants.Terrafirma_Color.ROCK;
			//tileTypeDefs2 [] = Constants.Terrafirma_Color.HELL;
			tileTypeDefs2 [267] = Constants.Terrafirma_Color.LAVA;
			tileTypeDefs2 [266] = Constants.Terrafirma_Color.WATER;
			
			//walls
			tileTypeDefs2 [268] = Constants.Terrafirma_Color.STONE_WALL;
			tileTypeDefs2 [269] = Constants.Terrafirma_Color.DIRT_WALL;
			tileTypeDefs2 [270] = Constants.Terrafirma_Color.STONE_WALL2;
			tileTypeDefs2 [271] = Constants.Terrafirma_Color.WOOD_WALL;
			tileTypeDefs2 [272] = Constants.Terrafirma_Color.BRICK_WALL;
			tileTypeDefs2 [273] = Constants.Terrafirma_Color.RED_BRICK_WALL;
			tileTypeDefs2 [274] = Constants.Terrafirma_Color.BLUE_BRICK_WALL;
			tileTypeDefs2 [275] = Constants.Terrafirma_Color.GREEN_BRICK_WALL;
			tileTypeDefs2 [276] = Constants.Terrafirma_Color.PINK_BRICK_WALL;
			tileTypeDefs2 [277] = Constants.Terrafirma_Color.GOLD_BRICK_WALL;
			tileTypeDefs2 [278] = Constants.Terrafirma_Color.SILVER_BRICK_WALL;
			tileTypeDefs2 [279] = Constants.Terrafirma_Color.COPPER_BRICK_WALL;
			tileTypeDefs2 [280] = Constants.Terrafirma_Color.HELLSTONE_BRICK_WALL;
			tileTypeDefs2 [281] = Constants.Terrafirma_Color.OBSIDIAN_WALL;
			tileTypeDefs2 [282] = Constants.Terrafirma_Color.MUD_WALL;
			tileTypeDefs2 [283] = Constants.Terrafirma_Color.DIRT_WALL2;
			tileTypeDefs2 [284] = Constants.Terrafirma_Color.DARK_BLUE_BRICK_WALL;
			tileTypeDefs2 [285] = Constants.Terrafirma_Color.DARK_GREEN_BRICK_WALL;
			tileTypeDefs2 [286] = Constants.Terrafirma_Color.DARK_PINK_BRICK_WALL;
			tileTypeDefs2 [287] = Constants.Terrafirma_Color.DARK_OBSIDIAN_WALL;
			
			//fix
			tileTypeDefs2 [288] = Constants.Terrafirma_Color.DARK_OBSIDIAN_WALL;
			tileTypeDefs2 [330] = Constants.Terrafirma_Color.DARK_OBSIDIAN_WALL;
			
		}
	}
}
