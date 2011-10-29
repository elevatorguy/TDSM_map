using System.Drawing;
using System.Collections.Generic;
using Terraria_Server.Logging;
using Terraria_Server;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System;
using System.Threading;

namespace MapPlugin
{
    public partial class MapPlugin
    {
        public static Dictionary<int, Color> ColorDefs;
        public static Dictionary<int, UInt32> UInt32Defs;
        public static Dictionary<int, Color> DimColorDefs;
        public static Dictionary<int, UInt32> DimUInt32Defs;

        public static Bitmap bmp;
        public static Dictionary<UInt32, Color> waterblendlist = new Dictionary<UInt32, Color>();
        public static Dictionary<UInt32, Color> lavablendlist = new Dictionary<UInt32, Color>();
        //better to have a separate list for dim liquid lists
        public static Dictionary<UInt32, Color> waterdimlist = new Dictionary<UInt32, Color>();
        public static Dictionary<UInt32, Color> lavadimlist = new Dictionary<UInt32, Color>();

        public void mapWorld2()
        {
            Stopwatch stopwatch = new Stopwatch();
            Server.notifyOps("Saving Image...", true);
            stopwatch.Start();
            bmp = new Bitmap(Main.maxTilesX/4, Main.maxTilesY, PixelFormat.Format32bppArgb);
            Graphics graphicsHandle = Graphics.FromImage((Image)bmp);
            //draw background
            if (highlight)
            {
                Color SKY = dimC(0x84AAF8);
                Color EARTH = dimC(0x583D2E);
                Color HELL = dimC(0x000000);
                graphicsHandle.FillRectangle(new SolidBrush(SKY), 0, 0, bmp.Width, (float)Main.worldSurface);
                graphicsHandle.FillRectangle(new SolidBrush(EARTH), 0, (float)Main.worldSurface, bmp.Width, (float)Main.rockLayer);
                graphicsHandle.FillRectangle(new SolidBrush(HELL), 0, (float)Main.rockLayer, bmp.Width, (float)Main.maxTilesY);
                //this fades the background from rock to hell
                Color dimColor;
                for (int y = (int)Main.rockLayer; y < Main.maxTilesY; y++)
                {
                    dimColor = dimC(UInt32Defs[331 + y]);
                    graphicsHandle.DrawLine(new Pen(dimColor), 0, y, bmp.Width, y);
                }
            }
            else
            {
                graphicsHandle.FillRectangle(new SolidBrush(Constants.Terrafirma_Color.SKY), 0, 0, bmp.Width, (float)Main.worldSurface);
                graphicsHandle.FillRectangle(new SolidBrush(Constants.Terrafirma_Color.EARTH), 0, (float)Main.worldSurface, bmp.Width, (float)Main.rockLayer);
                graphicsHandle.FillRectangle(new SolidBrush(Constants.Terrafirma_Color.HELL), 0, (float)Main.rockLayer, bmp.Width, (float)Main.maxTilesY);
                //this fades the background from rock to hell
                for (int y = (int)Main.rockLayer; y < Main.maxTilesY; y++)
                {
                    graphicsHandle.DrawLine(new Pen(ColorDefs[331 + y]), 0, y, bmp.Width, y);
                }
            }

            piece1 = (Bitmap)bmp.Clone();
            piece2 = (Bitmap)bmp.Clone();
            piece3 = (Bitmap)bmp.Clone();
            piece4 = (Bitmap)bmp.Clone();

            // splits the work into four threads
            Thread part1 = new Thread(mapthread1);
            Thread part2 = new Thread(mapthread2);
            Thread part3 = new Thread(mapthread3);
            Thread part4 = new Thread(mapthread4);
            part1.Start();
            part2.Start();
            part3.Start();
            part4.Start();

            while (part1.IsAlive || part2.IsAlive || part3.IsAlive || part4.IsAlive) ;

            bmp = new Bitmap(Main.maxTilesX, Main.maxTilesY, PixelFormat.Format32bppArgb);
            int quarter = (Main.maxTilesX / 4);
            using (Graphics gfx = Graphics.FromImage(bmp))
            {
                gfx.DrawImage(piece1, new Point(0, 0));
                gfx.DrawImage(piece2, new Point(quarter, 0));
                gfx.DrawImage(piece3, new Point(2 * quarter, 0));
                gfx.DrawImage(piece4, new Point(3 * quarter, 0));
            }            

            Server.notifyOps("Saving Data...", true);
            bmp.Save(string.Concat(p, Path.DirectorySeparatorChar, filename));
            stopwatch.Stop();
            ProgramLog.Log("Save duration: " + stopwatch.Elapsed.Seconds + " Second(s)");
            Server.notifyOps("Saving Complete.", true);
            bmp = null;
        }

