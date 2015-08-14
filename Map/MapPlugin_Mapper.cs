using System.Drawing;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using TDSM.API.Logging;
using TDSM.API;
using Terraria;
using System;

namespace Map
{
    public partial class MapPlugin
    {
        public static Dictionary<int, System.Drawing.Color> tileTypeDefs;

        public void mapWorld()
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
                if (x1 < 0)
                    x1 = 0;
                if (y1 < 0)
                    y1 = 0;
                if (x2 > Main.maxTilesX)
                    x2 = Main.maxTilesX;
                if (y2 > Main.maxTilesY)
                    y2 = Main.maxTilesY;
            }
            Stopwatch stopwatch = new Stopwatch();
            Tools.NotifyAllOps("Saving Image...");
            stopwatch.Start();

            try
            {
                bmp = new Bitmap((x2 - x1), (y2 - y1), PixelFormat.Format32bppArgb);
            }
            catch (ArgumentException e)
            {
                ProgramLog.BareLog(ProgramLog.Error, "<map> ERROR: could not create initial Bitmap object.");
                ProgramLog.BareLog(ProgramLog.Plugin, e.StackTrace.ToString());
                stopwatch.Stop();
                isMapping = false;
                return;
            }

            Graphics graphicsHandle = Graphics.FromImage((Image)bmp);
            graphicsHandle.FillRectangle(new SolidBrush(Constants.MoreTerra_Color.SKY), 0, 0, bmp.Width, (float)(Main.worldSurface));
            graphicsHandle.FillRectangle(new SolidBrush(Constants.MoreTerra_Color.WALL_BACKGROUND), 0, (float)(Main.worldSurface), bmp.Width, (float)(Main.rockLayer));
            graphicsHandle.FillRectangle(new SolidBrush(Constants.MoreTerra_Color.WALL_HELL), 0, (float)(Main.rockLayer), bmp.Width, bmp.Height);