        private static Bitmap piece1;
        private static Bitmap piece2;
        private static Bitmap piece3;
        private static Bitmap piece4;

        private void mapthread1()
        {
            int quarter = (Main.maxTilesX / 4);
            using (var prog = new ProgressLogger(quarter-1, "Saving image data"))
            for (int i = 0; i < quarter; i++)
            {
                prog.Value = i; // each thread finished about the same time so I put the progress logger on one of them
                maprenderloop(i, piece1, 0);
            }
        }

        private void mapthread2()
        {
            int quarter = (Main.maxTilesX / 4);
            for (int i = 0; i < quarter; i++)
            {
                maprenderloop(i + quarter, piece2, quarter);
            }
        }

        private void mapthread3()
        {
            int quarter = (Main.maxTilesX / 4);
            for (int i = 0; i < quarter; i++)
            {
                maprenderloop(i + 2 * quarter, piece3, 2 * quarter);
            }
        }

        private void mapthread4()
        {
            int quarter = (Main.maxTilesX / 4);
            for (int i = 0; i < quarter; i++)
            {
                maprenderloop(i + 3 * quarter, piece4, 3 * quarter);
            }
        }

        private void maprenderloop(int i, Bitmap bmp, int piece)
        {
            UInt32 tempColor;
            List<int> list;
            for (int j = 0; j < Main.maxTilesY; j++)
            {
                if (highlight) //dim the world
                {
                    //draws tiles or walls
                    if (Main.tile.At(i, j).Wall == 0)
                    {
                        if (Main.tile.At(i, j).Active)
                        {
                            bmp.SetPixel(i-piece, j, DimColorDefs[Main.tile.At(i, j).Type]);
                            tempColor = DimUInt32Defs[Main.tile.At(i, j).Type];
                        }
                        else
                        {
                            tempColor = DimUInt32Defs[j + 331];
                        }
                    }
                    else
                    {
                        //priority to tiles
                        if (Main.tile.At(i, j).Active)
                        {
                            bmp.SetPixel(i - piece, j, DimColorDefs[Main.tile.At(i, j).Type]);
                            tempColor = DimUInt32Defs[Main.tile.At(i, j).Type];
                        }
                        else
                        {
                            bmp.SetPixel(i - piece, j, DimColorDefs[Main.tile.At(i, j).Wall + 267]);
                            tempColor = DimUInt32Defs[Main.tile.At(i, j).Wall + 267];
                        }
                    }
                    // lookup blendcolor of color just drawn, and draw again
                    if (Main.tile.At(i, j).Liquid > 0)
                    {
                        if (lavadimlist.ContainsKey(tempColor))
                        {  // incase the map has hacked data
                            bmp.SetPixel(i - piece, j, Main.tile.At(i, j).Lava ? lavadimlist[tempColor] : waterdimlist[tempColor]);
                        }
                    }

                    list = getGiveID(Main.tile.At(i, j).Type, (Main.tile.At(i, j).Wall));
                    //highlight the tiles of supplied type from the map command
                    if (list.Contains(highlightID))
                    {
                        bmp.SetPixel(i - piece, j, Color.White);
                    }
                }
                else
                {
                    //draws tiles or walls
                    if (Main.tile.At(i, j).Wall == 0)
                    {
                        if (Main.tile.At(i, j).Active)
                        {
                            bmp.SetPixel(i - piece, j, ColorDefs[Main.tile.At(i, j).Type]);
                            tempColor = UInt32Defs[Main.tile.At(i, j).Type];
                        }
                        else
                        {
                            tempColor = UInt32Defs[j + 331];
                        }
                    }
                    else
                    {
                        //priority to tiles
                        if (Main.tile.At(i, j).Active)
                        {
                            bmp.SetPixel(i - piece, j, ColorDefs[Main.tile.At(i, j).Type]);
                            tempColor = UInt32Defs[Main.tile.At(i, j).Type];
                        }
                        else
                        {
                            bmp.SetPixel(i - piece, j, ColorDefs[Main.tile.At(i, j).Wall + 267]);
                            tempColor = UInt32Defs[Main.tile.At(i, j).Wall + 267];
                        }
                    }
                    // lookup blendcolor of color just drawn, and draw again
                    if (Main.tile.At(i, j).Liquid > 0)
                    {
                        if (lavablendlist.ContainsKey(tempColor))
                        {  // incase the map has hacked data
                            bmp.SetPixel(i - piece, j, Main.tile.At(i, j).Lava ? lavablendlist[tempColor] : waterblendlist[tempColor]);
                        }
                    }
                }
            }

        }

        //pre-blends colors when loading plugin so making the image is faster
        public void initBList()
        {
            //adds all the colors for walls/tiles/global
            UInt32 waterColor = 0x093DBF;
            UInt32 lavaColor = 0xFD2003;
            //blends water and lava with UInt32Defs
            using (var blendprog = new ProgressLogger(Main.maxTilesX - 1, "[map] Blending colors"))
                for (int y = 0; y <= Main.maxTilesY + 331; y++)
                {
                    if (UInt32Defs.ContainsKey(y))
                    {
                        UInt32 c = UInt32Defs[y];

                        //initialize DimColorDefs and DimUInt32Defs first
                        UInt32 dimblend = dimI(c);
                        Color dimresult = dimC(c);
                        DimUInt32Defs[y] = dimblend;
                        DimColorDefs[y] = dimresult;

                        UInt32 d = DimUInt32Defs[y];
                        blendprog.Value = y;

                        doBlendResult(c, waterColor, lavaColor, "regular");
                        doBlendResult(d, waterColor, lavaColor, "dim");
                    }
                }
        }

        private void doBlendResult(UInt32 c, UInt32 waterColor, UInt32 lavaColor, string type)
        {
            if (type == "regular" && !(lavablendlist.ContainsKey(c)))
            {
                Color waterblendresult = toColor(alphaBlend(c, waterColor, 0.5));

                waterblendlist.Add(c, waterblendresult);
                lavablendlist.Add(c, toColor(alphaBlend(c, lavaColor, 0.5)));
            }
            if (type == "dim" && !(lavadimlist.ContainsKey(c)))
            {
                UInt32 waterdimresult = alphaBlend(c, dimI(waterColor), 0.5);

                waterdimlist.Add(c, toColor(waterdimresult));
                lavadimlist.Add(c, toColor(alphaBlend(c, dimI(lavaColor), 0.5)));
            }
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

        public void InitializeMapperDefs2() //Credits go to the authors of MoreTerra
        {
            ColorDefs = new Dictionary<int, Color>(255 + Main.maxTilesY);

            //tiles
            ColorDefs[0] = Constants.Terrafirma_Color.DIRT;
            ColorDefs[1] = Constants.Terrafirma_Color.STONE;
            ColorDefs[2] = Constants.Terrafirma_Color.GRASS;
            ColorDefs[3] = Constants.Terrafirma_Color.WEEDS;
            ColorDefs[4] = Constants.Terrafirma_Color.TORCH;
            ColorDefs[5] = Constants.Terrafirma_Color.TREE;
            ColorDefs[6] = Constants.Terrafirma_Color.IRON_ORE;
            ColorDefs[7] = Constants.Terrafirma_Color.COPPER_ORE;
            ColorDefs[8] = Constants.Terrafirma_Color.GOLD_ORE;
            ColorDefs[9] = Constants.Terrafirma_Color.SILVER_ORE;
            ColorDefs[10] = Constants.Terrafirma_Color.CLOSED_DOOR;
            ColorDefs[11] = Constants.Terrafirma_Color.OPEN_DOOR;
            ColorDefs[12] = Constants.Terrafirma_Color.HEARTSTONE;
            ColorDefs[13] = Constants.Terrafirma_Color.BOTTLE;
            ColorDefs[14] = Constants.Terrafirma_Color.TABLE;
            ColorDefs[15] = Constants.Terrafirma_Color.CHAIR;
            ColorDefs[16] = Constants.Terrafirma_Color.ANVIL;
            ColorDefs[17] = Constants.Terrafirma_Color.FURNACE;
            ColorDefs[18] = Constants.Terrafirma_Color.WORKBENCH;
            ColorDefs[19] = Constants.Terrafirma_Color.WOODEN_PLATFORM;
            ColorDefs[20] = Constants.Terrafirma_Color.SAPLING;
            ColorDefs[21] = Constants.Terrafirma_Color.CHEST;
            ColorDefs[22] = Constants.Terrafirma_Color.DEMONITE_ORE;
            ColorDefs[23] = Constants.Terrafirma_Color.CORRUPTED_GRASS;
            ColorDefs[24] = Constants.Terrafirma_Color.CORRUPTED_WEEDS;
            ColorDefs[25] = Constants.Terrafirma_Color.EBONSTONE;
            ColorDefs[26] = Constants.Terrafirma_Color.DEMON_ALTAR;
            ColorDefs[27] = Constants.Terrafirma_Color.SUNFLOWER;
            ColorDefs[28] = Constants.Terrafirma_Color.POT;
            ColorDefs[29] = Constants.Terrafirma_Color.PIGGY_BANK;
            ColorDefs[30] = Constants.Terrafirma_Color.WOOD;
            ColorDefs[31] = Constants.Terrafirma_Color.SHADOW_ORB;
            ColorDefs[32] = Constants.Terrafirma_Color.CORRUPTED_VINES;
            ColorDefs[33] = Constants.Terrafirma_Color.CANDLE;
            ColorDefs[34] = Constants.Terrafirma_Color.COPPER_CHANDELIER;
            ColorDefs[35] = Constants.Terrafirma_Color.SILVER_CHANDELIER;
            ColorDefs[36] = Constants.Terrafirma_Color.GOLD_CHANDELIER;
            ColorDefs[37] = Constants.Terrafirma_Color.METEORITE;
            ColorDefs[38] = Constants.Terrafirma_Color.GRAY_BRICK;
            ColorDefs[39] = Constants.Terrafirma_Color.CLAY_BRICK;
            ColorDefs[40] = Constants.Terrafirma_Color.CLAY;
            ColorDefs[41] = Constants.Terrafirma_Color.BLUE_BRICK;
            ColorDefs[42] = Constants.Terrafirma_Color.LIGHT_GLOBE;
            ColorDefs[43] = Constants.Terrafirma_Color.GREEN_BRICK;
            ColorDefs[44] = Constants.Terrafirma_Color.PINK_BRICK;
            ColorDefs[45] = Constants.Terrafirma_Color.GOLD_BRICK;
            ColorDefs[46] = Constants.Terrafirma_Color.SILVER_BRICK;
            ColorDefs[47] = Constants.Terrafirma_Color.COPPER_BRICK;
            ColorDefs[48] = Constants.Terrafirma_Color.SPIKES;
            ColorDefs[49] = Constants.Terrafirma_Color.BLUE_CANDLE;
            ColorDefs[50] = Constants.Terrafirma_Color.BOOKS;
            ColorDefs[51] = Constants.Terrafirma_Color.COBWEBS;
            ColorDefs[52] = Constants.Terrafirma_Color.VINES;
            ColorDefs[53] = Constants.Terrafirma_Color.SAND;
            ColorDefs[54] = Constants.Terrafirma_Color.GLASS;
            ColorDefs[55] = Constants.Terrafirma_Color.SIGN;
            ColorDefs[56] = Constants.Terrafirma_Color.OBSIDIAN;
            ColorDefs[57] = Constants.Terrafirma_Color.ASH;
            ColorDefs[58] = Constants.Terrafirma_Color.HELLSTONE;
            ColorDefs[59] = Constants.Terrafirma_Color.MUD;
            ColorDefs[60] = Constants.Terrafirma_Color.JUNGLE_GRASS;
            ColorDefs[61] = Constants.Terrafirma_Color.JUNGLE_WEEDS;
            ColorDefs[62] = Constants.Terrafirma_Color.JUNGLE_VINES;
            ColorDefs[63] = Constants.Terrafirma_Color.SAPPHIRE;
            ColorDefs[64] = Constants.Terrafirma_Color.RUBY;
            ColorDefs[65] = Constants.Terrafirma_Color.EMERALD;
            ColorDefs[66] = Constants.Terrafirma_Color.TOPAZ;
            ColorDefs[67] = Constants.Terrafirma_Color.AMETHYST;
            ColorDefs[68] = Constants.Terrafirma_Color.DIAMOND;
            ColorDefs[69] = Constants.Terrafirma_Color.JUNGLE_THORN;
            ColorDefs[70] = Constants.Terrafirma_Color.MUSHROOM_GRASS;
            ColorDefs[71] = Constants.Terrafirma_Color.MUSHROOM;
            ColorDefs[72] = Constants.Terrafirma_Color.MUSHROOM_TREE;
            ColorDefs[73] = Constants.Terrafirma_Color.WEEDS_73;
            ColorDefs[74] = Constants.Terrafirma_Color.WEEDS_74;
            ColorDefs[75] = Constants.Terrafirma_Color.OBSIDIAN_BRICK;
            ColorDefs[76] = Constants.Terrafirma_Color.HELLSTONE_BRICK;
            ColorDefs[77] = Constants.Terrafirma_Color.HELLFORGE;
            ColorDefs[78] = Constants.Terrafirma_Color.CLAY_POT;
            ColorDefs[79] = Constants.Terrafirma_Color.BED;
            ColorDefs[80] = Constants.Terrafirma_Color.CACTUS;
            ColorDefs[81] = Constants.Terrafirma_Color.CORAL;
            ColorDefs[82] = Constants.Terrafirma_Color.HERB_SPROUTS;
            ColorDefs[83] = Constants.Terrafirma_Color.HERB_STALKS;
            ColorDefs[84] = Constants.Terrafirma_Color.HERBS;
            ColorDefs[85] = Constants.Terrafirma_Color.TOMBSTONE;
            ColorDefs[86] = Constants.Terrafirma_Color.LOOM;
            ColorDefs[87] = Constants.Terrafirma_Color.PIANO;
            ColorDefs[88] = Constants.Terrafirma_Color.DRESSER;
            ColorDefs[89] = Constants.Terrafirma_Color.BENCH;
            ColorDefs[90] = Constants.Terrafirma_Color.BATHTUB;
            ColorDefs[91] = Constants.Terrafirma_Color.BANNER;
            ColorDefs[92] = Constants.Terrafirma_Color.LAMP_POST;
            ColorDefs[93] = Constants.Terrafirma_Color.TIKI_TORCH;
            ColorDefs[94] = Constants.Terrafirma_Color.KEG;
            ColorDefs[95] = Constants.Terrafirma_Color.CHINESE_LANTERN;
            ColorDefs[96] = Constants.Terrafirma_Color.COOKING_POT;
            ColorDefs[97] = Constants.Terrafirma_Color.SAFE;
            ColorDefs[98] = Constants.Terrafirma_Color.SKULL_LANTERN;
            ColorDefs[99] = Constants.Terrafirma_Color.TRASH_CAN;
            ColorDefs[100] = Constants.Terrafirma_Color.CANDELABRA;
            ColorDefs[101] = Constants.Terrafirma_Color.BOOKCASE;
            ColorDefs[102] = Constants.Terrafirma_Color.THRONE;
            ColorDefs[103] = Constants.Terrafirma_Color.BOWL;
            ColorDefs[104] = Constants.Terrafirma_Color.GRANDFATHER_CLOCK;
            ColorDefs[105] = Constants.Terrafirma_Color.STATUE;

            for (int i = 106; i < 265; i++)
            {
                ColorDefs[i] = Color.Magenta;
            }

            //global
            ColorDefs[265] = Constants.Terrafirma_Color.SKY;
            ColorDefs[266] = Constants.Terrafirma_Color.WATER;
            ColorDefs[267] = Constants.Terrafirma_Color.LAVA;

            //walls
            ColorDefs[268] = Constants.Terrafirma_Color.STONE_WALL;
            ColorDefs[269] = Constants.Terrafirma_Color.DIRT_WALL;
            ColorDefs[270] = Constants.Terrafirma_Color.STONE_WALL2;
            ColorDefs[271] = Constants.Terrafirma_Color.WOOD_WALL;
            ColorDefs[272] = Constants.Terrafirma_Color.BRICK_WALL;
            ColorDefs[273] = Constants.Terrafirma_Color.RED_BRICK_WALL;
            ColorDefs[274] = Constants.Terrafirma_Color.BLUE_BRICK_WALL;
            ColorDefs[275] = Constants.Terrafirma_Color.GREEN_BRICK_WALL;
            ColorDefs[276] = Constants.Terrafirma_Color.PINK_BRICK_WALL;
            ColorDefs[277] = Constants.Terrafirma_Color.GOLD_BRICK_WALL;
            ColorDefs[278] = Constants.Terrafirma_Color.SILVER_BRICK_WALL;
            ColorDefs[279] = Constants.Terrafirma_Color.COPPER_BRICK_WALL;
            ColorDefs[280] = Constants.Terrafirma_Color.HELLSTONE_BRICK_WALL;
            ColorDefs[281] = Constants.Terrafirma_Color.OBSIDIAN_WALL;
            ColorDefs[282] = Constants.Terrafirma_Color.MUD_WALL;
            ColorDefs[283] = Constants.Terrafirma_Color.DIRT_WALL2;
            ColorDefs[284] = Constants.Terrafirma_Color.DARK_BLUE_BRICK_WALL;
            ColorDefs[285] = Constants.Terrafirma_Color.DARK_GREEN_BRICK_WALL;
            ColorDefs[286] = Constants.Terrafirma_Color.DARK_PINK_BRICK_WALL;
            ColorDefs[287] = Constants.Terrafirma_Color.DARK_OBSIDIAN_WALL;

            //fix
            ColorDefs[288] = Constants.Terrafirma_Color.DARK_OBSIDIAN_WALL;
            ColorDefs[330] = Constants.Terrafirma_Color.DARK_OBSIDIAN_WALL;

            // this is for faster performace
            // rather than converting from Color to UInt32 alot.
            UInt32Defs = new Dictionary<int, UInt32>(255 + Main.maxTilesY);

            //adds sky and earth

            for (int i = 331; i < Main.worldSurface + 331; i++)
            {
                UInt32Defs[i] = 0x84AAF8;
                ColorDefs[i] = Constants.Terrafirma_Color.SKY;
            }
            for (int i = (int)Main.worldSurface + 331; i < (int)Main.rockLayer + 331; i++)
            {
                UInt32Defs[i] = 0x583D2E;
                ColorDefs[i] = Constants.Terrafirma_Color.EARTH;
            }
            for (int i = (int)Main.rockLayer + 331; i < Main.maxTilesY + 331; i++)
            {
                UInt32Defs[i] = 0x000000;
                ColorDefs[i] = Constants.Terrafirma_Color.HELL;
            }

            //adds the background fade in both ColorDefs and UInt32Defs
            for (int y = (int)Main.rockLayer; y < Main.maxTilesY; y++)
            {
                double alpha = (double)(y - Main.rockLayer) / (double)(Main.maxTilesY - Main.rockLayer);
                UInt32 c = alphaBlend(0x4A433C, 0x000000, alpha);   // (rockcolor, hellcolor, alpha)
                UInt32Defs[y + 331] = c;
                ColorDefs[y + 331] = toColor(c);
            }

            //tiles
            UInt32Defs[0] = 0x976B4B;
            UInt32Defs[1] = 0x808080;
            UInt32Defs[2] = 0x1CD85E;
            UInt32Defs[3] = 0x1E9648;
            UInt32Defs[4] = 0xFDDD03;
            UInt32Defs[5] = 0x976B4B;
            UInt32Defs[6] = 0xB5A495;
            UInt32Defs[7] = 0x964316;
            UInt32Defs[8] = 0xB9A417;
            UInt32Defs[9] = 0xD9DFDF;
            UInt32Defs[10] = 0xBF8F6F;
            UInt32Defs[11] = 0x946B50;
            UInt32Defs[12] = 0xB61239;
            UInt32Defs[13] = 0x4EC5FC;
            UInt32Defs[14] = 0x7F5C45;
            UInt32Defs[15] = 0xA2785C;
            UInt32Defs[16] = 0x505050;
            UInt32Defs[17] = 0x636363;
            UInt32Defs[18] = 0x7F5C45;
            UInt32Defs[19] = 0xB18567;
            UInt32Defs[20] = 0x1E9648;
            UInt32Defs[21] = 0x946B50;
            UInt32Defs[22] = 0x625FA7;
            UInt32Defs[23] = 0x8D89DF;
            UInt32Defs[24] = 0x6D6AAE;
            UInt32Defs[25] = 0x7D7991;
            UInt32Defs[26] = 0x5E5561;
            UInt32Defs[27] = 0xE3B903;
            UInt32Defs[28] = 0x796E61;
            UInt32Defs[29] = 0x9C546C;
            UInt32Defs[30] = 0xA97D5D;
            UInt32Defs[31] = 0x674D62;
            UInt32Defs[32] = 0x7A618F;
            UInt32Defs[33] = 0xFDDD03;
            UInt32Defs[34] = 0xB75819;
            UInt32Defs[35] = 0xC1CACB;
            UInt32Defs[36] = 0xB9A417;
            UInt32Defs[37] = 0x685654;
            UInt32Defs[38] = 0x8C8C8C;
            UInt32Defs[39] = 0xC37057;
            UInt32Defs[40] = 0x925144;
            UInt32Defs[41] = 0x6365C9;
            UInt32Defs[42] = 0xF99851;
            UInt32Defs[43] = 0x3FA931;
            UInt32Defs[44] = 0xA93175;
            UInt32Defs[45] = 0xCCB548;
            UInt32Defs[46] = 0xAEC1C2;
            UInt32Defs[47] = 0xCD7D47;
            UInt32Defs[48] = 0xAFAFAF;
            UInt32Defs[49] = 0x0B2EFF;
            UInt32Defs[50] = 0x3095AA;
            UInt32Defs[51] = 0x9EADAE;
            UInt32Defs[52] = 0x1E9648;
            UInt32Defs[53] = 0xD3C66F;
            UInt32Defs[54] = 0xC8F6FE;
            UInt32Defs[55] = 0x7F5C45;
            UInt32Defs[56] = 0x5751AD;
            UInt32Defs[57] = 0x44444C;
            UInt32Defs[58] = 0x8E4242;
            UInt32Defs[59] = 0x5C4449;
            UInt32Defs[60] = 0x8FD71D;
            UInt32Defs[61] = 0x63971F;
            UInt32Defs[62] = 0x28650D;
            UInt32Defs[63] = 0x2A82FA;
            UInt32Defs[64] = 0xFA2A51;
            UInt32Defs[65] = 0x05C95D;
            UInt32Defs[66] = 0xC78B09;
            UInt32Defs[67] = 0xA30BD5;
            UInt32Defs[68] = 0x19D1E7;
            UInt32Defs[69] = 0x855141;
            UInt32Defs[70] = 0x5D7FFF;
            UInt32Defs[71] = 0xB1AE83;
            UInt32Defs[72] = 0x968F6E;
            UInt32Defs[73] = 0x0D6524;
            UInt32Defs[74] = 0x28650D;
            UInt32Defs[75] = 0x665CC2;
            UInt32Defs[76] = 0x8E4242;
            UInt32Defs[77] = 0xEE6646;
            UInt32Defs[78] = 0x796E61;
            UInt32Defs[79] = 0x5C6298;
            UInt32Defs[80] = 0x497811;
            UInt32Defs[81] = 0xe5533f;
            UInt32Defs[82] = 0xfe5402;
            UInt32Defs[83] = 0xfe5402;
            UInt32Defs[84] = 0xfe5402;
            UInt32Defs[85] = 0xc0c0c0;
            UInt32Defs[86] = 0x7F5C45;
            UInt32Defs[87] = 0x584430;
            UInt32Defs[88] = 0x906850;
            UInt32Defs[89] = 0xB18567;
            UInt32Defs[90] = 0x606060;
            UInt32Defs[91] = 0x188008;
            UInt32Defs[92] = 0x323232;
            UInt32Defs[93] = 0x503B2F;
            UInt32Defs[94] = 0xA87858;
            UInt32Defs[95] = 0xF87800;
            UInt32Defs[96] = 0x606060;
            UInt32Defs[97] = 0x808080;
            UInt32Defs[98] = 0xB2B28A;
            UInt32Defs[99] = 0x808080;
            UInt32Defs[100] = 0xCCB548;
            UInt32Defs[101] = 0xB08460;
            UInt32Defs[102] = 0x780C08;
            UInt32Defs[103] = 0x8D624D;
            UInt32Defs[104] = 0x946B50;
            UInt32Defs[105] = 0x282828;

            // unknown
            for (int i = 106; i < 265; i++)
            {
                UInt32Defs[i] = 0xFF00FF;
            }

            //global
            UInt32Defs[265] = 0x84AAF8;
            UInt32Defs[266] = 0x093dbf;
            UInt32Defs[267] = 0xfd2003;

            //walls
            UInt32Defs[268] = 0x343434;
            UInt32Defs[269] = 0x583D2E;
            UInt32Defs[270] = 0x3D3A4E;
            UInt32Defs[271] = 0x523C2D;
            UInt32Defs[272] = 0x464646;
            UInt32Defs[273] = 0x5B1E1E;
            UInt32Defs[274] = 0x212462;
            UInt32Defs[275] = 0x0E4410;
            UInt32Defs[276] = 0x440E31;
            UInt32Defs[277] = 0x4A3E0C;
            UInt32Defs[278] = 0x576162;
            UInt32Defs[279] = 0x4B200B;
            UInt32Defs[280] = 0x301515;
            UInt32Defs[281] = 0x332F60;
            UInt32Defs[282] = 0x31282B;
            UInt32Defs[283] = 0x583D2E;
            UInt32Defs[284] = 0x2A2D48;
            UInt32Defs[285] = 0x4F4F43;
            UInt32Defs[286] = 0x543E40;
            UInt32Defs[287] = 0x332F60;

            UInt32Defs[288] = 0x332F60;
            UInt32Defs[330] = 0x332F60;

            //list for when dimming the world for highlighting
            DimColorDefs = new Dictionary<int, Color>(255 + Main.maxTilesY);
            DimUInt32Defs = new Dictionary<int, UInt32>(255 + Main.maxTilesY);
        }
    }
}