            using (var prog = new ProgressLogger(x2 - 1, "Saving image data"))
                for (int i = x1; i < x2; i++)
                {
                    prog.Value = i;
                    for (int j = y1; j < y2; j++)
                    {

                        //TODO: find a more understandable way on these if statements
                        if (Main.tile[i, j].wall == 0)
                        {
                            if (Main.tile[i, j].active())
                            {
                                bmp.SetPixel(i - x1, j - y1, tileTypeDefs[Main.tile[i, j].type]);
                            }
                            else
                            {

                                if (Main.tile[i, j].liquid > 0)
                                {
                                    if (Main.tile[i, j].lava())
                                    {
                                        bmp.SetPixel(i - x1, j - y1, Constants.MoreTerra_Color.LAVA);
                                    }
                                    else
                                    {
                                        bmp.SetPixel(i - x1, j - y1, Constants.MoreTerra_Color.WATER);
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (Main.tile[i, j].active())
                            {
                                bmp.SetPixel(i - x1, j - y1, tileTypeDefs[Main.tile[i, j].type]);
                            }
                            else
                            {
                                bmp.SetPixel(i - x1, j - y1, tileTypeDefs[Main.tile[i, j].wall + 450]);
                            }
                        }

                    }
                }
            Tools.NotifyAllOps("Saving Data...");
            bmp.Save(string.Concat(p, Path.DirectorySeparatorChar, filename));
            stopwatch.Stop();
            Tools.NotifyAllOps("Save duration: " + stopwatch.Elapsed.Seconds + " Second(s)");
            Tools.NotifyAllOps("Saving Complete.");
            bmp = null;
            isMapping = false;
        }

        public partial class Constants //credits go to the authors of MoreTerra
        {
            public static class MoreTerra_Color
            {
                public static System.Drawing.Color DIRT = ColorTranslator.FromHtml("#976B4B"); // #AF8365
                public static System.Drawing.Color STONE = ColorTranslator.FromHtml("#808080"); // #808080
                public static System.Drawing.Color GRASS = ColorTranslator.FromHtml("#1CD85E"); // #1CD85E
                public static System.Drawing.Color PLANTS = ColorTranslator.FromHtml("#1AC454"); // Plants
                public static System.Drawing.Color TORCH = ColorTranslator.FromHtml("#A97D5D"); // Light Source
                public static System.Drawing.Color TREES = ColorTranslator.FromHtml("#976B4B"); // Wood
                public static System.Drawing.Color IRON = ColorTranslator.FromHtml("#8C6550"); // #BD9F8B
                public static System.Drawing.Color COPPER = ColorTranslator.FromHtml("#964316"); // #FF9532
                public static System.Drawing.Color GOLD = ColorTranslator.FromHtml("#B9A417"); // #B9A417
                public static System.Drawing.Color SILVER = ColorTranslator.FromHtml("#B9C2C3"); // #D9DFDF
                public static System.Drawing.Color CLOSED_DOOR = ColorTranslator.FromHtml("#77694F"); // Decorative
                public static System.Drawing.Color OPEN_DOOR = ColorTranslator.FromHtml("#77694F"); // Decorative
                public static System.Drawing.Color HEART = ColorTranslator.FromHtml("#AE1845"); // Important
                public static System.Drawing.Color BOTTLES = ColorTranslator.FromHtml("#85D5F7"); // Decorative
                public static System.Drawing.Color TABLE = ColorTranslator.FromHtml("#BF8E6F"); // Decorative
                public static System.Drawing.Color CHAIRS = ColorTranslator.FromHtml("#BF8E6F"); // Decorative
                public static System.Drawing.Color ANVIL = ColorTranslator.FromHtml("#8C8274"); // Decorative
                public static System.Drawing.Color FURNACE = ColorTranslator.FromHtml("#909490"); // Decorative
                public static System.Drawing.Color WORK_BENCH = ColorTranslator.FromHtml("#BF8E6F"); // Decorative
                public static System.Drawing.Color WOOD_PLATFORM = ColorTranslator.FromHtml("#BF8E6F"); // Wood
                public static System.Drawing.Color SAPLINGS = ColorTranslator.FromHtml("#A37451"); // Plants
                public static System.Drawing.Color CONTAINERS = ColorTranslator.FromHtml("#AE815C"); // Important
                public static System.Drawing.Color DEMONITE = ColorTranslator.FromHtml("#625FA7"); // #625FA7
                public static System.Drawing.Color CORRUPT_GRASS = ColorTranslator.FromHtml("#8D89DF"); // Corruption Grass
                public static System.Drawing.Color CORRUPT_PLANTS = ColorTranslator.FromHtml("#7A74DA"); // Corruption Grass
                public static System.Drawing.Color EBONSTONE = ColorTranslator.FromHtml("#6D5A80"); // #4B4A82
                public static System.Drawing.Color DEMON_ALTAR = ColorTranslator.FromHtml("#77657D"); // Important
                public static System.Drawing.Color SUNFLOWER = ColorTranslator.FromHtml("#369A36"); // Plants
                public static System.Drawing.Color POTS = ColorTranslator.FromHtml("#974F50"); // Important
                public static System.Drawing.Color PIGGY_BANK = ColorTranslator.FromHtml("#AF6980"); // Decorative
                public static System.Drawing.Color WOOD_BLOCK = ColorTranslator.FromHtml("#976B4B"); // #A87957
                public static System.Drawing.Color SHADOW_ORB = ColorTranslator.FromHtml("#8D78A8"); // Important
                public static System.Drawing.Color CORRUPT_THORNS = ColorTranslator.FromHtml("#9787B7"); // #7A618F
                public static System.Drawing.Color CANDLE = ColorTranslator.FromHtml("#FDDD03"); // Light Source
                public static System.Drawing.Color CHANDELIERS = ColorTranslator.FromHtml("#EBA687"); // Light Source
                public static System.Drawing.Color JACK_O_LANTERNS = ColorTranslator.FromHtml("#E2911E"); // Light Source
                public static System.Drawing.Color PRESENTS = ColorTranslator.FromHtml("#E6595C"); // Decorative
                public static System.Drawing.Color METEORITE = ColorTranslator.FromHtml("#685654"); // #FF00FF
                public static System.Drawing.Color GRAY_BRICK = ColorTranslator.FromHtml("#808080"); // Block
                public static System.Drawing.Color RED_BRICK = ColorTranslator.FromHtml("#B53E3B"); // Block
                public static System.Drawing.Color CLAY_BLOCK = ColorTranslator.FromHtml("#925144"); // #D87365
                public static System.Drawing.Color BLUE_BRICK = ColorTranslator.FromHtml("#42546D"); // #4245C2
                public static System.Drawing.Color HANGING_GLOBE = ColorTranslator.FromHtml("#FBEB7F"); // Light Source
                public static System.Drawing.Color GREEN_BRICK = ColorTranslator.FromHtml("#54643F"); // #1A8822
                public static System.Drawing.Color PINK_BRICK = ColorTranslator.FromHtml("#6B4463"); // #A93175
                public static System.Drawing.Color GOLD_BRICK = ColorTranslator.FromHtml("#B9A417"); // Block
                public static System.Drawing.Color SILVER_BRICK = ColorTranslator.FromHtml("#B9C2C3"); // Block
                public static System.Drawing.Color COPPER_BRICK = ColorTranslator.FromHtml("#964316"); // Block
                public static System.Drawing.Color SPIKE = ColorTranslator.FromHtml("#808080"); // #6D6D6D
                public static System.Drawing.Color WATER_CANDLE = ColorTranslator.FromHtml("#59C9FF"); // Light Source
                public static System.Drawing.Color BOOKS = ColorTranslator.FromHtml("#AA3072"); // Decorative
                public static System.Drawing.Color COBWEB = ColorTranslator.FromHtml("#C0CACB"); // #F0F0F0
                public static System.Drawing.Color VINES = ColorTranslator.FromHtml("#17B14C"); // Plants
                public static System.Drawing.Color SAND = ColorTranslator.FromHtml("#BAA854"); // #FFDA38
                public static System.Drawing.Color GLASS = ColorTranslator.FromHtml("#C8F6FE"); // Decorative
                public static System.Drawing.Color SIGNS = ColorTranslator.FromHtml("#BF8E6F"); // Decorative
                public static System.Drawing.Color OBSIDIAN = ColorTranslator.FromHtml("#2B2854"); // #5751AD
                public static System.Drawing.Color ASH = ColorTranslator.FromHtml("#44444C"); // #44444C
                public static System.Drawing.Color HELLSTONE = ColorTranslator.FromHtml("#8E4242"); // #662222
                public static System.Drawing.Color MUD = ColorTranslator.FromHtml("#5C4449"); // #5C4449
                public static System.Drawing.Color JUNGLE_GRASS = ColorTranslator.FromHtml("#8FD71D"); // Jungle Plants
                public static System.Drawing.Color JUNGLE_PLANTS = ColorTranslator.FromHtml("#87C41A"); // Jungle Plants
                public static System.Drawing.Color JUNGLE_VINES = ColorTranslator.FromHtml("#79B018"); // #8ACE1C
                public static System.Drawing.Color SAPPHIRE = ColorTranslator.FromHtml("#6E8CB6"); // Gems
                public static System.Drawing.Color RUBY = ColorTranslator.FromHtml("#C46072"); // Gems
                public static System.Drawing.Color EMERALD = ColorTranslator.FromHtml("#389661"); // Gems
                public static System.Drawing.Color TOPAZ = ColorTranslator.FromHtml("#A0763A"); // Gems
                public static System.Drawing.Color AMETHYST = ColorTranslator.FromHtml("#8C3AA6"); // Gems
                public static System.Drawing.Color DIAMOND = ColorTranslator.FromHtml("#7DBFC5"); // Gems
                public static System.Drawing.Color JUNGLE_THORNS = ColorTranslator.FromHtml("#BE965C"); // #5E3037
                public static System.Drawing.Color MUSHROOM_GRASS = ColorTranslator.FromHtml("#5D7FFF"); // #5D7FFF
                public static System.Drawing.Color MUSHROOM_PLANTS = ColorTranslator.FromHtml("#B6AF82"); // #B1AE83
                public static System.Drawing.Color MUSHROOM_TREES = ColorTranslator.FromHtml("#B6AF82"); // #968F6E
                public static System.Drawing.Color PLANTS2 = ColorTranslator.FromHtml("#1BC56D"); // Plants
                public static System.Drawing.Color PLANTS3 = ColorTranslator.FromHtml("#60C51B"); // Plants
                public static System.Drawing.Color OBSIDIAN_BRICK = ColorTranslator.FromHtml("#1A1A1A"); // Block
                public static System.Drawing.Color HELLSTONE_BRICK = ColorTranslator.FromHtml("#8E4242"); // Block
                public static System.Drawing.Color HELLFORGE = ColorTranslator.FromHtml("#EE5546"); // Important
                public static System.Drawing.Color CLAY_POT = ColorTranslator.FromHtml("#796E61"); // Decorative
                public static System.Drawing.Color BED = ColorTranslator.FromHtml("#BF8E6F"); // Decorative
                public static System.Drawing.Color CACTUS = ColorTranslator.FromHtml("#497811"); // #006400
                public static System.Drawing.Color CORAL = ColorTranslator.FromHtml("#F585BF"); // #FFB6C1
                public static System.Drawing.Color IMMATURE_HERBS = ColorTranslator.FromHtml("#F6C51A"); // Herb
                public static System.Drawing.Color MATURE_HERBS = ColorTranslator.FromHtml("#F6C51A"); // Herb
                public static System.Drawing.Color BLOOMING_HERBS = ColorTranslator.FromHtml("#F6C51A"); // Herb
                public static System.Drawing.Color TOMBSTONE = ColorTranslator.FromHtml("#C0C0C0"); // #696969
                public static System.Drawing.Color LOOM = ColorTranslator.FromHtml("#BF8E6F"); // Decorative
                public static System.Drawing.Color PIANO = ColorTranslator.FromHtml("#BF8E6F"); // Decorative
                public static System.Drawing.Color DRESSER = ColorTranslator.FromHtml("#BF8E6F"); // Decorative
                public static System.Drawing.Color BENCH = ColorTranslator.FromHtml("#BF8E6F"); // Decorative
                public static System.Drawing.Color BATHTUB = ColorTranslator.FromHtml("#909490"); // Decorative
                public static System.Drawing.Color BANNERS = ColorTranslator.FromHtml("#0D5882"); // Decorative
                public static System.Drawing.Color LAMPPOST = ColorTranslator.FromHtml("#D5E5ED"); // Light Source
                public static System.Drawing.Color TIKI_TORCH = ColorTranslator.FromHtml("#FDDD03"); // Light Source
                public static System.Drawing.Color KEG = ColorTranslator.FromHtml("#BF8E6F"); // Decorative
                public static System.Drawing.Color CHINESE_LANTERN = ColorTranslator.FromHtml("#FFA21F"); // Light Source
                public static System.Drawing.Color COOKING_POT = ColorTranslator.FromHtml("#909490"); // Decorative
                public static System.Drawing.Color SAFE = ColorTranslator.FromHtml("#909490"); // Decorative
                public static System.Drawing.Color SKULL_CANDLE = ColorTranslator.FromHtml("#FDDD03"); // Light Source
                public static System.Drawing.Color TRASH_CAN = ColorTranslator.FromHtml("#909490"); // Decorative
                public static System.Drawing.Color CANDELABRA = ColorTranslator.FromHtml("#FDDD03"); // Light Source
                public static System.Drawing.Color BOOKCASE = ColorTranslator.FromHtml("#BF8E6F"); // Decorative
                public static System.Drawing.Color THRONE = ColorTranslator.FromHtml("#E5D449"); // Decorative
                public static System.Drawing.Color BOWL = ColorTranslator.FromHtml("#8D624D"); // Decorative
                public static System.Drawing.Color GRANDFATHER_CLOCK = ColorTranslator.FromHtml("#BF8E6F"); // Decorative
                public static System.Drawing.Color STATUE = ColorTranslator.FromHtml("#909490"); // Decorative
                public static System.Drawing.Color SAWMILL = ColorTranslator.FromHtml("#BF8E6F"); // Decorative
                public static System.Drawing.Color COBALT = ColorTranslator.FromHtml("#0B508F"); // #0B508F
                public static System.Drawing.Color MYTHRIL = ColorTranslator.FromHtml("#5BA9A9"); // #5BA9A9
                public static System.Drawing.Color HALLOWED_GRASS = ColorTranslator.FromHtml("#4EC1E3"); // Hallowed Plants
                public static System.Drawing.Color HALLOWED_PLANTS = ColorTranslator.FromHtml("#30BA87"); // Hallowed Plants
                public static System.Drawing.Color ADAMANTITE = ColorTranslator.FromHtml("#801A34"); // #801A34
                public static System.Drawing.Color EBONSAND = ColorTranslator.FromHtml("#67627A"); // #595353
                public static System.Drawing.Color HALLOWED_PLANTS_2 = ColorTranslator.FromHtml("#30D0EA"); // Hallowed Plants
                public static System.Drawing.Color TINKERERS_WORKBENCH = ColorTranslator.FromHtml("#BF8E6F"); // Decorative
                public static System.Drawing.Color HALLOWED_VINES = ColorTranslator.FromHtml("#21ABCF"); // Hallowed Vines
                public static System.Drawing.Color PEARLSAND = ColorTranslator.FromHtml("#EEE1DA"); // #EEE1DA
                public static System.Drawing.Color PEARLSTONE = ColorTranslator.FromHtml("#B5ACBE"); // #B5ACBE
                public static System.Drawing.Color PEARLSTONE_BRICK = ColorTranslator.FromHtml("#EEE1DA"); // Block
                public static System.Drawing.Color IRIDESCENT_BRICK = ColorTranslator.FromHtml("#6B5C6C"); // Block
                public static System.Drawing.Color UNKNOWN_NEW_BRICK = ColorTranslator.FromHtml("#5C4449"); // Unknown
                public static System.Drawing.Color COBALT_BRICK = ColorTranslator.FromHtml("#0B508F"); // Block
                public static System.Drawing.Color MYTHRIL_BRICK = ColorTranslator.FromHtml("#5BA9A9"); // Block
                public static System.Drawing.Color SILT = ColorTranslator.FromHtml("#6A6B76"); // #67627A
                public static System.Drawing.Color WOODEN_PLANK = ColorTranslator.FromHtml("#493324"); // Wood
                public static System.Drawing.Color CRYSTAL_BALL = ColorTranslator.FromHtml("#8DAFFF"); // Decorative
                public static System.Drawing.Color DISCO_BALL = ColorTranslator.FromHtml("#9FD1E5"); // Decorative
                public static System.Drawing.Color ICE_BLOCK = ColorTranslator.FromHtml("#FF00FF"); // Block
                public static System.Drawing.Color MANNEQUIN = ColorTranslator.FromHtml("#BF8E6F"); // Decorative
                public static System.Drawing.Color CRYSTALS = ColorTranslator.FromHtml("#FF75E0"); // #785AD9
                public static System.Drawing.Color ACTIVE_STONE_BLOCK = ColorTranslator.FromHtml("#808080"); // Mechanical
                public static System.Drawing.Color INACTIVE_STONE_BLOCK = ColorTranslator.FromHtml("#343434"); // Mechanical
                public static System.Drawing.Color LEVER = ColorTranslator.FromHtml("#909490"); // Mechanical
                public static System.Drawing.Color ADAMANTITE_FORGE = ColorTranslator.FromHtml("#E73538"); // Decorative
                public static System.Drawing.Color MYTHRIL_ANVIL = ColorTranslator.FromHtml("#A6BB99"); // Decorative
                public static System.Drawing.Color PRESSURE_PLATES = ColorTranslator.FromHtml("#FF00FF"); // Mechanical
                public static System.Drawing.Color SWITCHES = ColorTranslator.FromHtml("#D5CBCC"); // Mechanical
                public static System.Drawing.Color DART_TRAP = ColorTranslator.FromHtml("#909490"); // Mechanical
                public static System.Drawing.Color BOULDER = ColorTranslator.FromHtml("#808080"); // Decorative
                public static System.Drawing.Color MUSIC_BOXES = ColorTranslator.FromHtml("#BF8E6F"); // Decorative
                public static System.Drawing.Color EBONSTONE_BRICK = ColorTranslator.FromHtml("#625FA7"); // Block
                public static System.Drawing.Color EXPLOSIVES = ColorTranslator.FromHtml("#C03B3B"); // Mechanical
                public static System.Drawing.Color INLET_PUMP = ColorTranslator.FromHtml("#909490"); // Mechanical
                public static System.Drawing.Color OUTLET_PUMP = ColorTranslator.FromHtml("#909490"); // Mechanical
                public static System.Drawing.Color TIMERS = ColorTranslator.FromHtml("#909490"); // Mechanical
                public static System.Drawing.Color CANDY_CANE_BLOCK = ColorTranslator.FromHtml("#C01E1E"); // Block
                public static System.Drawing.Color GREEN_CANDY_CANE_BLOCK = ColorTranslator.FromHtml("#2BC01E"); // Block
                public static System.Drawing.Color SNOW_BLOCK = ColorTranslator.FromHtml("#D3ECF1"); // #FFFFFF
                public static System.Drawing.Color SNOW_BRICK = ColorTranslator.FromHtml("#D3ECF1"); // Block
                public static System.Drawing.Color HOLIDAY_LIGHTS = ColorTranslator.FromHtml("#DC3232"); // Light Source
                public static System.Drawing.Color ADAMANTITE_BEAM = ColorTranslator.FromHtml("#801A34"); // Unknown
                public static System.Drawing.Color SANDSTONE_BRICK = ColorTranslator.FromHtml("#BEAB5E"); // Unknown
                public static System.Drawing.Color EBONSTONE_BRICK_2 = ColorTranslator.FromHtml("#8085B8"); // Unknown
                public static System.Drawing.Color RED_STUCCO = ColorTranslator.FromHtml("#EF8D7E"); // Unknown
                public static System.Drawing.Color YELLOW_STUCCO = ColorTranslator.FromHtml("#BEAB5E"); // Unknown
                public static System.Drawing.Color GREEN_STUCCO = ColorTranslator.FromHtml("#83A2A1"); // Unknown
                public static System.Drawing.Color GRAY_STUCCO = ColorTranslator.FromHtml("#AAAB9D"); // Unknown
                public static System.Drawing.Color EBONWOOD = ColorTranslator.FromHtml("#68647E"); // Unknown
                public static System.Drawing.Color RICH_MAHOGANY = ColorTranslator.FromHtml("#915155"); // Unknown
                public static System.Drawing.Color PEARLWOOD = ColorTranslator.FromHtml("#948562"); // Unknown
                public static System.Drawing.Color RAINBOW_BRICK = ColorTranslator.FromHtml("#C80000"); // Unknown
                public static System.Drawing.Color ICE = ColorTranslator.FromHtml("#90C3E8"); // Unknown
                public static System.Drawing.Color BREAKABLE_ICE = ColorTranslator.FromHtml("#B8DBF0"); // Unknown
                public static System.Drawing.Color PURPLE_ICE = ColorTranslator.FromHtml("#AE91D6"); // Unknown
                public static System.Drawing.Color PINK_ICE = ColorTranslator.FromHtml("#DAB6CC"); // Unknown
                public static System.Drawing.Color STALAGTITE = ColorTranslator.FromHtml("#73ADE5"); // Unknown
                public static System.Drawing.Color TIN_ORE = ColorTranslator.FromHtml("#817D5D"); // #E3DBA2
                public static System.Drawing.Color LEAD_ORE = ColorTranslator.FromHtml("#3E5272"); // #55727B
                public static System.Drawing.Color TUNGSTEN_ORE = ColorTranslator.FromHtml("#849D7F"); // #5A7D53
                public static System.Drawing.Color PLATINUM_ORE = ColorTranslator.FromHtml("#98ABC6"); // #8097B8
                public static System.Drawing.Color PINE_TREE = ColorTranslator.FromHtml("#1B6D45"); // Unknown
                public static System.Drawing.Color CHRISTMAS_TREE = ColorTranslator.FromHtml("#218755"); // Unknown
                public static System.Drawing.Color UNUSED_PLATINUM_CHANDELIER = ColorTranslator.FromHtml("#B5C2D9"); // Unknown
                public static System.Drawing.Color PLATINUM_CANDELABRA = ColorTranslator.FromHtml("#FDDD03"); // Unknown
                public static System.Drawing.Color PLATINUM_CANDLE = ColorTranslator.FromHtml("#FDDD03"); // Unknown
                public static System.Drawing.Color TIN_BRICK = ColorTranslator.FromHtml("#817D5D"); // Unknown
                public static System.Drawing.Color TUNGSTEN_BRICK = ColorTranslator.FromHtml("#849D7F"); // Unknown
                public static System.Drawing.Color PLATINUM_BRICK = ColorTranslator.FromHtml("#98ABC6"); // Unknown
                public static System.Drawing.Color EXPOSED_GEMS = ColorTranslator.FromHtml("#FFD978"); // Unknown
                public static System.Drawing.Color GREEN_MOSS = ColorTranslator.FromHtml("#318672"); // Unknown
                public static System.Drawing.Color BROWN_MOSS = ColorTranslator.FromHtml("#7E8631"); // Unknown
                public static System.Drawing.Color RED_MOSS = ColorTranslator.FromHtml("#863B31"); // Unknown
                public static System.Drawing.Color BLUE_MOSS = ColorTranslator.FromHtml("#2B568C"); // Unknown
                public static System.Drawing.Color PURPLE_MOSS = ColorTranslator.FromHtml("#793186"); // Unknown
                public static System.Drawing.Color LONG_MOSS = ColorTranslator.FromHtml("#1D6A58"); // Unknown
                public static System.Drawing.Color SMALL_DETRITUS = ColorTranslator.FromHtml("#636363"); // Unknown
                public static System.Drawing.Color LARGE_DETRITUS = ColorTranslator.FromHtml("#636363"); // Unknown
                public static System.Drawing.Color LARGE_DETRITUS2 = ColorTranslator.FromHtml("#636363"); // Unknown
                public static System.Drawing.Color CACTUS_BLOCK = ColorTranslator.FromHtml("#497811"); // Unknown
                public static System.Drawing.Color CLOUD = ColorTranslator.FromHtml("#DFFFFF"); // Unknown
                public static System.Drawing.Color GLOWING_MUSHROOM = ColorTranslator.FromHtml("#B6AF82"); // Unknown
                public static System.Drawing.Color LIVING_WOOD = ColorTranslator.FromHtml("#976B4B"); // Unknown
                public static System.Drawing.Color LEAF_BLOCK = ColorTranslator.FromHtml("#1AC454"); // Unknown
                public static System.Drawing.Color SLIME_BLOCK = ColorTranslator.FromHtml("#3879FF"); // Unknown
                public static System.Drawing.Color BONE_BLOCK = ColorTranslator.FromHtml("#9D9D6B"); // Unknown
                public static System.Drawing.Color FLESH_BLOCK = ColorTranslator.FromHtml("#861622"); // Unknown
                public static System.Drawing.Color RAIN_CLOUD = ColorTranslator.FromHtml("#9390B2"); // Unknown
                public static System.Drawing.Color FROZEN_SLIME = ColorTranslator.FromHtml("#61C8E1"); // Unknown
                public static System.Drawing.Color ASPHALT = ColorTranslator.FromHtml("#3E3D34"); // Unknown
                public static System.Drawing.Color FLESH_GRASS = ColorTranslator.FromHtml("#D05050"); // Unknown
                public static System.Drawing.Color RED_ICE = ColorTranslator.FromHtml("#D89890"); // Unknown
                public static System.Drawing.Color FLESH_WEEDS = ColorTranslator.FromHtml("#CB3D40"); // Unknown
                public static System.Drawing.Color SUNPLATE = ColorTranslator.FromHtml("#D5B21C"); // Unknown
                public static System.Drawing.Color CRIMSTONE = ColorTranslator.FromHtml("#802C2D"); // Unknown
                public static System.Drawing.Color CRIMTANE_ORE = ColorTranslator.FromHtml("#7D3741"); // Unknown
                public static System.Drawing.Color CRIMSTONE_VINES = ColorTranslator.FromHtml("#BA3234"); // Unknown
                public static System.Drawing.Color ICE_BRICK = ColorTranslator.FromHtml("#7CAFC9"); // Unknown
                public static System.Drawing.Color WATER_FOUNTAIN = ColorTranslator.FromHtml("#909490"); // Unknown
                public static System.Drawing.Color SHADEWOOD = ColorTranslator.FromHtml("#586976"); // Unknown
                public static System.Drawing.Color CANNON = ColorTranslator.FromHtml("#909490"); // Unknown
                public static System.Drawing.Color LAND_MINE = ColorTranslator.FromHtml("#FF00FF"); // Unknown
                public static System.Drawing.Color CHLOROPHYTE_ORE = ColorTranslator.FromHtml("#BFE973"); // Unknown
                public static System.Drawing.Color SNOWBALL_LAUNCHER = ColorTranslator.FromHtml("#909490"); // Unknown
                public static System.Drawing.Color ROPE = ColorTranslator.FromHtml("#897843"); // Unknown
                public static System.Drawing.Color CHAIN = ColorTranslator.FromHtml("#676767"); // Unknown
                public static System.Drawing.Color CAMPFIRE = ColorTranslator.FromHtml("#FE7902"); // Unknown
                public static System.Drawing.Color ROCKET = ColorTranslator.FromHtml("#BF8E6F"); // Unknown
                public static System.Drawing.Color BLEND_O_MATIC = ColorTranslator.FromHtml("#909490"); // Unknown
                public static System.Drawing.Color MEAT_GRINDER = ColorTranslator.FromHtml("#909490"); // Unknown
                public static System.Drawing.Color SILT_EXTRACTINATOR = ColorTranslator.FromHtml("#909490"); // Unknown
                public static System.Drawing.Color SOLIDIFIER = ColorTranslator.FromHtml("#909490"); // Unknown
                public static System.Drawing.Color PALLADIUM_ORE = ColorTranslator.FromHtml("#EF5A32"); // Unknown
                public static System.Drawing.Color ORICHALCUM_ORE = ColorTranslator.FromHtml("#E760E4"); // Unknown
                public static System.Drawing.Color TITANIUM_ORE = ColorTranslator.FromHtml("#395565"); // Unknown
                public static System.Drawing.Color SLUSH = ColorTranslator.FromHtml("#6B848B"); // Unknown
                public static System.Drawing.Color HIVE = ColorTranslator.FromHtml("#E37D16"); // Unknown
                public static System.Drawing.Color LIHZAHRD_BRICK = ColorTranslator.FromHtml("#8D3800"); // Unknown
                public static System.Drawing.Color DYE_PLANT = ColorTranslator.FromHtml("#4AC59B"); // Unknown
                public static System.Drawing.Color DYE_VAT = ColorTranslator.FromHtml("#909490"); // Unknown
                public static System.Drawing.Color HONEY_BLOCK = ColorTranslator.FromHtml("#FF9C0C"); // Unknown
                public static System.Drawing.Color CRIPSY_HONEY_BLOCK = ColorTranslator.FromHtml("#834F0D"); // Unknown
                public static System.Drawing.Color LARVA = ColorTranslator.FromHtml("#E0C265"); // Unknown
                public static System.Drawing.Color WOODEN_SPIKE = ColorTranslator.FromHtml("#915155"); // Unknown
                public static System.Drawing.Color PLANTS4 = ColorTranslator.FromHtml("#6BB61D"); // Unknown
                public static System.Drawing.Color CRIMSAND = ColorTranslator.FromHtml("#352C29"); // Unknown
                public static System.Drawing.Color TELEPORTER = ColorTranslator.FromHtml("#D6B82E"); // Unknown
                public static System.Drawing.Color LIFE_FRUIT = ColorTranslator.FromHtml("#95E857"); // Unknown
                public static System.Drawing.Color LIHZAHRD_ALTAR = ColorTranslator.FromHtml("#FFF133"); // Unknown
                public static System.Drawing.Color PLANTERAS_BULB = ColorTranslator.FromHtml("#E180CE"); // Unknown
                public static System.Drawing.Color METAL_BAR = ColorTranslator.FromHtml("#E0C265"); // Unknown
                public static System.Drawing.Color PICTURE_3X3 = ColorTranslator.FromHtml("#78553C"); // Unknown
                public static System.Drawing.Color PICTURE_CATACOMB = ColorTranslator.FromHtml("#4D4A48"); // Unknown
                public static System.Drawing.Color PICTURE_6X4 = ColorTranslator.FromHtml("#63321E"); // Unknown
                public static System.Drawing.Color IMBUING_STATION = ColorTranslator.FromHtml("#FF00FF"); // Unknown
                public static System.Drawing.Color BUBBLE_MACHINE = ColorTranslator.FromHtml("#C8F5FD"); // Unknown
                public static System.Drawing.Color PICTURE_2X3 = ColorTranslator.FromHtml("#63321E"); // Unknown
                public static System.Drawing.Color PICTURE_3X2 = ColorTranslator.FromHtml("#63321E"); // Unknown
                public static System.Drawing.Color AUTOHAMMER = ColorTranslator.FromHtml("#8C9696"); // Unknown
                public static System.Drawing.Color PALLADIUM_COLUMN = ColorTranslator.FromHtml("#DB4726"); // Unknown
                public static System.Drawing.Color BUBBLEGUM_BLOCK = ColorTranslator.FromHtml("#EB26E7"); // Unknown
                public static System.Drawing.Color TITANSTONE = ColorTranslator.FromHtml("#56555C"); // Unknown
                public static System.Drawing.Color PUMPKIN = ColorTranslator.FromHtml("#EB9617"); // Unknown
                public static System.Drawing.Color HAY = ColorTranslator.FromHtml("#99832C"); // Unknown
                public static System.Drawing.Color SPOOKY_WOOD = ColorTranslator.FromHtml("#393061"); // Unknown
                public static System.Drawing.Color PUMPKINS = ColorTranslator.FromHtml("#F89E5C"); // Unknown
                public static System.Drawing.Color AMETHYST_GEMSPARK_OFF = ColorTranslator.FromHtml("#56555C"); // Unknown
                public static System.Drawing.Color TOPAZ_GEMSPARK_OFF = ColorTranslator.FromHtml("#EB9617"); // Unknown
                public static System.Drawing.Color SAPPHIRE_GEMSPARK_OFF = ColorTranslator.FromHtml("#99832C"); // Unknown
                public static System.Drawing.Color EMERALD_GEMSPARK_OFF_WOOD = ColorTranslator.FromHtml("#393061"); // Unknown
                public static System.Drawing.Color RUBY_GEMSPARK_OFF = ColorTranslator.FromHtml("#F89E5C"); // Unknown
                public static System.Drawing.Color DIAMOND_GEMSPARK_OFF = ColorTranslator.FromHtml("#F89E5C"); // Unknown
                public static System.Drawing.Color AMBER_GEMSPARK_OFF = ColorTranslator.FromHtml("#F89E5C"); // Unknown
                public static System.Drawing.Color WOMANNEQUIN = ColorTranslator.FromHtml("#56555C"); // Unknown
                public static System.Drawing.Color TOPAZ_GEMSPARK_OFF_2 = ColorTranslator.FromHtml("#EB9617"); // Unknown
                public static System.Drawing.Color SAPPHIRE_GEMSPARK_OFF_2 = ColorTranslator.FromHtml("#99832C"); // Unknown
                public static System.Drawing.Color EMERALD_GEMSPARK_OFF_WOOD_2 = ColorTranslator.FromHtml("#393061"); // Unknown
                public static System.Drawing.Color RUBY_GEMSPARK_OFF_2 = ColorTranslator.FromHtml("#F89E5C"); // Unknown
                public static System.Drawing.Color DIAMOND_GEMSPARK_OFF_2 = ColorTranslator.FromHtml("#F89E5C"); // Unknown
                public static System.Drawing.Color AMBER_GEMSPARK_OFF_2 = ColorTranslator.FromHtml("#F89E5C"); // Unknown
                public static System.Drawing.Color COPPER_CACHE = ColorTranslator.FromHtml("#964316"); // Unknown
                public static System.Drawing.Color SILVER_CACHE = ColorTranslator.FromHtml("#B9C2C3"); // Unknown
                public static System.Drawing.Color GOLD_CACHE = ColorTranslator.FromHtml("#B9A417"); // Unknown
                public static System.Drawing.Color ENCHANTED_SWORD = ColorTranslator.FromHtml("#7998EF"); // Unknown

                public static System.Drawing.Color LAVA = ColorTranslator.FromHtml("#FD2003");
                public static System.Drawing.Color WATER = ColorTranslator.FromHtml("#093DBF");
                public static System.Drawing.Color HONEY = ColorTranslator.FromHtml("#FEC214");
                public static System.Drawing.Color SKY = ColorTranslator.FromHtml("#7B98FE");

                public static System.Drawing.Color WALL_HELL = System.Drawing.Color.FromArgb(50, 44, 38);

                //todo: //double check thesee... kept from last version.
                public static System.Drawing.Color WALL_STONE = System.Drawing.Color.FromArgb(66, 66, 66);
                public static System.Drawing.Color WALL_DIRT = System.Drawing.Color.FromArgb(88, 61, 46);
                public static System.Drawing.Color WALL_EBONSTONE = System.Drawing.Color.FromArgb(61, 58, 78);
                public static System.Drawing.Color WALL_WOOD = System.Drawing.Color.FromArgb(73, 51, 36);
                public static System.Drawing.Color WALL_BRICK = System.Drawing.Color.FromArgb(60, 60, 60);
                public static System.Drawing.Color WALL_BACKGROUND = System.Drawing.Color.FromArgb(74, 67, 60);
                public static System.Drawing.Color WALL_DUNGEON_PINK = System.Drawing.Color.FromArgb(84, 25, 60);
                public static System.Drawing.Color WALL_DUNGEON_BLUE = System.Drawing.Color.FromArgb(29, 31, 72);
                public static System.Drawing.Color WALL_DUNGEON_GREEN = System.Drawing.Color.FromArgb(14, 68, 16);
                public static System.Drawing.Color WALL_MUD = System.Drawing.Color.FromArgb(61, 46, 49);
                public static System.Drawing.Color WALL_HELLSTONE = System.Drawing.Color.FromArgb(48, 21, 21);
                public static System.Drawing.Color WALL_OBSIDIAN = System.Drawing.Color.FromArgb(87, 81, 173);

                public static System.Drawing.Color STONE_WALL = ColorTranslator.FromHtml("#343434"); // #424242
                public static System.Drawing.Color DIRT_WALL_UNSAFE = ColorTranslator.FromHtml("#583D2E"); // Wall Dirt
                public static System.Drawing.Color EBONSTONE_WALL_UNSAFE = ColorTranslator.FromHtml("#3D3A4E"); // #3D3A4E
                public static System.Drawing.Color WOOD_WALL = ColorTranslator.FromHtml("#493324"); // Wall Wood
                public static System.Drawing.Color GREY_BRICK_WALL = ColorTranslator.FromHtml("#343434"); // Wall Brick
                public static System.Drawing.Color RED_BRICK_WALL = ColorTranslator.FromHtml("#5B1E1E"); // Wall Brick
                public static System.Drawing.Color BLUE_BRICK_WALL_UNSAFE = ColorTranslator.FromHtml("#1B1F2A"); // Wall Dungeon Blue
                public static System.Drawing.Color GREEN_BRICK_WALL_UNSAFE = ColorTranslator.FromHtml("#1F271A"); // Wall Dungeon Green
                public static System.Drawing.Color PINK_BRICK_WALL_UNSAFE = ColorTranslator.FromHtml("#291C24"); // Wall Dungeon Pink
                public static System.Drawing.Color GOLD_BRICK_WALL = ColorTranslator.FromHtml("#4A3E0C"); // Wall Brick
                public static System.Drawing.Color SILVER_BRICK_WALL = ColorTranslator.FromHtml("#2E383B"); // Wall Brick
                public static System.Drawing.Color COPPER_BRICK_WALL = ColorTranslator.FromHtml("#4B200B"); // Wall Brick
                public static System.Drawing.Color HELLSTONE_BRICK_WALL_UNSAFE = ColorTranslator.FromHtml("#432525"); // #301515
                public static System.Drawing.Color OBSIDIAN_BRICK_WALL = ColorTranslator.FromHtml("#0F0F0F"); // #5751AD
                public static System.Drawing.Color MUD_WALL_UNSAFE = ColorTranslator.FromHtml("#342B2D"); // #3D2E31
                public static System.Drawing.Color DIRT_WALL = ColorTranslator.FromHtml("#583D2E"); // Wall Dirt
                public static System.Drawing.Color BLUE_BRICK_WALL = ColorTranslator.FromHtml("#1B1F2A"); // Wall Dungeon Blue
                public static System.Drawing.Color GREEN_BRICK_WALL = ColorTranslator.FromHtml("#1F271A"); // Wall Dungeon Green
                public static System.Drawing.Color PINK_BRICK_WALL = ColorTranslator.FromHtml("#291C24"); // Wall Dungeon Pink
                public static System.Drawing.Color OBSIDIAN_BRICK_WALL_2 = ColorTranslator.FromHtml("#0F0F0F"); // Wall Dirt
                public static System.Drawing.Color GLASS_WALL = ColorTranslator.FromHtml("#FF00FF"); // Wall Brick
                public static System.Drawing.Color PEARLSTONE_BRICK_WALL = ColorTranslator.FromHtml("#716363"); // Wall Brick
                public static System.Drawing.Color IRIDESCENT_BRICK_WALL = ColorTranslator.FromHtml("#26262B"); // Wall Brick
                public static System.Drawing.Color MUDSTONE_BRICK_WALL = ColorTranslator.FromHtml("#352729"); // Wall Brick
                public static System.Drawing.Color COBALT_BRICK_WALL = ColorTranslator.FromHtml("#0B233E"); // Wall Brick
                public static System.Drawing.Color MYTHRIL_BRICK_WALL = ColorTranslator.FromHtml("#153F46"); // Wall Brick
                public static System.Drawing.Color PLANKED_WALL = ColorTranslator.FromHtml("#583D2E"); // Wall Wood
                public static System.Drawing.Color PEARLSTONE_BRICK_WALL_UNSAFE = ColorTranslator.FromHtml("#515465"); // Wall Brick
                public static System.Drawing.Color CANDY_CANE_WALL = ColorTranslator.FromHtml("#581717"); // Wall Brick
                public static System.Drawing.Color GREEN_CANDY_CANE_WALL = ColorTranslator.FromHtml("#1C5817"); // Wall Brick
                public static System.Drawing.Color SNOW_BRICK_WALL = ColorTranslator.FromHtml("#4E5763"); // Wall Brick
                public static System.Drawing.Color ADAMANTITE_BEAM_WALL = ColorTranslator.FromHtml("#561128"); // Unknown
                public static System.Drawing.Color DEMONITE_BRICK_WALL = ColorTranslator.FromHtml("#312F53"); // Unknown
                public static System.Drawing.Color SANDSTONE_BRICK_WALL = ColorTranslator.FromHtml("#454329"); // Unknown
                public static System.Drawing.Color EBONSTONE_BRICK_WALL = ColorTranslator.FromHtml("#333346"); // Unknown
                public static System.Drawing.Color RED_STUCCO_WALL = ColorTranslator.FromHtml("#573B37"); // Unknown
                public static System.Drawing.Color YELLOW_STUCCO_WALL = ColorTranslator.FromHtml("#454329"); // Unknown
                public static System.Drawing.Color GREEN_STUCCO_WALL = ColorTranslator.FromHtml("#313931"); // Unknown
                public static System.Drawing.Color GRAY_WALL = ColorTranslator.FromHtml("#4E4F49"); // Unknown
                public static System.Drawing.Color SNOW_WALL_UNSAFE = ColorTranslator.FromHtml("#556667"); // Unknown
                public static System.Drawing.Color EBONWOOD_WALL = ColorTranslator.FromHtml("#34323E"); // Unknown
                public static System.Drawing.Color RICH_MAHOGANY_WALL = ColorTranslator.FromHtml("#472A2C"); // Unknown
                public static System.Drawing.Color PEARLWOOD_WALL = ColorTranslator.FromHtml("#494232"); // Unknown
                public static System.Drawing.Color RAINBOW_BRICK_WALL = ColorTranslator.FromHtml("#343434"); // Unknown
                public static System.Drawing.Color TIN_BRICK_WALL = ColorTranslator.FromHtml("#3C3B33"); // Unknown
                public static System.Drawing.Color TUNGSTEN_BRICK_WALL = ColorTranslator.FromHtml("#30392F"); // Unknown
                public static System.Drawing.Color PLATINUM_BRICK_WALL = ColorTranslator.FromHtml("#474D55"); // Unknown
                public static System.Drawing.Color AMETHYST_WALL_UNSAFE = ColorTranslator.FromHtml("#343434"); // Unknown
                public static System.Drawing.Color TOPAZ_WALL_UNSAFE = ColorTranslator.FromHtml("#343434"); // Unknown
                public static System.Drawing.Color SAPPHIRE_WALL_UNSAFE = ColorTranslator.FromHtml("#343434"); // Unknown
                public static System.Drawing.Color EMERALD_WALL_UNSAFE = ColorTranslator.FromHtml("#343434"); // Unknown
                public static System.Drawing.Color RUBY_WALL_UNSAFE = ColorTranslator.FromHtml("#343434"); // Unknown
                public static System.Drawing.Color DIAMOND_WALL_UNSAFE = ColorTranslator.FromHtml("#343434"); // Unknown
                public static System.Drawing.Color UNIQUE_CAVE_WALL_1_UNSAFE = ColorTranslator.FromHtml("#283832"); // Unknown
                public static System.Drawing.Color UNIQUE_CAVE_WALL_2_UNSAFE = ColorTranslator.FromHtml("#313024"); // Unknown
                public static System.Drawing.Color UNIQUE_CAVE_WALL_3_UNSAFE = ColorTranslator.FromHtml("#2B2120"); // Unknown
                public static System.Drawing.Color UNIQUE_CAVE_WALL_4_UNSAFE = ColorTranslator.FromHtml("#1F2831"); // Unknown
                public static System.Drawing.Color UNIQUE_CAVE_WALL_5_UNSAFE = ColorTranslator.FromHtml("#302334"); // Unknown
                public static System.Drawing.Color CAVE_WALL_UNSAFE = ColorTranslator.FromHtml("#583D2E"); // Unknown
                public static System.Drawing.Color LEAVES_WALL_UNSAFE = ColorTranslator.FromHtml("#013414"); // Unknown
                public static System.Drawing.Color UNIQUE_CAVE_WALL_6_UNSAFE = ColorTranslator.FromHtml("#37271A"); // Unknown
                public static System.Drawing.Color SPIDER_WALL_UNSAFE = ColorTranslator.FromHtml("#27211A"); // Unknown
                public static System.Drawing.Color GRASS_WALL_UNSAFE = ColorTranslator.FromHtml("#1E5030"); // Unknown
                public static System.Drawing.Color JUNGLE_WALL_UNSAFE = ColorTranslator.FromHtml("#35501E"); // Unknown
                public static System.Drawing.Color FLOWER_WALL_UNSAFE = ColorTranslator.FromHtml("#1E5030"); // Unknown
                public static System.Drawing.Color GRASS_WALL = ColorTranslator.FromHtml("#1E5030"); // Unknown
                public static System.Drawing.Color JUNGLE_WALL = ColorTranslator.FromHtml("#35501E"); // Unknown
                public static System.Drawing.Color FLOWER_WALL = ColorTranslator.FromHtml("#1E5030"); // Unknown
                public static System.Drawing.Color CORRUPT_GRASS_WALL_UNSAFE = ColorTranslator.FromHtml("#2B2A44"); // Unknown
                public static System.Drawing.Color HALLOWED_GRASS_WALL_UNSAFE = ColorTranslator.FromHtml("#1E4650"); // Unknown
                public static System.Drawing.Color ICE_WALL_UNSAFE = ColorTranslator.FromHtml("#4E6987"); // Unknown
                public static System.Drawing.Color CACTUS_WALL = ColorTranslator.FromHtml("#34540C"); // Unknown
                public static System.Drawing.Color CLOUD_WALL = ColorTranslator.FromHtml("#BECCDF"); // Unknown
                public static System.Drawing.Color MUSHROOM_WALL = ColorTranslator.FromHtml("#403E50"); // Unknown
                public static System.Drawing.Color BONE_BLOCK_WALL = ColorTranslator.FromHtml("#414123"); // Unknown
                public static System.Drawing.Color SLIME_BLOCK_WALL = ColorTranslator.FromHtml("#142E68"); // Unknown
                public static System.Drawing.Color FLESH_BLOCK_WALL = ColorTranslator.FromHtml("#3D0D10"); // Unknown
                public static System.Drawing.Color LIVING_WOOD_WALL = ColorTranslator.FromHtml("#3F271A"); // Unknown
                public static System.Drawing.Color OBSIDIAN_BACK_WALL_UNSAFE = ColorTranslator.FromHtml("#332F60"); // Unknown
                public static System.Drawing.Color MUSHROOM_WALL_UNSAFE = ColorTranslator.FromHtml("#403E50"); // Unknown
                public static System.Drawing.Color CRIMGRASS_WALL_UNSAFE = ColorTranslator.FromHtml("#653333"); // Unknown
                public static System.Drawing.Color DISC_WALL = ColorTranslator.FromHtml("#4D4022"); // Unknown
                public static System.Drawing.Color CRIMSTONE_WALL_UNSAFE = ColorTranslator.FromHtml("#3E2629"); // Unknown
                public static System.Drawing.Color ICE_BRICK_WALL = ColorTranslator.FromHtml("#304E5D"); // Unknown
                public static System.Drawing.Color SHADEWOOD_WALL = ColorTranslator.FromHtml("#363F45"); // Unknown
                public static System.Drawing.Color HIVE_WALL_UNSAFE = ColorTranslator.FromHtml("#8A4926"); // Unknown
                public static System.Drawing.Color LIHZAHRD_BRICK_WALL_UNSAFE = ColorTranslator.FromHtml("#141311"); // Unknown
                public static System.Drawing.Color PURPLE_STAINED_GLASS = ColorTranslator.FromHtml("#FF00FF"); // Unknown
                public static System.Drawing.Color YELLOW_STAINED_GLASS = ColorTranslator.FromHtml("#FF00FF"); // Unknown
                public static System.Drawing.Color BLUE_STAINED_GLASS = ColorTranslator.FromHtml("#FF00FF"); // Unknown
                public static System.Drawing.Color GREEN_STAINED_GLASS = ColorTranslator.FromHtml("#FF00FF"); // Unknown
                public static System.Drawing.Color RED_STAINED_GLASS = ColorTranslator.FromHtml("#FF00FF"); // Unknown
                public static System.Drawing.Color MULTICOLORED_STAINED_GLASS = ColorTranslator.FromHtml("#FF00FF"); // Unknown
                public static System.Drawing.Color BLUE_SLAB_WALL_UNSAFE = ColorTranslator.FromHtml("#20282D"); // Unknown
                public static System.Drawing.Color BLUE_TILED_WALL_UNSAFE = ColorTranslator.FromHtml("#2C2932"); // Unknown
                public static System.Drawing.Color PINK_SLAB_WALL_UNSAFE = ColorTranslator.FromHtml("#48324D"); // Unknown
                public static System.Drawing.Color PINK_TILED_WALL_UNSAFE = ColorTranslator.FromHtml("#4E3245"); // Unknown
                public static System.Drawing.Color GREEN_SLAB_WALL_UNSAFE = ColorTranslator.FromHtml("#242D2C"); // Unknown
                public static System.Drawing.Color GREEN_TILED_WALL_UNSAFE = ColorTranslator.FromHtml("#263132"); // Unknown
                public static System.Drawing.Color BLUE_SLAB_WALL = ColorTranslator.FromHtml("#20282D"); // Unknown
                public static System.Drawing.Color BLUE_TILED_WALL = ColorTranslator.FromHtml("#2C2932"); // Unknown
                public static System.Drawing.Color PINK_SLAB_WALL = ColorTranslator.FromHtml("#48324D"); // Unknown
                public static System.Drawing.Color PINK_TILED_WALL = ColorTranslator.FromHtml("#4E3245"); // Unknown
                public static System.Drawing.Color GREEN_SLAB_WALL = ColorTranslator.FromHtml("#242D2C"); // Unknown
                public static System.Drawing.Color GREEN_TILED_WALL = ColorTranslator.FromHtml("#263132"); // Unknown
                public static System.Drawing.Color WOODEN_FENCE = ColorTranslator.FromHtml("#FF00FF"); // Unknown
                public static System.Drawing.Color METAL_FENCE = ColorTranslator.FromHtml("#FF00FF"); // Unknown
                public static System.Drawing.Color HIVE_WALL = ColorTranslator.FromHtml("#8A4926"); // Unknown
                public static System.Drawing.Color PALLADIUM_COLUMN_WALL = ColorTranslator.FromHtml("#5E1911"); // Unknown
                public static System.Drawing.Color BUBBLEGUM_BLOCK_WALL = ColorTranslator.FromHtml("#7D247A"); // Unknown
                public static System.Drawing.Color TITANSTONE_BLOCK_WALL = ColorTranslator.FromHtml("#33231B"); // Unknown
                public static System.Drawing.Color LIHZAHRD_BRICK_WALL = ColorTranslator.FromHtml("#141311"); // Unknown
                public static System.Drawing.Color PUMPKIN_WALL = ColorTranslator.FromHtml("#873A00"); // Unknown
                public static System.Drawing.Color HAY_WALL = ColorTranslator.FromHtml("#41340F"); // Unknown
                public static System.Drawing.Color SPOOKY_WOOD_WALL = ColorTranslator.FromHtml("#272A33"); // Unknown
                public static System.Drawing.Color CHRISTMAS_TREE_WALLPAPER = ColorTranslator.FromHtml("#591A1B"); // Unknown
                public static System.Drawing.Color ORNAMENT_WALLPAPER = ColorTranslator.FromHtml("#7E7B73"); // Unknown
                public static System.Drawing.Color CANDY_CANE_WALLPAPER = ColorTranslator.FromHtml("#083213"); // Unknown
                public static System.Drawing.Color FESTIVE_WALLPAPER = ColorTranslator.FromHtml("#5F1518"); // Unknown
                public static System.Drawing.Color STARS_WALLPAPER = ColorTranslator.FromHtml("#111F41"); // Unknown
                public static System.Drawing.Color SQUIGGLES_WALLPAPER = ColorTranslator.FromHtml("#C0AD8F"); // Unknown
                public static System.Drawing.Color SNOWFLAKE_WALLPAPER = ColorTranslator.FromHtml("#727283"); // Unknown
                public static System.Drawing.Color KRAMPUS_HORN_WALLPAPER = ColorTranslator.FromHtml("#887707"); // Unknown
                public static System.Drawing.Color BLUEGREEN_WALLPAPER = ColorTranslator.FromHtml("#084803"); // Unknown
                public static System.Drawing.Color GRINCH_FINGER_WALLPAPER = ColorTranslator.FromHtml("#758452"); // Unknown
                public static System.Drawing.Color FANCY_GREY_WALLPAPER = ColorTranslator.FromHtml("#111F41"); // Unknown
                public static System.Drawing.Color ICE_FLOE_WALLPAPER = ColorTranslator.FromHtml("#C0AD8F"); // Unknown
                public static System.Drawing.Color MUSIC_WALLPAPER = ColorTranslator.FromHtml("#727283"); // Unknown
                public static System.Drawing.Color PURPLERAIN_WALLPAPER = ColorTranslator.FromHtml("#887707"); // Unknown
                public static System.Drawing.Color RAINBOW_WALLPAPER = ColorTranslator.FromHtml("#084803"); // Unknown
                public static System.Drawing.Color SPARKLE_STONE_WALLPAPER = ColorTranslator.FromHtml("#758452"); // Unknown
                public static System.Drawing.Color STARLIT_HEAVEN_WALLPAPER = ColorTranslator.FromHtml("#727283"); // Unknown
                public static System.Drawing.Color BUBBLE_WALLPAPER = ColorTranslator.FromHtml("#887707"); // Unknown
                public static System.Drawing.Color COPPERPIPE_WALLPAPER = ColorTranslator.FromHtml("#084803"); // Unknown
                public static System.Drawing.Color DUCKY_WALLPAPER = ColorTranslator.FromHtml("#758452"); // Unknown
                public static System.Drawing.Color WATERFALLR = ColorTranslator.FromHtml("#111F41"); // Unknown
                public static System.Drawing.Color LAVAFALL = ColorTranslator.FromHtml("#C0AD8F"); // Unknown
                public static System.Drawing.Color EBONWOOD_FENCE = ColorTranslator.FromHtml("#727283"); // Unknown
                public static System.Drawing.Color RICHMAHOGANY_FENCE = ColorTranslator.FromHtml("#887707"); // Unknown
                public static System.Drawing.Color PEARLWOOD_FENCE = ColorTranslator.FromHtml("#084803"); // Unknown
                public static System.Drawing.Color SHADEWOOD_FENCE = ColorTranslator.FromHtml("#758452"); // Unknown
                public static System.Drawing.Color WHITE_DYNASTY = ColorTranslator.FromHtml("#727283"); // Unknown
                public static System.Drawing.Color BLUE_DYNASTY = ColorTranslator.FromHtml("#887707"); // Unknown
                public static System.Drawing.Color ARCANE_RUNES = ColorTranslator.FromHtml("#084803"); // Unknown
                public static System.Drawing.Color IRON_FENCE = ColorTranslator.FromHtml("#887707"); // Unknown
                public static System.Drawing.Color COPPER_PLATING = ColorTranslator.FromHtml("#084803"); // Unknown
                public static System.Drawing.Color STONE_SLAB = ColorTranslator.FromHtml("#758452"); // Unknown
                public static System.Drawing.Color SAIL = ColorTranslator.FromHtml("#727283"); // Unknown
                public static System.Drawing.Color BOREAL_WOOD = ColorTranslator.FromHtml("#887707"); // Unknown
                public static System.Drawing.Color BOREAL_WOOD_FENCE = ColorTranslator.FromHtml("#084803"); // Unknown
                public static System.Drawing.Color PALM_WOOD = ColorTranslator.FromHtml("#887707"); // Unknown
                public static System.Drawing.Color PALM_WOOD_FENCE = ColorTranslator.FromHtml("#084803"); // Unknown
                public static System.Drawing.Color AMBER_GEMSPARK = ColorTranslator.FromHtml("#887707"); // Unknown
                public static System.Drawing.Color AMETHYST_GEMSPARK = ColorTranslator.FromHtml("#084803"); // Unknown
                public static System.Drawing.Color DIAMOND_GEMSPARK = ColorTranslator.FromHtml("#084803"); // Unknown
                public static System.Drawing.Color EMERALD_GEMSPARK = ColorTranslator.FromHtml("#084803"); // Unknown
                public static System.Drawing.Color AMBER_GEMSPARK_OFF_WALL = ColorTranslator.FromHtml("#887707"); // Unknown
                public static System.Drawing.Color AMETHYST_GEMSPARK_OFF_WALL = ColorTranslator.FromHtml("#084803"); // Unknown
                public static System.Drawing.Color DIAMOND_GEMSPARK_OFF_WALL = ColorTranslator.FromHtml("#084803"); // Unknown
                public static System.Drawing.Color EMERALD_GEMSPARK_OFF = ColorTranslator.FromHtml("#084803"); // Unknown
                public static System.Drawing.Color RUBY_GEMSPARK_OFF_WALL = ColorTranslator.FromHtml("#084803"); // Unknown
                public static System.Drawing.Color SAPPHIRE_GEMSPARK_OFF_WALL = ColorTranslator.FromHtml("#084803"); // Unknown
                public static System.Drawing.Color TOPAZ_GEMSPARK_OFF_WALL = ColorTranslator.FromHtml("#084803"); // Unknown
                public static System.Drawing.Color RUBY_GEMSPARK = ColorTranslator.FromHtml("#084803"); // Unknown
                public static System.Drawing.Color SAPPHIRE_GEMSPARK = ColorTranslator.FromHtml("#084803"); // Unknown
                public static System.Drawing.Color TOPAZ_GEMSPARK = ColorTranslator.FromHtml("#084803"); // Unknown
                public static System.Drawing.Color TIN_PLATING = ColorTranslator.FromHtml("#084803"); // Unknown
                public static System.Drawing.Color CONFETTI = ColorTranslator.FromHtml("#084803"); // Unknown
                public static System.Drawing.Color CONFETTI_BLACK = ColorTranslator.FromHtml("#084803"); // Unknown
                public static System.Drawing.Color CAVE_WALL = ColorTranslator.FromHtml("#084803"); // Unknown
                public static System.Drawing.Color CAVE_WALL2 = ColorTranslator.FromHtml("#084803"); // Unknown
                public static System.Drawing.Color COUNT = ColorTranslator.FromHtml("#758452"); // Unknown

                public static System.Drawing.Color UNKNOWN = System.Drawing.Color.Magenta;
            }
        }

        public static void InitializeMapperDefs() //Credits go to the authors of MoreTerra
        {
            tileTypeDefs = new Dictionary<int, System.Drawing.Color>(623);

            tileTypeDefs[0] = Constants.MoreTerra_Color.DIRT; // #AF8365
            tileTypeDefs[1] = Constants.MoreTerra_Color.STONE; // #808080
            tileTypeDefs[2] = Constants.MoreTerra_Color.GRASS; // #1CD85E
            tileTypeDefs[3] = Constants.MoreTerra_Color.PLANTS; // Plants
            tileTypeDefs[4] = Constants.MoreTerra_Color.TORCH; // Light Source
            tileTypeDefs[5] = Constants.MoreTerra_Color.TREES; // Wood
            tileTypeDefs[6] = Constants.MoreTerra_Color.IRON; // #BD9F8B
            tileTypeDefs[7] = Constants.MoreTerra_Color.COPPER; // #FF9532
            tileTypeDefs[8] = Constants.MoreTerra_Color.GOLD; // #B9A417
            tileTypeDefs[9] = Constants.MoreTerra_Color.SILVER; // #D9DFDF
            tileTypeDefs[10] = Constants.MoreTerra_Color.CLOSED_DOOR; // Decorative
            tileTypeDefs[11] = Constants.MoreTerra_Color.OPEN_DOOR; // Decorative
            tileTypeDefs[12] = Constants.MoreTerra_Color.HEART; // Important
            tileTypeDefs[13] = Constants.MoreTerra_Color.BOTTLES; // Decorative
            tileTypeDefs[14] = Constants.MoreTerra_Color.TABLE; // Decorative
            tileTypeDefs[15] = Constants.MoreTerra_Color.CHAIRS; // Decorative
            tileTypeDefs[16] = Constants.MoreTerra_Color.ANVIL; // Decorative
            tileTypeDefs[17] = Constants.MoreTerra_Color.FURNACE; // Decorative
            tileTypeDefs[18] = Constants.MoreTerra_Color.WORK_BENCH; // Decorative
            tileTypeDefs[19] = Constants.MoreTerra_Color.WOOD_PLATFORM; // Wood
            tileTypeDefs[20] = Constants.MoreTerra_Color.SAPLINGS; // Plants
            tileTypeDefs[21] = Constants.MoreTerra_Color.CONTAINERS; // Important
            tileTypeDefs[22] = Constants.MoreTerra_Color.DEMONITE; // #625FA7
            tileTypeDefs[23] = Constants.MoreTerra_Color.CORRUPT_GRASS; // Corruption Grass
            tileTypeDefs[24] = Constants.MoreTerra_Color.CORRUPT_PLANTS; // Corruption Grass
            tileTypeDefs[25] = Constants.MoreTerra_Color.EBONSTONE; // #4B4A82
            tileTypeDefs[26] = Constants.MoreTerra_Color.DEMON_ALTAR; // Important
            tileTypeDefs[27] = Constants.MoreTerra_Color.SUNFLOWER; // Plants
            tileTypeDefs[28] = Constants.MoreTerra_Color.POTS; // Important
            tileTypeDefs[29] = Constants.MoreTerra_Color.PIGGY_BANK; // Decorative
            tileTypeDefs[30] = Constants.MoreTerra_Color.WOOD_BLOCK; // #A87957
            tileTypeDefs[31] = Constants.MoreTerra_Color.SHADOW_ORB; // Important
            tileTypeDefs[32] = Constants.MoreTerra_Color.CORRUPT_THORNS; // #7A618F
            tileTypeDefs[33] = Constants.MoreTerra_Color.CANDLE; // Light Source
            tileTypeDefs[34] = Constants.MoreTerra_Color.CHANDELIERS; // Light Source
            tileTypeDefs[35] = Constants.MoreTerra_Color.JACK_O_LANTERNS; // Light Source
            tileTypeDefs[36] = Constants.MoreTerra_Color.PRESENTS; // Decorative
            tileTypeDefs[37] = Constants.MoreTerra_Color.METEORITE; // #FF00FF
            tileTypeDefs[38] = Constants.MoreTerra_Color.GRAY_BRICK; // Block
            tileTypeDefs[39] = Constants.MoreTerra_Color.RED_BRICK; // Block
            tileTypeDefs[40] = Constants.MoreTerra_Color.CLAY_BLOCK; // #D87365
            tileTypeDefs[41] = Constants.MoreTerra_Color.BLUE_BRICK; // #4245C2
            tileTypeDefs[42] = Constants.MoreTerra_Color.HANGING_GLOBE; // Light Source
            tileTypeDefs[43] = Constants.MoreTerra_Color.GREEN_BRICK; // #1A8822
            tileTypeDefs[44] = Constants.MoreTerra_Color.PINK_BRICK; // #A93175
            tileTypeDefs[45] = Constants.MoreTerra_Color.GOLD_BRICK; // Block
            tileTypeDefs[46] = Constants.MoreTerra_Color.SILVER_BRICK; // Block
            tileTypeDefs[47] = Constants.MoreTerra_Color.COPPER_BRICK; // Block
            tileTypeDefs[48] = Constants.MoreTerra_Color.SPIKE; // #6D6D6D
            tileTypeDefs[49] = Constants.MoreTerra_Color.WATER_CANDLE; // Light Source
            tileTypeDefs[50] = Constants.MoreTerra_Color.BOOKS; // Decorative
            tileTypeDefs[51] = Constants.MoreTerra_Color.COBWEB; // #F0F0F0
            tileTypeDefs[52] = Constants.MoreTerra_Color.VINES; // Plants
            tileTypeDefs[53] = Constants.MoreTerra_Color.SAND; // #FFDA38
            tileTypeDefs[54] = Constants.MoreTerra_Color.GLASS; // Decorative
            tileTypeDefs[55] = Constants.MoreTerra_Color.SIGNS; // Decorative
            tileTypeDefs[56] = Constants.MoreTerra_Color.OBSIDIAN; // #5751AD
            tileTypeDefs[57] = Constants.MoreTerra_Color.ASH; // #44444C
            tileTypeDefs[58] = Constants.MoreTerra_Color.HELLSTONE; // #662222
            tileTypeDefs[59] = Constants.MoreTerra_Color.MUD; // #5C4449
            tileTypeDefs[60] = Constants.MoreTerra_Color.JUNGLE_GRASS; // Jungle Plants
            tileTypeDefs[61] = Constants.MoreTerra_Color.JUNGLE_PLANTS; // Jungle Plants
            tileTypeDefs[62] = Constants.MoreTerra_Color.JUNGLE_VINES; // #8ACE1C
            tileTypeDefs[63] = Constants.MoreTerra_Color.SAPPHIRE; // Gems
            tileTypeDefs[64] = Constants.MoreTerra_Color.RUBY; // Gems
            tileTypeDefs[65] = Constants.MoreTerra_Color.EMERALD; // Gems
            tileTypeDefs[66] = Constants.MoreTerra_Color.TOPAZ; // Gems
            tileTypeDefs[67] = Constants.MoreTerra_Color.AMETHYST; // Gems
            tileTypeDefs[68] = Constants.MoreTerra_Color.DIAMOND; // Gems
            tileTypeDefs[69] = Constants.MoreTerra_Color.JUNGLE_THORNS; // #5E3037
            tileTypeDefs[70] = Constants.MoreTerra_Color.MUSHROOM_GRASS; // #5D7FFF
            tileTypeDefs[71] = Constants.MoreTerra_Color.MUSHROOM_PLANTS; // #B1AE83
            tileTypeDefs[72] = Constants.MoreTerra_Color.MUSHROOM_TREES; // #968F6E
            tileTypeDefs[73] = Constants.MoreTerra_Color.PLANTS2; // Plants
            tileTypeDefs[74] = Constants.MoreTerra_Color.PLANTS3; // Plants
            tileTypeDefs[75] = Constants.MoreTerra_Color.OBSIDIAN_BRICK; // Block
            tileTypeDefs[76] = Constants.MoreTerra_Color.HELLSTONE_BRICK; // Block
            tileTypeDefs[77] = Constants.MoreTerra_Color.HELLFORGE; // Important
            tileTypeDefs[78] = Constants.MoreTerra_Color.CLAY_POT; // Decorative
            tileTypeDefs[79] = Constants.MoreTerra_Color.BED; // Decorative
            tileTypeDefs[80] = Constants.MoreTerra_Color.CACTUS; // #006400
            tileTypeDefs[81] = Constants.MoreTerra_Color.CORAL; // #FFB6C1
            tileTypeDefs[82] = Constants.MoreTerra_Color.IMMATURE_HERBS; // Herb
            tileTypeDefs[83] = Constants.MoreTerra_Color.MATURE_HERBS; // Herb
            tileTypeDefs[84] = Constants.MoreTerra_Color.BLOOMING_HERBS; // Herb
            tileTypeDefs[85] = Constants.MoreTerra_Color.TOMBSTONE; // #696969
            tileTypeDefs[86] = Constants.MoreTerra_Color.LOOM; // Decorative
            tileTypeDefs[87] = Constants.MoreTerra_Color.PIANO; // Decorative
            tileTypeDefs[88] = Constants.MoreTerra_Color.DRESSER; // Decorative
            tileTypeDefs[89] = Constants.MoreTerra_Color.BENCH; // Decorative
            tileTypeDefs[90] = Constants.MoreTerra_Color.BATHTUB; // Decorative
            tileTypeDefs[91] = Constants.MoreTerra_Color.BANNERS; // Decorative
            tileTypeDefs[92] = Constants.MoreTerra_Color.LAMPPOST; // Light Source
            tileTypeDefs[93] = Constants.MoreTerra_Color.TIKI_TORCH; // Light Source
            tileTypeDefs[94] = Constants.MoreTerra_Color.KEG; // Decorative
            tileTypeDefs[95] = Constants.MoreTerra_Color.CHINESE_LANTERN; // Light Source
            tileTypeDefs[96] = Constants.MoreTerra_Color.COOKING_POT; // Decorative
            tileTypeDefs[97] = Constants.MoreTerra_Color.SAFE; // Decorative
            tileTypeDefs[98] = Constants.MoreTerra_Color.SKULL_CANDLE; // Light Source
            tileTypeDefs[99] = Constants.MoreTerra_Color.TRASH_CAN; // Decorative
            tileTypeDefs[100] = Constants.MoreTerra_Color.CANDELABRA; // Light Source
            tileTypeDefs[101] = Constants.MoreTerra_Color.BOOKCASE; // Decorative
            tileTypeDefs[102] = Constants.MoreTerra_Color.THRONE; // Decorative
            tileTypeDefs[103] = Constants.MoreTerra_Color.BOWL; // Decorative
            tileTypeDefs[104] = Constants.MoreTerra_Color.GRANDFATHER_CLOCK; // Decorative
            tileTypeDefs[105] = Constants.MoreTerra_Color.STATUE; // Decorative
            tileTypeDefs[106] = Constants.MoreTerra_Color.SAWMILL; // Decorative
            tileTypeDefs[107] = Constants.MoreTerra_Color.COBALT; // #0B508F
            tileTypeDefs[108] = Constants.MoreTerra_Color.MYTHRIL; // #5BA9A9
            tileTypeDefs[109] = Constants.MoreTerra_Color.HALLOWED_GRASS; // Hallowed Plants
            tileTypeDefs[110] = Constants.MoreTerra_Color.HALLOWED_PLANTS; // Hallowed Plants
            tileTypeDefs[111] = Constants.MoreTerra_Color.ADAMANTITE; // #801A34
            tileTypeDefs[112] = Constants.MoreTerra_Color.EBONSAND; // #595353
            tileTypeDefs[113] = Constants.MoreTerra_Color.HALLOWED_PLANTS_2; // Hallowed Plants
            tileTypeDefs[114] = Constants.MoreTerra_Color.TINKERERS_WORKBENCH; // Decorative
            tileTypeDefs[115] = Constants.MoreTerra_Color.HALLOWED_VINES; // Hallowed Vines
            tileTypeDefs[116] = Constants.MoreTerra_Color.PEARLSAND; // #EEE1DA
            tileTypeDefs[117] = Constants.MoreTerra_Color.PEARLSTONE; // #B5ACBE
            tileTypeDefs[118] = Constants.MoreTerra_Color.PEARLSTONE_BRICK; // Block
            tileTypeDefs[119] = Constants.MoreTerra_Color.IRIDESCENT_BRICK; // Block
            tileTypeDefs[120] = Constants.MoreTerra_Color.UNKNOWN_NEW_BRICK; // Unknown
            tileTypeDefs[121] = Constants.MoreTerra_Color.COBALT_BRICK; // Block
            tileTypeDefs[122] = Constants.MoreTerra_Color.MYTHRIL_BRICK; // Block
            tileTypeDefs[123] = Constants.MoreTerra_Color.SILT; // #67627A
            tileTypeDefs[124] = Constants.MoreTerra_Color.WOODEN_PLANK; // Wood
            tileTypeDefs[125] = Constants.MoreTerra_Color.CRYSTAL_BALL; // Decorative
            tileTypeDefs[126] = Constants.MoreTerra_Color.DISCO_BALL; // Decorative
            tileTypeDefs[127] = Constants.MoreTerra_Color.ICE_BLOCK; // Block
            tileTypeDefs[128] = Constants.MoreTerra_Color.MANNEQUIN; // Decorative
            tileTypeDefs[129] = Constants.MoreTerra_Color.CRYSTALS; // #785AD9
            tileTypeDefs[130] = Constants.MoreTerra_Color.ACTIVE_STONE_BLOCK; // Mechanical
            tileTypeDefs[131] = Constants.MoreTerra_Color.INACTIVE_STONE_BLOCK; // Mechanical
            tileTypeDefs[132] = Constants.MoreTerra_Color.LEVER; // Mechanical
            tileTypeDefs[133] = Constants.MoreTerra_Color.ADAMANTITE_FORGE; // Decorative
            tileTypeDefs[134] = Constants.MoreTerra_Color.MYTHRIL_ANVIL; // Decorative
            tileTypeDefs[135] = Constants.MoreTerra_Color.PRESSURE_PLATES; // Mechanical
            tileTypeDefs[136] = Constants.MoreTerra_Color.SWITCHES; // Mechanical
            tileTypeDefs[137] = Constants.MoreTerra_Color.DART_TRAP; // Mechanical
            tileTypeDefs[138] = Constants.MoreTerra_Color.BOULDER; // Decorative
            tileTypeDefs[139] = Constants.MoreTerra_Color.MUSIC_BOXES; // Decorative
            tileTypeDefs[140] = Constants.MoreTerra_Color.EBONSTONE_BRICK; // Block
            tileTypeDefs[141] = Constants.MoreTerra_Color.EXPLOSIVES; // Mechanical
            tileTypeDefs[142] = Constants.MoreTerra_Color.INLET_PUMP; // Mechanical
            tileTypeDefs[143] = Constants.MoreTerra_Color.OUTLET_PUMP; // Mechanical
            tileTypeDefs[144] = Constants.MoreTerra_Color.TIMERS; // Mechanical
            tileTypeDefs[145] = Constants.MoreTerra_Color.CANDY_CANE_BLOCK; // Block
            tileTypeDefs[146] = Constants.MoreTerra_Color.GREEN_CANDY_CANE_BLOCK; // Block
            tileTypeDefs[147] = Constants.MoreTerra_Color.SNOW_BLOCK; // #FFFFFF
            tileTypeDefs[148] = Constants.MoreTerra_Color.SNOW_BRICK; // Block
            tileTypeDefs[149] = Constants.MoreTerra_Color.HOLIDAY_LIGHTS; // Light Source
            tileTypeDefs[150] = Constants.MoreTerra_Color.ADAMANTITE_BEAM; // Unknown
            tileTypeDefs[151] = Constants.MoreTerra_Color.SANDSTONE_BRICK; // Unknown
            tileTypeDefs[152] = Constants.MoreTerra_Color.EBONSTONE_BRICK_2; // Unknown
            tileTypeDefs[153] = Constants.MoreTerra_Color.RED_STUCCO; // Unknown
            tileTypeDefs[154] = Constants.MoreTerra_Color.YELLOW_STUCCO; // Unknown
            tileTypeDefs[155] = Constants.MoreTerra_Color.GREEN_STUCCO; // Unknown
            tileTypeDefs[156] = Constants.MoreTerra_Color.GRAY_STUCCO; // Unknown
            tileTypeDefs[157] = Constants.MoreTerra_Color.EBONWOOD; // Unknown
            tileTypeDefs[158] = Constants.MoreTerra_Color.RICH_MAHOGANY; // Unknown
            tileTypeDefs[159] = Constants.MoreTerra_Color.PEARLWOOD; // Unknown
            tileTypeDefs[160] = Constants.MoreTerra_Color.RAINBOW_BRICK; // Unknown
            tileTypeDefs[161] = Constants.MoreTerra_Color.ICE; // Unknown
            tileTypeDefs[162] = Constants.MoreTerra_Color.BREAKABLE_ICE; // Unknown
            tileTypeDefs[163] = Constants.MoreTerra_Color.PURPLE_ICE; // Unknown
            tileTypeDefs[164] = Constants.MoreTerra_Color.PINK_ICE; // Unknown
            tileTypeDefs[165] = Constants.MoreTerra_Color.STALAGTITE; // Unknown
            tileTypeDefs[166] = Constants.MoreTerra_Color.TIN_ORE; // #E3DBA2
            tileTypeDefs[167] = Constants.MoreTerra_Color.LEAD_ORE; // #55727B
            tileTypeDefs[168] = Constants.MoreTerra_Color.TUNGSTEN_ORE; // #5A7D53
            tileTypeDefs[169] = Constants.MoreTerra_Color.PLATINUM_ORE; // #8097B8
            tileTypeDefs[170] = Constants.MoreTerra_Color.PINE_TREE; // Unknown
            tileTypeDefs[171] = Constants.MoreTerra_Color.CHRISTMAS_TREE; // Unknown
            tileTypeDefs[172] = Constants.MoreTerra_Color.UNUSED_PLATINUM_CHANDELIER; // Unknown
            tileTypeDefs[173] = Constants.MoreTerra_Color.PLATINUM_CANDELABRA; // Unknown
            tileTypeDefs[174] = Constants.MoreTerra_Color.PLATINUM_CANDLE; // Unknown
            tileTypeDefs[175] = Constants.MoreTerra_Color.TIN_BRICK; // Unknown
            tileTypeDefs[176] = Constants.MoreTerra_Color.TUNGSTEN_BRICK; // Unknown
            tileTypeDefs[177] = Constants.MoreTerra_Color.PLATINUM_BRICK; // Unknown
            tileTypeDefs[178] = Constants.MoreTerra_Color.EXPOSED_GEMS; // Unknown
            tileTypeDefs[179] = Constants.MoreTerra_Color.GREEN_MOSS; // Unknown
            tileTypeDefs[180] = Constants.MoreTerra_Color.BROWN_MOSS; // Unknown
            tileTypeDefs[181] = Constants.MoreTerra_Color.RED_MOSS; // Unknown
            tileTypeDefs[182] = Constants.MoreTerra_Color.BLUE_MOSS; // Unknown
            tileTypeDefs[183] = Constants.MoreTerra_Color.PURPLE_MOSS; // Unknown
            tileTypeDefs[184] = Constants.MoreTerra_Color.LONG_MOSS; // Unknown
            tileTypeDefs[185] = Constants.MoreTerra_Color.SMALL_DETRITUS; // Unknown
            tileTypeDefs[186] = Constants.MoreTerra_Color.LARGE_DETRITUS; // Unknown
            tileTypeDefs[187] = Constants.MoreTerra_Color.LARGE_DETRITUS2; // Unknown
            tileTypeDefs[188] = Constants.MoreTerra_Color.CACTUS_BLOCK; // Unknown
            tileTypeDefs[189] = Constants.MoreTerra_Color.CLOUD; // Unknown
            tileTypeDefs[190] = Constants.MoreTerra_Color.GLOWING_MUSHROOM; // Unknown
            tileTypeDefs[191] = Constants.MoreTerra_Color.LIVING_WOOD; // Unknown
            tileTypeDefs[192] = Constants.MoreTerra_Color.LEAF_BLOCK; // Unknown
            tileTypeDefs[193] = Constants.MoreTerra_Color.SLIME_BLOCK; // Unknown
            tileTypeDefs[194] = Constants.MoreTerra_Color.BONE_BLOCK; // Unknown
            tileTypeDefs[195] = Constants.MoreTerra_Color.FLESH_BLOCK; // Unknown
            tileTypeDefs[196] = Constants.MoreTerra_Color.RAIN_CLOUD; // Unknown
            tileTypeDefs[197] = Constants.MoreTerra_Color.FROZEN_SLIME; // Unknown
            tileTypeDefs[198] = Constants.MoreTerra_Color.ASPHALT; // Unknown
            tileTypeDefs[199] = Constants.MoreTerra_Color.FLESH_GRASS; // Unknown
            tileTypeDefs[200] = Constants.MoreTerra_Color.RED_ICE; // Unknown
            tileTypeDefs[201] = Constants.MoreTerra_Color.FLESH_WEEDS; // Unknown
            tileTypeDefs[202] = Constants.MoreTerra_Color.SUNPLATE; // Unknown
            tileTypeDefs[203] = Constants.MoreTerra_Color.CRIMSTONE; // Unknown
            tileTypeDefs[204] = Constants.MoreTerra_Color.CRIMTANE_ORE; // Unknown
            tileTypeDefs[205] = Constants.MoreTerra_Color.CRIMSTONE_VINES; // Unknown
            tileTypeDefs[206] = Constants.MoreTerra_Color.ICE_BRICK; // Unknown
            tileTypeDefs[207] = Constants.MoreTerra_Color.WATER_FOUNTAIN; // Unknown
            tileTypeDefs[208] = Constants.MoreTerra_Color.SHADEWOOD; // Unknown
            tileTypeDefs[209] = Constants.MoreTerra_Color.CANNON; // Unknown
            tileTypeDefs[210] = Constants.MoreTerra_Color.LAND_MINE; // Unknown
            tileTypeDefs[211] = Constants.MoreTerra_Color.CHLOROPHYTE_ORE; // Unknown
            tileTypeDefs[212] = Constants.MoreTerra_Color.SNOWBALL_LAUNCHER; // Unknown
            tileTypeDefs[213] = Constants.MoreTerra_Color.ROPE; // Unknown
            tileTypeDefs[214] = Constants.MoreTerra_Color.CHAIN; // Unknown
            tileTypeDefs[215] = Constants.MoreTerra_Color.CAMPFIRE; // Unknown
            tileTypeDefs[216] = Constants.MoreTerra_Color.ROCKET; // Unknown
            tileTypeDefs[217] = Constants.MoreTerra_Color.BLEND_O_MATIC; // Unknown
            tileTypeDefs[218] = Constants.MoreTerra_Color.MEAT_GRINDER; // Unknown
            tileTypeDefs[219] = Constants.MoreTerra_Color.SILT_EXTRACTINATOR; // Unknown
            tileTypeDefs[220] = Constants.MoreTerra_Color.SOLIDIFIER; // Unknown
            tileTypeDefs[221] = Constants.MoreTerra_Color.PALLADIUM_ORE; // Unknown
            tileTypeDefs[222] = Constants.MoreTerra_Color.ORICHALCUM_ORE; // Unknown
            tileTypeDefs[223] = Constants.MoreTerra_Color.TITANIUM_ORE; // Unknown
            tileTypeDefs[224] = Constants.MoreTerra_Color.SLUSH; // Unknown
            tileTypeDefs[225] = Constants.MoreTerra_Color.HIVE; // Unknown
            tileTypeDefs[226] = Constants.MoreTerra_Color.LIHZAHRD_BRICK; // Unknown
            tileTypeDefs[227] = Constants.MoreTerra_Color.DYE_PLANT; // Unknown
            tileTypeDefs[228] = Constants.MoreTerra_Color.DYE_VAT; // Unknown
            tileTypeDefs[229] = Constants.MoreTerra_Color.HONEY_BLOCK; // Unknown
            tileTypeDefs[230] = Constants.MoreTerra_Color.CRIPSY_HONEY_BLOCK; // Unknown
            tileTypeDefs[231] = Constants.MoreTerra_Color.LARVA; // Unknown
            tileTypeDefs[232] = Constants.MoreTerra_Color.WOODEN_SPIKE; // Unknown
            tileTypeDefs[233] = Constants.MoreTerra_Color.PLANTS4; // Unknown
            tileTypeDefs[234] = Constants.MoreTerra_Color.CRIMSAND; // Unknown
            tileTypeDefs[235] = Constants.MoreTerra_Color.TELEPORTER; // Unknown
            tileTypeDefs[236] = Constants.MoreTerra_Color.LIFE_FRUIT; // Unknown
            tileTypeDefs[237] = Constants.MoreTerra_Color.LIHZAHRD_ALTAR; // Unknown
            tileTypeDefs[238] = Constants.MoreTerra_Color.PLANTERAS_BULB; // Unknown
            tileTypeDefs[239] = Constants.MoreTerra_Color.METAL_BAR; // Unknown
            tileTypeDefs[240] = Constants.MoreTerra_Color.PICTURE_3X3; // Unknown
            tileTypeDefs[241] = Constants.MoreTerra_Color.PICTURE_CATACOMB; // Unknown
            tileTypeDefs[242] = Constants.MoreTerra_Color.PICTURE_6X4; // Unknown
            tileTypeDefs[243] = Constants.MoreTerra_Color.IMBUING_STATION; // Unknown
            tileTypeDefs[244] = Constants.MoreTerra_Color.BUBBLE_MACHINE; // Unknown
            tileTypeDefs[245] = Constants.MoreTerra_Color.PICTURE_2X3; // Unknown
            tileTypeDefs[246] = Constants.MoreTerra_Color.PICTURE_3X2; // Unknown
            tileTypeDefs[247] = Constants.MoreTerra_Color.AUTOHAMMER; // Unknown
            tileTypeDefs[248] = Constants.MoreTerra_Color.PALLADIUM_COLUMN; // Unknown
            tileTypeDefs[249] = Constants.MoreTerra_Color.BUBBLEGUM_BLOCK; // Unknown
            tileTypeDefs[250] = Constants.MoreTerra_Color.TITANSTONE; // Unknown
            tileTypeDefs[251] = Constants.MoreTerra_Color.PUMPKIN; // Unknown
            tileTypeDefs[252] = Constants.MoreTerra_Color.HAY; // Unknown
            tileTypeDefs[253] = Constants.MoreTerra_Color.SPOOKY_WOOD; // Unknown
            tileTypeDefs[254] = Constants.MoreTerra_Color.PUMPKINS; // Unknown
            tileTypeDefs[255] = Constants.MoreTerra_Color.AMETHYST_GEMSPARK_OFF; // Unknown
            tileTypeDefs[256] = Constants.MoreTerra_Color.TOPAZ_GEMSPARK_OFF; // Unknown
            tileTypeDefs[257] = Constants.MoreTerra_Color.SAPPHIRE_GEMSPARK_OFF; // Unknown
            tileTypeDefs[258] = Constants.MoreTerra_Color.EMERALD_GEMSPARK_OFF_WOOD; // Unknown
            tileTypeDefs[259] = Constants.MoreTerra_Color.RUBY_GEMSPARK_OFF; // Unknown
            tileTypeDefs[260] = Constants.MoreTerra_Color.DIAMOND_GEMSPARK_OFF; // Unknown
            tileTypeDefs[261] = Constants.MoreTerra_Color.AMBER_GEMSPARK_OFF; // Unknown
            tileTypeDefs[262] = Constants.MoreTerra_Color.WOMANNEQUIN; // Unknown
            tileTypeDefs[263] = Constants.MoreTerra_Color.TOPAZ_GEMSPARK_OFF_2; // Unknown
            tileTypeDefs[264] = Constants.MoreTerra_Color.SAPPHIRE_GEMSPARK_OFF_2; // Unknown
            tileTypeDefs[265] = Constants.MoreTerra_Color.EMERALD_GEMSPARK_OFF_WOOD_2; // Unknown
            tileTypeDefs[266] = Constants.MoreTerra_Color.RUBY_GEMSPARK_OFF_2; // Unknown
            tileTypeDefs[267] = Constants.MoreTerra_Color.DIAMOND_GEMSPARK_OFF_2; // Unknown
            tileTypeDefs[268] = Constants.MoreTerra_Color.AMBER_GEMSPARK_OFF_2; // Unknown

            for (int i = 269; i < 330; i++)
            {
                tileTypeDefs[i] = System.Drawing.Color.Magenta;
            }

            tileTypeDefs[330] = Constants.MoreTerra_Color.COPPER_CACHE; // Unknown
            tileTypeDefs[331] = Constants.MoreTerra_Color.SILVER_CACHE; // Unknown
            tileTypeDefs[332] = Constants.MoreTerra_Color.GOLD_CACHE; // Unknown
            tileTypeDefs[333] = Constants.MoreTerra_Color.ENCHANTED_SWORD; // Unknown

            for (int i = 334; i < 448; i++)
            {
                tileTypeDefs[i] = System.Drawing.Color.Magenta;
            }

            tileTypeDefs[448] = Constants.MoreTerra_Color.SKY;
            tileTypeDefs[449] = Constants.MoreTerra_Color.WATER;
            tileTypeDefs[450] = Constants.MoreTerra_Color.LAVA;

            // Walls
            tileTypeDefs[451] = Constants.MoreTerra_Color.STONE_WALL; // #424242
            tileTypeDefs[452] = Constants.MoreTerra_Color.DIRT_WALL_UNSAFE; // Wall Dirt
            tileTypeDefs[453] = Constants.MoreTerra_Color.EBONSTONE_WALL_UNSAFE; // #3D3A4E
            tileTypeDefs[454] = Constants.MoreTerra_Color.WOOD_WALL; // Wall Wood
            tileTypeDefs[455] = Constants.MoreTerra_Color.GREY_BRICK_WALL; // Wall Brick
            tileTypeDefs[456] = Constants.MoreTerra_Color.RED_BRICK_WALL; // Wall Brick
            tileTypeDefs[457] = Constants.MoreTerra_Color.BLUE_BRICK_WALL_UNSAFE; // Wall Dungeon Blue
            tileTypeDefs[458] = Constants.MoreTerra_Color.GREEN_BRICK_WALL_UNSAFE; // Wall Dungeon Green
            tileTypeDefs[459] = Constants.MoreTerra_Color.PINK_BRICK_WALL_UNSAFE; // Wall Dungeon Pink
            tileTypeDefs[460] = Constants.MoreTerra_Color.GOLD_BRICK_WALL; // Wall Brick
            tileTypeDefs[461] = Constants.MoreTerra_Color.SILVER_BRICK_WALL; // Wall Brick
            tileTypeDefs[462] = Constants.MoreTerra_Color.COPPER_BRICK_WALL; // Wall Brick
            tileTypeDefs[463] = Constants.MoreTerra_Color.HELLSTONE_BRICK_WALL_UNSAFE; // #301515
            tileTypeDefs[464] = Constants.MoreTerra_Color.OBSIDIAN_BRICK_WALL; // #5751AD
            tileTypeDefs[465] = Constants.MoreTerra_Color.MUD_WALL_UNSAFE; // #3D2E31
            tileTypeDefs[466] = Constants.MoreTerra_Color.DIRT_WALL; // Wall Dirt
            tileTypeDefs[467] = Constants.MoreTerra_Color.BLUE_BRICK_WALL; // Wall Dungeon Blue
            tileTypeDefs[468] = Constants.MoreTerra_Color.GREEN_BRICK_WALL; // Wall Dungeon Green
            tileTypeDefs[469] = Constants.MoreTerra_Color.PINK_BRICK_WALL; // Wall Dungeon Pink
            tileTypeDefs[470] = Constants.MoreTerra_Color.OBSIDIAN_BRICK_WALL_2; // Wall Dirt
            tileTypeDefs[471] = Constants.MoreTerra_Color.GLASS_WALL; // Wall Brick
            tileTypeDefs[472] = Constants.MoreTerra_Color.PEARLSTONE_BRICK_WALL; // Wall Brick
            tileTypeDefs[473] = Constants.MoreTerra_Color.IRIDESCENT_BRICK_WALL; // Wall Brick
            tileTypeDefs[474] = Constants.MoreTerra_Color.MUDSTONE_BRICK_WALL; // Wall Brick
            tileTypeDefs[475] = Constants.MoreTerra_Color.COBALT_BRICK_WALL; // Wall Brick
            tileTypeDefs[476] = Constants.MoreTerra_Color.MYTHRIL_BRICK_WALL; // Wall Brick
            tileTypeDefs[477] = Constants.MoreTerra_Color.PLANKED_WALL; // Wall Wood
            tileTypeDefs[478] = Constants.MoreTerra_Color.PEARLSTONE_BRICK_WALL_UNSAFE; // Wall Brick
            tileTypeDefs[479] = Constants.MoreTerra_Color.CANDY_CANE_WALL; // Wall Brick
            tileTypeDefs[480] = Constants.MoreTerra_Color.GREEN_CANDY_CANE_WALL; // Wall Brick
            tileTypeDefs[481] = Constants.MoreTerra_Color.SNOW_BRICK_WALL; // Wall Brick
            tileTypeDefs[482] = Constants.MoreTerra_Color.ADAMANTITE_BEAM_WALL; // Unknown
            tileTypeDefs[483] = Constants.MoreTerra_Color.DEMONITE_BRICK_WALL; // Unknown
            tileTypeDefs[484] = Constants.MoreTerra_Color.SANDSTONE_BRICK_WALL; // Unknown
            tileTypeDefs[485] = Constants.MoreTerra_Color.EBONSTONE_BRICK_WALL; // Unknown
            tileTypeDefs[486] = Constants.MoreTerra_Color.RED_STUCCO_WALL; // Unknown
            tileTypeDefs[487] = Constants.MoreTerra_Color.YELLOW_STUCCO_WALL; // Unknown
            tileTypeDefs[488] = Constants.MoreTerra_Color.GREEN_STUCCO_WALL; // Unknown
            tileTypeDefs[489] = Constants.MoreTerra_Color.GRAY_WALL; // Unknown
            tileTypeDefs[490] = Constants.MoreTerra_Color.SNOW_WALL_UNSAFE; // Unknown
            tileTypeDefs[491] = Constants.MoreTerra_Color.EBONWOOD_WALL; // Unknown
            tileTypeDefs[492] = Constants.MoreTerra_Color.RICH_MAHOGANY_WALL; // Unknown
            tileTypeDefs[493] = Constants.MoreTerra_Color.PEARLWOOD_WALL; // Unknown
            tileTypeDefs[494] = Constants.MoreTerra_Color.RAINBOW_BRICK_WALL; // Unknown
            tileTypeDefs[495] = Constants.MoreTerra_Color.TIN_BRICK_WALL; // Unknown
            tileTypeDefs[496] = Constants.MoreTerra_Color.TUNGSTEN_BRICK_WALL; // Unknown
            tileTypeDefs[497] = Constants.MoreTerra_Color.PLATINUM_BRICK_WALL; // Unknown
            tileTypeDefs[498] = Constants.MoreTerra_Color.AMETHYST_WALL_UNSAFE; // Unknown
            tileTypeDefs[499] = Constants.MoreTerra_Color.TOPAZ_WALL_UNSAFE; // Unknown
            tileTypeDefs[500] = Constants.MoreTerra_Color.SAPPHIRE_WALL_UNSAFE; // Unknown
            tileTypeDefs[501] = Constants.MoreTerra_Color.EMERALD_WALL_UNSAFE; // Unknown
            tileTypeDefs[502] = Constants.MoreTerra_Color.RUBY_WALL_UNSAFE; // Unknown
            tileTypeDefs[503] = Constants.MoreTerra_Color.DIAMOND_WALL_UNSAFE; // Unknown
            tileTypeDefs[504] = Constants.MoreTerra_Color.UNIQUE_CAVE_WALL_1_UNSAFE; // Unknown
            tileTypeDefs[505] = Constants.MoreTerra_Color.UNIQUE_CAVE_WALL_2_UNSAFE; // Unknown
            tileTypeDefs[506] = Constants.MoreTerra_Color.UNIQUE_CAVE_WALL_3_UNSAFE; // Unknown
            tileTypeDefs[507] = Constants.MoreTerra_Color.UNIQUE_CAVE_WALL_4_UNSAFE; // Unknown
            tileTypeDefs[508] = Constants.MoreTerra_Color.UNIQUE_CAVE_WALL_5_UNSAFE; // Unknown
            tileTypeDefs[509] = Constants.MoreTerra_Color.CAVE_WALL_UNSAFE; // Unknown
            tileTypeDefs[510] = Constants.MoreTerra_Color.LEAVES_WALL_UNSAFE; // Unknown
            tileTypeDefs[511] = Constants.MoreTerra_Color.UNIQUE_CAVE_WALL_6_UNSAFE; // Unknown
            tileTypeDefs[512] = Constants.MoreTerra_Color.SPIDER_WALL_UNSAFE; // Unknown
            tileTypeDefs[513] = Constants.MoreTerra_Color.GRASS_WALL_UNSAFE; // Unknown
            tileTypeDefs[514] = Constants.MoreTerra_Color.JUNGLE_WALL_UNSAFE; // Unknown
            tileTypeDefs[515] = Constants.MoreTerra_Color.FLOWER_WALL_UNSAFE; // Unknown
            tileTypeDefs[516] = Constants.MoreTerra_Color.GRASS_WALL; // Unknown
            tileTypeDefs[517] = Constants.MoreTerra_Color.JUNGLE_WALL; // Unknown
            tileTypeDefs[518] = Constants.MoreTerra_Color.FLOWER_WALL; // Unknown
            tileTypeDefs[519] = Constants.MoreTerra_Color.CORRUPT_GRASS_WALL_UNSAFE; // Unknown
            tileTypeDefs[520] = Constants.MoreTerra_Color.HALLOWED_GRASS_WALL_UNSAFE; // Unknown
            tileTypeDefs[521] = Constants.MoreTerra_Color.ICE_WALL_UNSAFE; // Unknown
            tileTypeDefs[522] = Constants.MoreTerra_Color.CACTUS_WALL; // Unknown
            tileTypeDefs[523] = Constants.MoreTerra_Color.CLOUD_WALL; // Unknown
            tileTypeDefs[524] = Constants.MoreTerra_Color.MUSHROOM_WALL; // Unknown
            tileTypeDefs[525] = Constants.MoreTerra_Color.BONE_BLOCK_WALL; // Unknown
            tileTypeDefs[526] = Constants.MoreTerra_Color.SLIME_BLOCK_WALL; // Unknown
            tileTypeDefs[527] = Constants.MoreTerra_Color.FLESH_BLOCK_WALL; // Unknown
            tileTypeDefs[528] = Constants.MoreTerra_Color.LIVING_WOOD_WALL; // Unknown
            tileTypeDefs[529] = Constants.MoreTerra_Color.OBSIDIAN_BACK_WALL_UNSAFE; // Unknown
            tileTypeDefs[530] = Constants.MoreTerra_Color.MUSHROOM_WALL_UNSAFE; // Unknown
            tileTypeDefs[531] = Constants.MoreTerra_Color.CRIMGRASS_WALL_UNSAFE; // Unknown
            tileTypeDefs[532] = Constants.MoreTerra_Color.DISC_WALL; // Unknown
            tileTypeDefs[533] = Constants.MoreTerra_Color.CRIMSTONE_WALL_UNSAFE; // Unknown
            tileTypeDefs[534] = Constants.MoreTerra_Color.ICE_BRICK_WALL; // Unknown
            tileTypeDefs[535] = Constants.MoreTerra_Color.SHADEWOOD_WALL; // Unknown
            tileTypeDefs[536] = Constants.MoreTerra_Color.HIVE_WALL_UNSAFE; // Unknown
            tileTypeDefs[537] = Constants.MoreTerra_Color.LIHZAHRD_BRICK_WALL_UNSAFE; // Unknown
            tileTypeDefs[538] = Constants.MoreTerra_Color.PURPLE_STAINED_GLASS; // Unknown
            tileTypeDefs[539] = Constants.MoreTerra_Color.YELLOW_STAINED_GLASS; // Unknown
            tileTypeDefs[540] = Constants.MoreTerra_Color.BLUE_STAINED_GLASS; // Unknown
            tileTypeDefs[541] = Constants.MoreTerra_Color.GREEN_STAINED_GLASS; // Unknown
            tileTypeDefs[542] = Constants.MoreTerra_Color.RED_STAINED_GLASS; // Unknown
            tileTypeDefs[543] = Constants.MoreTerra_Color.MULTICOLORED_STAINED_GLASS; // Unknown
            tileTypeDefs[544] = Constants.MoreTerra_Color.BLUE_SLAB_WALL_UNSAFE; // Unknown
            tileTypeDefs[545] = Constants.MoreTerra_Color.BLUE_TILED_WALL_UNSAFE; // Unknown
            tileTypeDefs[546] = Constants.MoreTerra_Color.PINK_SLAB_WALL_UNSAFE; // Unknown
            tileTypeDefs[547] = Constants.MoreTerra_Color.PINK_TILED_WALL_UNSAFE; // Unknown
            tileTypeDefs[548] = Constants.MoreTerra_Color.GREEN_SLAB_WALL_UNSAFE; // Unknown
            tileTypeDefs[549] = Constants.MoreTerra_Color.GREEN_TILED_WALL_UNSAFE; // Unknown
            tileTypeDefs[550] = Constants.MoreTerra_Color.BLUE_SLAB_WALL; // Unknown
            tileTypeDefs[551] = Constants.MoreTerra_Color.BLUE_TILED_WALL; // Unknown
            tileTypeDefs[552] = Constants.MoreTerra_Color.PINK_SLAB_WALL; // Unknown
            tileTypeDefs[553] = Constants.MoreTerra_Color.PINK_TILED_WALL; // Unknown
            tileTypeDefs[554] = Constants.MoreTerra_Color.GREEN_SLAB_WALL; // Unknown
            tileTypeDefs[555] = Constants.MoreTerra_Color.GREEN_TILED_WALL; // Unknown
            tileTypeDefs[556] = Constants.MoreTerra_Color.WOODEN_FENCE; // Unknown
            tileTypeDefs[557] = Constants.MoreTerra_Color.METAL_FENCE; // Unknown
            tileTypeDefs[558] = Constants.MoreTerra_Color.HIVE_WALL; // Unknown
            tileTypeDefs[559] = Constants.MoreTerra_Color.PALLADIUM_COLUMN_WALL; // Unknown
            tileTypeDefs[560] = Constants.MoreTerra_Color.BUBBLEGUM_BLOCK_WALL; // Unknown
            tileTypeDefs[561] = Constants.MoreTerra_Color.TITANSTONE_BLOCK_WALL; // Unknown
            tileTypeDefs[562] = Constants.MoreTerra_Color.LIHZAHRD_BRICK_WALL; // Unknown
            tileTypeDefs[563] = Constants.MoreTerra_Color.PUMPKIN_WALL; // Unknown
            tileTypeDefs[564] = Constants.MoreTerra_Color.HAY_WALL; // Unknown
            tileTypeDefs[565] = Constants.MoreTerra_Color.SPOOKY_WOOD_WALL; // Unknown
            tileTypeDefs[566] = Constants.MoreTerra_Color.CHRISTMAS_TREE_WALLPAPER; // Unknown
            tileTypeDefs[567] = Constants.MoreTerra_Color.ORNAMENT_WALLPAPER; // Unknown
            tileTypeDefs[568] = Constants.MoreTerra_Color.CANDY_CANE_WALLPAPER; // Unknown
            tileTypeDefs[569] = Constants.MoreTerra_Color.FESTIVE_WALLPAPER; // Unknown
            tileTypeDefs[570] = Constants.MoreTerra_Color.STARS_WALLPAPER; // Unknown
            tileTypeDefs[571] = Constants.MoreTerra_Color.SQUIGGLES_WALLPAPER; // Unknown
            tileTypeDefs[572] = Constants.MoreTerra_Color.SNOWFLAKE_WALLPAPER; // Unknown
            tileTypeDefs[573] = Constants.MoreTerra_Color.KRAMPUS_HORN_WALLPAPER; // Unknown
            tileTypeDefs[574] = Constants.MoreTerra_Color.BLUEGREEN_WALLPAPER; // Unknown
            tileTypeDefs[575] = Constants.MoreTerra_Color.GRINCH_FINGER_WALLPAPER; // Unknown
            tileTypeDefs[576] = Constants.MoreTerra_Color.FANCY_GREY_WALLPAPER; // Unknown
            tileTypeDefs[577] = Constants.MoreTerra_Color.ICE_FLOE_WALLPAPER; // Unknown
            tileTypeDefs[578] = Constants.MoreTerra_Color.MUSIC_WALLPAPER; // Unknown
            tileTypeDefs[579] = Constants.MoreTerra_Color.PURPLERAIN_WALLPAPER; // Unknown
            tileTypeDefs[580] = Constants.MoreTerra_Color.RAINBOW_WALLPAPER; // Unknown
            tileTypeDefs[581] = Constants.MoreTerra_Color.SPARKLE_STONE_WALLPAPER; // Unknown
            tileTypeDefs[582] = Constants.MoreTerra_Color.STARLIT_HEAVEN_WALLPAPER; // Unknown
            tileTypeDefs[583] = Constants.MoreTerra_Color.BUBBLE_WALLPAPER; // Unknown
            tileTypeDefs[584] = Constants.MoreTerra_Color.COPPERPIPE_WALLPAPER; // Unknown
            tileTypeDefs[585] = Constants.MoreTerra_Color.DUCKY_WALLPAPER; // Unknown
            tileTypeDefs[586] = Constants.MoreTerra_Color.WATERFALLR; // Unknown
            tileTypeDefs[587] = Constants.MoreTerra_Color.LAVAFALL; // Unknown
            tileTypeDefs[588] = Constants.MoreTerra_Color.EBONWOOD_FENCE; // Unknown
            tileTypeDefs[589] = Constants.MoreTerra_Color.RICHMAHOGANY_FENCE; // Unknown
            tileTypeDefs[590] = Constants.MoreTerra_Color.PEARLWOOD_FENCE; // Unknown
            tileTypeDefs[591] = Constants.MoreTerra_Color.SHADEWOOD_FENCE; // Unknown
            tileTypeDefs[592] = Constants.MoreTerra_Color.WHITE_DYNASTY; // Unknown
            tileTypeDefs[593] = Constants.MoreTerra_Color.BLUE_DYNASTY; // Unknown
            tileTypeDefs[594] = Constants.MoreTerra_Color.ARCANE_RUNES; // Unknown
            tileTypeDefs[595] = Constants.MoreTerra_Color.IRON_FENCE; // Unknown
            tileTypeDefs[596] = Constants.MoreTerra_Color.COPPER_PLATING; // Unknown
            tileTypeDefs[597] = Constants.MoreTerra_Color.STONE_SLAB; // Unknown
            tileTypeDefs[598] = Constants.MoreTerra_Color.SAIL; // Unknown
            tileTypeDefs[599] = Constants.MoreTerra_Color.BOREAL_WOOD; // Unknown
            tileTypeDefs[600] = Constants.MoreTerra_Color.BOREAL_WOOD_FENCE; // Unknown
            tileTypeDefs[601] = Constants.MoreTerra_Color.PALM_WOOD; // Unknown
            tileTypeDefs[602] = Constants.MoreTerra_Color.PALM_WOOD_FENCE; // Unknown
            tileTypeDefs[603] = Constants.MoreTerra_Color.AMBER_GEMSPARK; // Unknown
            tileTypeDefs[604] = Constants.MoreTerra_Color.AMETHYST_GEMSPARK; // Unknown
            tileTypeDefs[605] = Constants.MoreTerra_Color.DIAMOND_GEMSPARK; // Unknown
            tileTypeDefs[606] = Constants.MoreTerra_Color.EMERALD_GEMSPARK; // Unknown
            tileTypeDefs[607] = Constants.MoreTerra_Color.AMBER_GEMSPARK_OFF_WALL; // Unknown
            tileTypeDefs[608] = Constants.MoreTerra_Color.AMETHYST_GEMSPARK_OFF_WALL; // Unknown
            tileTypeDefs[609] = Constants.MoreTerra_Color.DIAMOND_GEMSPARK_OFF_WALL; // Unknown
            tileTypeDefs[610] = Constants.MoreTerra_Color.EMERALD_GEMSPARK_OFF; // Unknown
            tileTypeDefs[611] = Constants.MoreTerra_Color.RUBY_GEMSPARK_OFF_WALL; // Unknown
            tileTypeDefs[612] = Constants.MoreTerra_Color.SAPPHIRE_GEMSPARK_OFF_WALL; // Unknown
            tileTypeDefs[613] = Constants.MoreTerra_Color.TOPAZ_GEMSPARK_OFF_WALL; // Unknown
            tileTypeDefs[614] = Constants.MoreTerra_Color.RUBY_GEMSPARK; // Unknown
            tileTypeDefs[615] = Constants.MoreTerra_Color.SAPPHIRE_GEMSPARK; // Unknown
            tileTypeDefs[616] = Constants.MoreTerra_Color.TOPAZ_GEMSPARK; // Unknown
            tileTypeDefs[617] = Constants.MoreTerra_Color.TIN_PLATING; // Unknown
            tileTypeDefs[618] = Constants.MoreTerra_Color.CONFETTI; // Unknown
            tileTypeDefs[619] = Constants.MoreTerra_Color.CONFETTI_BLACK; // Unknown
            tileTypeDefs[620] = Constants.MoreTerra_Color.CAVE_WALL; // Unknown
            tileTypeDefs[621] = Constants.MoreTerra_Color.CAVE_WALL2; // Unknown
            tileTypeDefs[622] = Constants.MoreTerra_Color.COUNT; // Unknown
        }
    }
}