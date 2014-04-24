using System.Drawing;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System;
using System.Threading;
using Terraria;

namespace Map
{
    public partial class MapPlugin
    {
        public static Dictionary<int, System.Drawing.Color> ColorDefs;
        public static Dictionary<int, UInt32> UInt32Defs;
        public static Dictionary<int, System.Drawing.Color> DimColorDefs;
        public static Dictionary<int, UInt32> DimUInt32Defs;

        public static Bitmap bmp;
        public static volatile bool pixelfailureflag; //volatile, to make it thread-safe.
        public static void SetPixel(Bitmap bit, int x, int y, System.Drawing.Color color, bool log)   // To prevent out of memory errors
        {
            try
            {
                bit.SetPixel(x, y, color);
            }
            catch (Exception) // "The operation failed" - http://msdn.microsoft.com/en-us/library/system.drawing.bitmap.setpixel(v=vs.110).aspx
            {
                //not successful - log it, but don't abort.
                if (log)
                {
                    TShockAPI.Utils.Instance.SendLogs("<map> WARNING: could not draw certain pixel (" + x + "," + y + ").", Color.Yellow);
                }
                pixelfailureflag = true;
                return;
            }
        }
        public static Dictionary<UInt32, System.Drawing.Color> waterblendlist = new Dictionary<UInt32, System.Drawing.Color>();
        public static Dictionary<UInt32, System.Drawing.Color> lavablendlist = new Dictionary<UInt32, System.Drawing.Color>();
        //better to have a separate list for dim liquid lists
        public static Dictionary<UInt32, System.Drawing.Color> waterdimlist = new Dictionary<UInt32, System.Drawing.Color>();
        public static Dictionary<UInt32, System.Drawing.Color> lavadimlist = new Dictionary<UInt32, System.Drawing.Color>();

        public int paintbackground(System.Drawing.Color SKY, System.Drawing.Color EARTH, System.Drawing.Color HELL)
        {
            int state = 0;
            Graphics graphicsHandle = Graphics.FromImage((Image)bmp);
            //all three
            if ((Main.worldSurface > y1) && (Main.rockLayer < y2))
            {
                graphicsHandle.FillRectangle(new SolidBrush(SKY), 0, 0, bmp.Width, (float)(Main.worldSurface - y1));
                graphicsHandle.FillRectangle(new SolidBrush(EARTH), 0, (float)(Main.worldSurface - y1), bmp.Width, (float)(Main.rockLayer - y1));
                graphicsHandle.FillRectangle(new SolidBrush(HELL), 0, (float)(Main.rockLayer - y1), bmp.Width, (float)y2);
                state = 1;
            }
            //just sky
            if ((Main.worldSurface > y2))
            {
                graphicsHandle.FillRectangle(new SolidBrush(SKY), 0, 0, bmp.Width, (float)(Main.worldSurface - y1));
                state = 2;
            }
            //sky and earth
            if ((Main.worldSurface > y1) && (Main.rockLayer > y2))
            {
                graphicsHandle.FillRectangle(new SolidBrush(SKY), 0, 0, bmp.Width, (float)(Main.worldSurface - y1));
                graphicsHandle.FillRectangle(new SolidBrush(EARTH), 0, (float)(Main.worldSurface - y1), bmp.Width, (float)y2);
                state = 3;
            }
            //earth and hell
            if ((Main.worldSurface < y1) && (Main.rockLayer < y2))
            {
                graphicsHandle.FillRectangle(new SolidBrush(EARTH), 0, 0, bmp.Width, (float)(Main.rockLayer - y1));
                graphicsHandle.FillRectangle(new SolidBrush(HELL), 0, (float)(Main.rockLayer - y1), bmp.Width, (float)y2);
                state = 4;
            }
            //just hell
            if (Main.rockLayer < y1)
            {
                graphicsHandle.FillRectangle(new SolidBrush(HELL), 0, 0, bmp.Width, (float)y2);
                state = 5;
            }
            //just earth
            if ((Main.worldSurface < y1) && (Main.rockLayer > y2))
            {
                graphicsHandle.FillRectangle(new SolidBrush(EARTH), 0, 0, bmp.Width, (float)y2);
                state = 6;
            }

            return state;
        }

        public void mapWorld2()
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
            utils.SendLogs("Saving Image...", Color.WhiteSmoke);
            stopwatch.Start();

            try
            {
                bmp = new Bitmap((x2 - x1) / 4, (y2 - y1), PixelFormat.Format32bppArgb);
            }
            catch (ArgumentException e)
            {
                utils.SendLogs("<map> ERROR: could not create initial Bitmap object.", Color.Red);
                utils.SendLogs(e.StackTrace.ToString(), Color.WhiteSmoke);
                stopwatch.Stop();
                isMapping = false;
                return;
            }

            Graphics graphicsHandle = Graphics.FromImage((Image)bmp);
            //draw background
            if (highlight)
            {
                System.Drawing.Color SKY = dimC(0x84AAF8);
                System.Drawing.Color EARTH = dimC(0x583D2E);
                System.Drawing.Color HELL = dimC(0x000000);
                int state = paintbackground(SKY, EARTH, HELL);

                //if has earth or hell
                if (state != 2)
                {
                    //this fades the background from rock to hell
                    try
                    {
                        System.Drawing.Color dimColor;
                        for (int y = (int)(Main.rockLayer - y1); y < y2; y++)
                        {
                            dimColor = dimC(UInt32Defs[595 + (y + y1)]);
                            graphicsHandle.DrawLine(new Pen(dimColor), 0, y, bmp.Width, y);
                        }
                    }
                    catch (KeyNotFoundException e)
                    {
                        utils.SendLogs("<map> ERROR: could not fade the dimmed background from rock to hell.", Color.Red);
                        utils.SendLogs(e.StackTrace.ToString(), Color.WhiteSmoke);
                        //continue and see if we keep getting errors when painting the actual world
                    }
                }
            }
            else
            {
                try
                {
                    int state = paintbackground(Constants.Terrafirma_Color.SKY, Constants.Terrafirma_Color.EARTH, Constants.Terrafirma_Color.HELL);
                    //if has earth or hell
                    if (state != 2)
                    {
                        //this fades the background from rock to hell
                        for (int y = (int)(Main.rockLayer - y1); y < y2; y++)
                        {
                            graphicsHandle.DrawLine(new Pen(ColorDefs[595 + (y + y1)]), 0, y, bmp.Width, y);
                        }
                    }
                }
                catch (KeyNotFoundException e)
                {
                    utils.SendLogs("<map> ERROR: could not fade the background from rock to hell.", Color.Red);
                    utils.SendLogs(e.StackTrace.ToString(), Color.WhiteSmoke);

                    //continue and see if we keep getting errors when painting the actual world
                }
                
            }

            piece1 = (Bitmap)bmp.Clone();
            piece2 = (Bitmap)bmp.Clone();
            piece3 = (Bitmap)bmp.Clone();
            piece4 = (Bitmap)bmp.Clone();
            bmp.Dispose();
            bmp = null;

            // splits the work into four threads
            Thread part1 = null, part2 = null, part3 = null, part4 = null;
            try
            {
                part1 = new Thread(mapthread1);
                part2 = new Thread(mapthread2);
                part3 = new Thread(mapthread3);
                part4 = new Thread(mapthread4);
                part1.Name = "Map mapper 1";
                part2.Name = "Map mapper 2";
                part3.Name = "Map mapper 3";
                part4.Name = "Map mapper 4";
                part1.Start();
                part2.Start();
                part3.Start();
                part4.Start();
            }
            catch (OutOfMemoryException e)
            {
                part1.Abort();
                part2.Abort();
                part3.Abort();
                part4.Abort();

                utils.SendLogs("<map> ERROR: not enough memory to start mapping threads.", Color.Red);
                utils.SendLogs(e.StackTrace.ToString(), Color.WhiteSmoke);

                //for memory's sake
                piece1.Dispose();
                piece1 = null;
                piece2.Dispose();
                piece2 = null;
                piece3.Dispose();
                piece3 = null;
                piece4.Dispose();
                piece4 = null;
                return;
            }

            //wait for threads to finish mapping.
            while (part1.IsAlive || part2.IsAlive || part3.IsAlive || part4.IsAlive) ;

            //mapping is done.. now to create final bitmap

            try
            {
                bmp = new Bitmap((x2 - x1), (y2 - y1), PixelFormat.Format32bppArgb);
            }
            catch (ArgumentException e)
            {
                utils.SendLogs("<map> ERROR: could not create final Bitmap object.", Color.Red);
                utils.SendLogs(e.StackTrace.ToString(), Color.WhiteSmoke);
                stopwatch.Stop();
                isMapping = false;
                return;
            }

            int quarter = ((x2 - x1) / 4);
            Graphics gfx;
            using (gfx = Graphics.FromImage(bmp))
            {
                gfx.DrawImage(piece1, new System.Drawing.Point(0, 0));
                gfx.DrawImage(piece2, new System.Drawing.Point(quarter, 0));
                gfx.DrawImage(piece3, new System.Drawing.Point(2 * quarter, 0));
                gfx.DrawImage(piece4, new System.Drawing.Point(3 * quarter, 0));
            }
            gfx.Dispose();

            pixelfailureflag = false;

            if (hlchests)
            {
                Terraria.Chest[] c = Main.chest;
                for (int i = 0; i < c.Length; i++)
                {
                    if (c[i] != null)
                    {
                        SetPixel(bmp, c[i].x, c[i].y, System.Drawing.Color.White, true);

                        //also the four pixels next to it so we can actually see it on the map
                        SetPixel(bmp, c[i].x + 1, c[i].y, System.Drawing.Color.White, true);
                        SetPixel(bmp, c[i].x - 1, c[i].y, System.Drawing.Color.White, true);
                        SetPixel(bmp, c[i].x, c[i].y + 1, System.Drawing.Color.White, true);
                        SetPixel(bmp, c[i].x, c[i].y - 1, System.Drawing.Color.White, true);
                    }
                }
            }

            if(pixelfailureflag)
            {
                utils.SendLogs("<map> WARNING: pixel fail write on hlchests.", Color.Yellow);
                pixelfailureflag = false;
            }
            
            utils.SendLogs("Saving Data...", Color.WhiteSmoke);
            bmp.Save(string.Concat(p, Path.DirectorySeparatorChar, filename));
            bmp.Dispose();
            stopwatch.Stop();
            utils.SendLogs("Save duration: " + stopwatch.Elapsed.Seconds + " Second(s)", Color.WhiteSmoke);
            utils.SendLogs("Saving Complete.", Color.WhiteSmoke);
            bmp = null;
            piece1.Dispose();
            piece1 = null;
            piece2.Dispose();
            piece2 = null;
            piece3.Dispose();
            piece3 = null;
            piece4.Dispose();
            piece4 = null;
            isMapping = false;
        }

        private static Bitmap piece1;
        private static Bitmap piece2;
        private static Bitmap piece3;
        private static Bitmap piece4;

        //cropping
        //DONE?: math of the y axis.
        //DONE?: math of the x axis.
        //TODO: testing of cropping

        private void mapthread1()
        {
            if (!crop)
            {
                int quarter = (Main.maxTilesX / 4);
                using (var prog = new ProgressLogger(quarter - 1, "Saving image data"))
                    for (int i = 0; i < quarter; i++)
                    {
                        prog.Value = i; // each thread finished about the same time so I put the progress logger on one of them
                        maprenderloop(i, piece1, 0);
                    }
            }
            else
            {
                int quarter = ((x2-x1) / 4);
                using (var prog = new ProgressLogger(quarter - 1, "Saving image data"))
                    for (int i = 0; i < quarter; i++)
                    {
                        prog.Value = i; // each thread finished about the same time so I put the progress logger on one of them
                        maprenderloop(x1 + i, piece1, 0,y1,y2);
                    }
            }
        }

        private void mapthread2()
        {
            if (!crop)
            {
                int quarter = (Main.maxTilesX / 4);
                for (int i = 0; i < quarter; i++)
                {
                    maprenderloop(i + quarter, piece2, quarter);
                }
            }
            else
            {
                int quarter = ((x2 - x1) / 4);
                for (int i = 0; i < quarter; i++)
                {
                    maprenderloop(x1 + i + quarter, piece2, quarter,y1,y2);
                }
            }
        }

        private void mapthread3()
        {
            if (!crop)
            {
                int quarter = (Main.maxTilesX / 4);
                for (int i = 0; i < quarter; i++)
                {
                    maprenderloop(i + 2 * quarter, piece3, 2 * quarter);
                }
            }
            else
            {
                int quarter = ((x2 - x1) / 4);
                for (int i = 0; i < quarter; i++)
                {
                    maprenderloop(x1 + i + 2 * quarter, piece3, 2 * quarter,y1,y2);
                }
            }
        }

        private void mapthread4()
        {
            if (!crop)
            {
                int quarter = (Main.maxTilesX / 4);
                for (int i = 0; i < quarter; i++)
                {
                    maprenderloop(i + 3 * quarter, piece4, 3 * quarter);
                }
            }
            else
            {
                int quarter = ((x2 - x1) / 4);
                for (int i = 0; i < quarter; i++)
                {
                    maprenderloop(x1 + i + 3 * quarter, piece4, 3 * quarter,y1,y2);
                }
            }
        }

        /**
         * This maps a 1 pixel vertical slice at x = i;
         */
        private void maprenderloop(int i, Bitmap bmp, int piece)
        {
            maprenderloop(i,bmp,piece,0,Main.maxTilesY);
        }

        /**
         * This maps a 1 pixel vertical slice at x = i; (from y = ymin to y = ymax)
         */
        private void maprenderloop(int i, Bitmap bmp, int piece, int ymin, int ymax)
        {
            UInt32 tempColor;
            List<int> list;
            int x = i - x1;
            for (int j = ymin; j < ymax; j++)
            {
                if (highlight) //dim the world
                {
                    //draws tiles or walls
                    if (Main.tile[i, j].wall == 0)
                    {
                        if (Main.tile[i, j].active())
                        {
                            SetPixel(bmp, x - piece, j - ymin, DimColorDefs[Main.tile[i, j].type], false);
                            tempColor = DimUInt32Defs[Main.tile[i, j].type];
                        }
                        else
                        {
                            tempColor = DimUInt32Defs[j + 595];
                        }
                    }
                    else
                    {
                        //priority to tiles
                        if (Main.tile[i, j].active())
                        {
                            SetPixel(bmp, x - piece, j - ymin, DimColorDefs[Main.tile[i, j].type], false);
                            tempColor = DimUInt32Defs[Main.tile[i, j].type];
                        }
                        else
                        {
                            SetPixel(bmp, x - piece, j - ymin, DimColorDefs[Main.tile[i, j].wall + 450], false);
                            tempColor = DimUInt32Defs[Main.tile[i, j].wall + 450];
                        }
                    }
                    // lookup blendcolor of color just drawn, and draw again
                    if (Main.tile[i, j].liquid > 0)
                    {
                        if (lavadimlist.ContainsKey(tempColor))
                        {  // incase the map has hacked data
                            SetPixel(bmp, x - piece, j - ymin, Main.tile[i, j].lava() ? lavadimlist[tempColor] : waterdimlist[tempColor], false);
                        }
                    }

                    list = getGiveID(Main.tile[i, j].type, (Main.tile[i, j].wall));
                    //highlight the tiles of supplied type from the map command
                    if (list.Contains(highlightID))
                    {
                        SetPixel(bmp, x - piece, j - ymin, System.Drawing.Color.White, false);
                    }
                }
                else
                {
                    //draws tiles or walls
                    if (Main.tile[i, j].wall == 0)
                    {
                        if (Main.tile[i, j].active())
                        {
                            SetPixel(bmp, x - piece, j - ymin, ColorDefs[Main.tile[i, j].type], false);
                            tempColor = UInt32Defs[Main.tile[i, j].type];
                        }
                        else
                        {
                            tempColor = UInt32Defs[j + 595];
                        }
                    }
                    else
                    {
                        //priority to tiles
                        if (Main.tile[i, j].active())
                        {
                            SetPixel(bmp, x - piece, j - ymin, ColorDefs[Main.tile[i, j].type], false);
                            tempColor = UInt32Defs[Main.tile[i, j].type];
                        }
                        else
                        {
                            SetPixel(bmp, x - piece, j - ymin, ColorDefs[Main.tile[i, j].wall + 450], false);
                            tempColor = UInt32Defs[Main.tile[i, j].wall + 450];
                        }
                    }
                    // lookup blendcolor of color just drawn, and draw again
                    if (Main.tile[i, j].liquid > 0)
                    {
                        if (lavablendlist.ContainsKey(tempColor))
                        {  // incase the map has hacked data
                            SetPixel(bmp, x - piece, j - ymin, Main.tile[i, j].lava() ? lavablendlist[tempColor] : waterblendlist[tempColor], false);
                        }
                    }
                }

            }

                if(pixelfailureflag)
                {
                    utils.SendLogs("<map> WARNING: could not draw certain pixel at row (" + i + ",y).", Color.Yellow);
                    pixelfailureflag = false;
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
                for (int y = 0; y <= Main.maxTilesY + 595; y++)
                {
                    if (UInt32Defs.ContainsKey(y))
                    {
                        UInt32 c = UInt32Defs[y];

                        //initialize DimColorDefs and DimUInt32Defs first
                        UInt32 dimblend = dimI(c);
                        System.Drawing.Color dimresult = dimC(c);
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
                System.Drawing.Color waterblendresult = toColor(alphaBlend(c, waterColor, 0.5));

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
                public static System.Drawing.Color DIRT = ColorTranslator.FromHtml("#976B4B");
                public static System.Drawing.Color STONE = ColorTranslator.FromHtml("#808080");
                public static System.Drawing.Color GRASS = ColorTranslator.FromHtml("#1CD85E");
                public static System.Drawing.Color WEED = ColorTranslator.FromHtml("#1E9648");
                public static System.Drawing.Color TORCH = ColorTranslator.FromHtml("#FDDD03");
                public static System.Drawing.Color TREE = ColorTranslator.FromHtml("#976B4B");
                public static System.Drawing.Color IRON_ORE = ColorTranslator.FromHtml("#B5A495");
                public static System.Drawing.Color COPPER_ORE = ColorTranslator.FromHtml("#964316");
                public static System.Drawing.Color GOLD_ORE = ColorTranslator.FromHtml("#B9A417");
                public static System.Drawing.Color SILVER_ORE = ColorTranslator.FromHtml("#D9DFDF");
                public static System.Drawing.Color CLOSED_DOORS = ColorTranslator.FromHtml("#BF8F6F");
                public static System.Drawing.Color OPEN_DOORS = ColorTranslator.FromHtml("#946B50");
                public static System.Drawing.Color HEARTSTONE = ColorTranslator.FromHtml("#B61239");
                public static System.Drawing.Color BOTTLE = ColorTranslator.FromHtml("#4EC5FC");
                public static System.Drawing.Color TABLES = ColorTranslator.FromHtml("#7F5C45");
                public static System.Drawing.Color CHAIRS = ColorTranslator.FromHtml("#A2785C");
                public static System.Drawing.Color IRON_ANVIL = ColorTranslator.FromHtml("#505050");
                public static System.Drawing.Color FURNACE = ColorTranslator.FromHtml("#636363");
                public static System.Drawing.Color WORK_BENCH = ColorTranslator.FromHtml("#7F5C45");
                public static System.Drawing.Color WOOD_PLATFORM = ColorTranslator.FromHtml("#B18567");
                public static System.Drawing.Color SAPLING = ColorTranslator.FromHtml("#1E9648");
                public static System.Drawing.Color CHEST = ColorTranslator.FromHtml("#946B50");
                public static System.Drawing.Color DEMONITE_ORE = ColorTranslator.FromHtml("#625FA7");
                public static System.Drawing.Color CORRUPTED_GRASS = ColorTranslator.FromHtml("#8D89DF");
                public static System.Drawing.Color CORRUPTED_WEEDS = ColorTranslator.FromHtml("#6D6AAE");
                public static System.Drawing.Color EBONSTONE = ColorTranslator.FromHtml("#7D7991");
                public static System.Drawing.Color DEMON_ALTAR = ColorTranslator.FromHtml("#5E5561");
                public static System.Drawing.Color SUNFLOWER = ColorTranslator.FromHtml("#E3B903");
                public static System.Drawing.Color POT = ColorTranslator.FromHtml("#796E61");
                public static System.Drawing.Color PIGGY_BANK = ColorTranslator.FromHtml("#9C546C");
                public static System.Drawing.Color WOOD = ColorTranslator.FromHtml("#A97D5D");
                public static System.Drawing.Color SHADOW_ORB = ColorTranslator.FromHtml("#674D62");
                public static System.Drawing.Color CORRUPTED_VINES = ColorTranslator.FromHtml("#7A618F");
                public static System.Drawing.Color CANDLE = ColorTranslator.FromHtml("#FDDD03");
                public static System.Drawing.Color COPPER_CHANDELIER = ColorTranslator.FromHtml("#B75819");
                public static System.Drawing.Color JACK_O_LANTERN = ColorTranslator.FromHtml("#EAAD53");
                public static System.Drawing.Color PRESENT = ColorTranslator.FromHtml("#C22E32");
                public static System.Drawing.Color METEORITE = ColorTranslator.FromHtml("#685654");
                public static System.Drawing.Color GRAY_BRICK = ColorTranslator.FromHtml("#8C8C8C");
                public static System.Drawing.Color RED_BRICK = ColorTranslator.FromHtml("#C37057");
                public static System.Drawing.Color CLAY = ColorTranslator.FromHtml("#925144");
                public static System.Drawing.Color BLUE_BRICK = ColorTranslator.FromHtml("#454E63");
                public static System.Drawing.Color CHAIN_LANTERN = ColorTranslator.FromHtml("#F99851");
                public static System.Drawing.Color GREEN_BRICK = ColorTranslator.FromHtml("#526556");
                public static System.Drawing.Color PINK_BRICK = ColorTranslator.FromHtml("#8A4469");
                public static System.Drawing.Color GOLD_BRICK = ColorTranslator.FromHtml("#947E18");
                public static System.Drawing.Color SILVER_BRICK = ColorTranslator.FromHtml("#AEC1C2");
                public static System.Drawing.Color COPPER_BRICK = ColorTranslator.FromHtml("#D5651A");
                public static System.Drawing.Color SPIKE = ColorTranslator.FromHtml("#AFAFAF");
                public static System.Drawing.Color WATER_CANDLE = ColorTranslator.FromHtml("#0B2EFF");
                public static System.Drawing.Color BOOK = ColorTranslator.FromHtml("#3095AA");
                public static System.Drawing.Color COBWEB = ColorTranslator.FromHtml("#9EADAE");
                public static System.Drawing.Color VINES = ColorTranslator.FromHtml("#1E9648");
                public static System.Drawing.Color SAND = ColorTranslator.FromHtml("#D3C66F");
                public static System.Drawing.Color GLASS = ColorTranslator.FromHtml("#C8F6FE");
                public static System.Drawing.Color SIGN = ColorTranslator.FromHtml("#7F5C45");
                public static System.Drawing.Color OBSIDIAN = ColorTranslator.FromHtml("#41414D");
                public static System.Drawing.Color ASH = ColorTranslator.FromHtml("#44444C");
                public static System.Drawing.Color HELLSTONE = ColorTranslator.FromHtml("#8E4242");
                public static System.Drawing.Color MUD = ColorTranslator.FromHtml("#5C4449");
                public static System.Drawing.Color JUNGLE_GRASS = ColorTranslator.FromHtml("#8FD71D");
                public static System.Drawing.Color JUNGLE_WEEDS = ColorTranslator.FromHtml("#63971F");
                public static System.Drawing.Color JUNGLE_VINES = ColorTranslator.FromHtml("#28650D");
                public static System.Drawing.Color SAPPHIRE = ColorTranslator.FromHtml("#2A82FA");
                public static System.Drawing.Color RUBY = ColorTranslator.FromHtml("#FA2A51");
                public static System.Drawing.Color EMERALD = ColorTranslator.FromHtml("#05C95D");
                public static System.Drawing.Color TOPAZ = ColorTranslator.FromHtml("#C78B09");
                public static System.Drawing.Color AMETHYST = ColorTranslator.FromHtml("#A30BD5");
                public static System.Drawing.Color DIAMOND = ColorTranslator.FromHtml("#19D1E7");
                public static System.Drawing.Color JUNGLE_THORN = ColorTranslator.FromHtml("#855141");
                public static System.Drawing.Color MUSHROOM_GRASS = ColorTranslator.FromHtml("#5D7FFF");
                public static System.Drawing.Color MUSHROOM = ColorTranslator.FromHtml("#B1AE83");
                public static System.Drawing.Color MUSHROOM_TREE = ColorTranslator.FromHtml("#968F6E");
                public static System.Drawing.Color WEEDS_73 = ColorTranslator.FromHtml("#0D6524");
                public static System.Drawing.Color WEEDS_74 = ColorTranslator.FromHtml("#28650D");
                public static System.Drawing.Color OBSIDIAN_BRICK = ColorTranslator.FromHtml("#0B0B0B");
                public static System.Drawing.Color HELLSTONE_BRICK = ColorTranslator.FromHtml("#8E4242");
                public static System.Drawing.Color HELLFORGE = ColorTranslator.FromHtml("#EE6646");
                public static System.Drawing.Color CLAY_POT = ColorTranslator.FromHtml("#796E61");
                public static System.Drawing.Color BED = ColorTranslator.FromHtml("#5C6298");
                public static System.Drawing.Color CACTUS = ColorTranslator.FromHtml("#497811");
                public static System.Drawing.Color CORAL = ColorTranslator.FromHtml("#E5533F");
                public static System.Drawing.Color HERB_SPROUT = ColorTranslator.FromHtml("#FE5402");
                public static System.Drawing.Color HERB = ColorTranslator.FromHtml("#FE5402");
                public static System.Drawing.Color HERB_BLOSSOM = ColorTranslator.FromHtml("#FE5402");
                public static System.Drawing.Color TOMBSTONE = ColorTranslator.FromHtml("#C0C0C0");
                public static System.Drawing.Color LOOM = ColorTranslator.FromHtml("#7F5C45");
                public static System.Drawing.Color PIANO = ColorTranslator.FromHtml("#584430");
                public static System.Drawing.Color DRESSER = ColorTranslator.FromHtml("#906850");
                public static System.Drawing.Color BENCH = ColorTranslator.FromHtml("#B18567");
                public static System.Drawing.Color BATHTUB = ColorTranslator.FromHtml("#606060");
                public static System.Drawing.Color BANNER = ColorTranslator.FromHtml("#188008");
                public static System.Drawing.Color LAMP_POST = ColorTranslator.FromHtml("#323232");
                public static System.Drawing.Color TIKI_TORCH = ColorTranslator.FromHtml("#503B2F");
                public static System.Drawing.Color KEG = ColorTranslator.FromHtml("#A87858");
                public static System.Drawing.Color CHINESE_LANTERN = ColorTranslator.FromHtml("#F87800");
                public static System.Drawing.Color COOKING_POT = ColorTranslator.FromHtml("#606060");
                public static System.Drawing.Color SAFE = ColorTranslator.FromHtml("#808080");
                public static System.Drawing.Color SKULL_LANTERN = ColorTranslator.FromHtml("#B2B28A");
                public static System.Drawing.Color TRASH_CAN = ColorTranslator.FromHtml("#808080");
                public static System.Drawing.Color CANDELABRA = ColorTranslator.FromHtml("#CCB548");
                public static System.Drawing.Color BOOKCASE = ColorTranslator.FromHtml("#B08460");
                public static System.Drawing.Color THRONE = ColorTranslator.FromHtml("#780C08");
                public static System.Drawing.Color BOWL = ColorTranslator.FromHtml("#8D624D");
                public static System.Drawing.Color GRANDFATHER_CLOCK = ColorTranslator.FromHtml("#946B50");
                public static System.Drawing.Color STATUE = ColorTranslator.FromHtml("#282828");
                public static System.Drawing.Color SAWMILL = ColorTranslator.FromHtml("#563E2C");
                public static System.Drawing.Color COBALT_ORE = ColorTranslator.FromHtml("#0B508F");
                public static System.Drawing.Color MYTHRIL_ORE = ColorTranslator.FromHtml("#5BA9A9");
                public static System.Drawing.Color HALLOWED_GRASS = ColorTranslator.FromHtml("#4EC1E3");
                public static System.Drawing.Color WEEDS = ColorTranslator.FromHtml("#1E9648");
                public static System.Drawing.Color ADAMANTITE_ORE = ColorTranslator.FromHtml("#801A34");
                public static System.Drawing.Color EBONSAND = ColorTranslator.FromHtml("#67627A");
                public static System.Drawing.Color WEEDS_2 = ColorTranslator.FromHtml("#1E9648");
                public static System.Drawing.Color TINKERERS_WORKSHOP = ColorTranslator.FromHtml("#7F5C45");
                public static System.Drawing.Color VINES_2 = ColorTranslator.FromHtml("#327FA1");
                public static System.Drawing.Color PEARLSAND = ColorTranslator.FromHtml("#D5C4C5");
                public static System.Drawing.Color PEARLSTONE = ColorTranslator.FromHtml("#B5ACBE");
                public static System.Drawing.Color PEARLSTONE_BRICK = ColorTranslator.FromHtml("#D5C4C5");
                public static System.Drawing.Color IRIDESCENT_BRICK = ColorTranslator.FromHtml("#3F3F49");
                public static System.Drawing.Color MUDSTONE = ColorTranslator.FromHtml("#967A7D");
                public static System.Drawing.Color COBALT_BRICK = ColorTranslator.FromHtml("#2576AB");
                public static System.Drawing.Color MYTHRIL_BRICK = ColorTranslator.FromHtml("#91BF75");
                public static System.Drawing.Color SILT = ColorTranslator.FromHtml("#595353");
                public static System.Drawing.Color WOODEN_BEAM = ColorTranslator.FromHtml("#5C4436");
                public static System.Drawing.Color CRYSTAL_BALL = ColorTranslator.FromHtml("#81A5FF");
                public static System.Drawing.Color DISCO_BALL = ColorTranslator.FromHtml("#DBDBDB");
                public static System.Drawing.Color GLASS_2 = ColorTranslator.FromHtml("#68B3C8");
                public static System.Drawing.Color MANNEQUIN = ColorTranslator.FromHtml("#906850");
                public static System.Drawing.Color CRYSTAL_SHARD = ColorTranslator.FromHtml("#004979");
                public static System.Drawing.Color ACTIVE_STONE = ColorTranslator.FromHtml("#A5A5A5");
                public static System.Drawing.Color INACTIVE_STONE = ColorTranslator.FromHtml("#1A1A1A");
                public static System.Drawing.Color LEVER = ColorTranslator.FromHtml("#C90303");
                public static System.Drawing.Color ADAMANTITE_FORGE = ColorTranslator.FromHtml("#891012");
                public static System.Drawing.Color MYTHRIL_ANVIL = ColorTranslator.FromHtml("#96AE87");
                public static System.Drawing.Color PRESSURE_PLATE = ColorTranslator.FromHtml("#FD7272");
                public static System.Drawing.Color SWITCH = ColorTranslator.FromHtml("#CCC0C0");
                public static System.Drawing.Color DART_TRAP = ColorTranslator.FromHtml("#8C8C8C");
                public static System.Drawing.Color BOULDER = ColorTranslator.FromHtml("#636363");
                public static System.Drawing.Color MUSIC_BOX = ColorTranslator.FromHtml("#996343");
                public static System.Drawing.Color DEMONITE_BRICK = ColorTranslator.FromHtml("#7875B3");
                public static System.Drawing.Color EXPLOSIVES = ColorTranslator.FromHtml("#AD2323");
                public static System.Drawing.Color INLET_PUMP = ColorTranslator.FromHtml("#C90303");
                public static System.Drawing.Color OUTLET_PUMP = ColorTranslator.FromHtml("#C90303");
                public static System.Drawing.Color TIMER = ColorTranslator.FromHtml("#C90303");
                public static System.Drawing.Color CANDY_CANE = ColorTranslator.FromHtml("#C01E1E");
                public static System.Drawing.Color GREEN_CANDY_CANE = ColorTranslator.FromHtml("#2BC01E");
                public static System.Drawing.Color SNOW = ColorTranslator.FromHtml("#C7DCDF");
                public static System.Drawing.Color SNOW_BRICK = ColorTranslator.FromHtml("#D3ECF1");
                public static System.Drawing.Color LIGHTS = ColorTranslator.FromHtml("#FFFFFF");
                public static System.Drawing.Color ADAMANTITE_BEAM = ColorTranslator.FromHtml("#731736");
                public static System.Drawing.Color SANDSTONE_BRICK = ColorTranslator.FromHtml("#BAA854");
                public static System.Drawing.Color EBONSTONE_BRICK = ColorTranslator.FromHtml("#5F5F95");
                public static System.Drawing.Color RED_STUCCO = ColorTranslator.FromHtml("#EF8D7E");
                public static System.Drawing.Color YELLOW_STUCCO = ColorTranslator.FromHtml("#DFDB93");
                public static System.Drawing.Color GREEN_STUCCO = ColorTranslator.FromHtml("#83A2A1");
                public static System.Drawing.Color GRAY_STUCCO = ColorTranslator.FromHtml("#BCBCB1");
                public static System.Drawing.Color EBONWOOD = ColorTranslator.FromHtml("#9989A5");
                public static System.Drawing.Color RICH_MAHOGANY = ColorTranslator.FromHtml("#915155");
                public static System.Drawing.Color PEARLWOOD = ColorTranslator.FromHtml("#57503F");
                public static System.Drawing.Color RAINBOW_BRICK = ColorTranslator.FromHtml("#D4D4D4");
                public static System.Drawing.Color ICE = ColorTranslator.FromHtml("#90C3E8");
                public static System.Drawing.Color BREAKABLE_ICE = ColorTranslator.FromHtml("#92AEBF");
                public static System.Drawing.Color PURPLE_ICE = ColorTranslator.FromHtml("#9188CB");
                public static System.Drawing.Color PINK_ICE = ColorTranslator.FromHtml("#D197BC");
                public static System.Drawing.Color STALAGTITE = ColorTranslator.FromHtml("#5C8FAF");
                public static System.Drawing.Color TIN_ORE = ColorTranslator.FromHtml("#817D5D");
                public static System.Drawing.Color LEAD_ORE = ColorTranslator.FromHtml("#2F3E57");
                public static System.Drawing.Color TUNGSTEN_ORE = ColorTranslator.FromHtml("#5A7D53");
                public static System.Drawing.Color PLATINUM_ORE = ColorTranslator.FromHtml("#8097B8");
                public static System.Drawing.Color PINE_TREE_BLOCK = ColorTranslator.FromHtml("#003F2C");
                public static System.Drawing.Color CHRISTMAS_TREE = ColorTranslator.FromHtml("#269660");
                public static System.Drawing.Color HAUNTED_PLATINUM_CHANDELIER = ColorTranslator.FromHtml("#4A3D26");
                public static System.Drawing.Color PLATINUM_CANDELABRA = ColorTranslator.FromHtml("#8097B8");
                public static System.Drawing.Color PLATINUM_CANDLE = ColorTranslator.FromHtml("#FE7902");
                public static System.Drawing.Color TIN_BRICK = ColorTranslator.FromHtml("#BBA57C");
                public static System.Drawing.Color TUNGSTEN_BRICK = ColorTranslator.FromHtml("#9CC09D");
                public static System.Drawing.Color PLATINUM_BRICK = ColorTranslator.FromHtml("#B5C2D9");
                public static System.Drawing.Color GEM = ColorTranslator.FromHtml("#892880");
                public static System.Drawing.Color MOSS = ColorTranslator.FromHtml("#318672");
                public static System.Drawing.Color BROWN_MOSS = ColorTranslator.FromHtml("#7E8631");
                public static System.Drawing.Color RED_MOSS = ColorTranslator.FromHtml("#863B31");
                public static System.Drawing.Color BLUE_MOSS = ColorTranslator.FromHtml("#2B568C");
                public static System.Drawing.Color PURPLE_MOSS = ColorTranslator.FromHtml("#793186");
                public static System.Drawing.Color FLOWERS = ColorTranslator.FromHtml("#208376");
                public static System.Drawing.Color RUBBLE = ColorTranslator.FromHtml("#808080");
                public static System.Drawing.Color RUBBLE_2 = ColorTranslator.FromHtml("#999979");
                public static System.Drawing.Color RUBBLE_3 = ColorTranslator.FromHtml("#63971F");
                public static System.Drawing.Color CACTUS_2 = ColorTranslator.FromHtml("#497811");
                public static System.Drawing.Color CLOUD = ColorTranslator.FromHtml("#FFFFFF");
                public static System.Drawing.Color GLOWING_MUSHROOM = ColorTranslator.FromHtml("#B6AF82");
                public static System.Drawing.Color LIVING_WOOD = ColorTranslator.FromHtml("#9E7354");
                public static System.Drawing.Color LEAVES = ColorTranslator.FromHtml("#0D6524");
                public static System.Drawing.Color SLIME = ColorTranslator.FromHtml("#3879FF");
                public static System.Drawing.Color BONE = ColorTranslator.FromHtml("#B2B28A");
                public static System.Drawing.Color FLESH = ColorTranslator.FromHtml("#B74D70");
                public static System.Drawing.Color RAIN_CLOUD = ColorTranslator.FromHtml("#9390B2");
                public static System.Drawing.Color FROZEN_SLIME = ColorTranslator.FromHtml("#61C8E1");
                public static System.Drawing.Color ASPHALT = ColorTranslator.FromHtml("#202122");
                public static System.Drawing.Color FLESH_GRASS = ColorTranslator.FromHtml("#9F3A3A");
                public static System.Drawing.Color RED_ICE = ColorTranslator.FromHtml("#E6BAB7");
                public static System.Drawing.Color FLESH_WEEDS = ColorTranslator.FromHtml("#A63F3F");
                public static System.Drawing.Color SUNPLATE = ColorTranslator.FromHtml("#171594");
                public static System.Drawing.Color CRIMSTONE = ColorTranslator.FromHtml("#C34343");
                public static System.Drawing.Color CRIMTANE = ColorTranslator.FromHtml("#85212E");
                public static System.Drawing.Color CRIMSTONE_VINES = ColorTranslator.FromHtml("#B74544");
                public static System.Drawing.Color ICE_BRICK = ColorTranslator.FromHtml("#7CAFC9");
                public static System.Drawing.Color PURE_WATER_FOUNTAIN = ColorTranslator.FromHtml("#838383");
                public static System.Drawing.Color SHADEWOOD = ColorTranslator.FromHtml("#687986");
                public static System.Drawing.Color CANNON = ColorTranslator.FromHtml("#676767");
                public static System.Drawing.Color LAND_MINE = ColorTranslator.FromHtml("#ED1C24");
                public static System.Drawing.Color CHLOROPHYTE_ORE = ColorTranslator.FromHtml("#4FBF2D");
                public static System.Drawing.Color SNOWBALL_LAUNCHER = ColorTranslator.FromHtml("#F5F5F5");
                public static System.Drawing.Color ROPE = ColorTranslator.FromHtml("#897843");
                public static System.Drawing.Color CHAIN = ColorTranslator.FromHtml("#676767");
                public static System.Drawing.Color CAMPFIRE = ColorTranslator.FromHtml("#FD3E03");
                public static System.Drawing.Color RED_ROCKET = ColorTranslator.FromHtml("#BE303E");
                public static System.Drawing.Color BLEND_O_MATIC = ColorTranslator.FromHtml("#676767");
                public static System.Drawing.Color MEAT_GRINDER = ColorTranslator.FromHtml("#4D4D4D");
                public static System.Drawing.Color SILT_EXTRACTINATOR = ColorTranslator.FromHtml("#676767");
                public static System.Drawing.Color SOLIDIFIER = ColorTranslator.FromHtml("#563A01");
                public static System.Drawing.Color PALLADIUM_ORE = ColorTranslator.FromHtml("#F35E36");
                public static System.Drawing.Color ORICHALCUM_ORE = ColorTranslator.FromHtml("#841380");
                public static System.Drawing.Color TITANIUM_ORE = ColorTranslator.FromHtml("#A7D29F");
                public static System.Drawing.Color SLUSH = ColorTranslator.FromHtml("#7E989D");
                public static System.Drawing.Color HIVE = ColorTranslator.FromHtml("#C86C10");
                public static System.Drawing.Color LIHZAHRD_BRICK = ColorTranslator.FromHtml("#8D3800");
                public static System.Drawing.Color DYE_FLOWERS = ColorTranslator.FromHtml("#46BB93");
                public static System.Drawing.Color DYE_VAT = ColorTranslator.FromHtml("#A87858");
                public static System.Drawing.Color HONEY = ColorTranslator.FromHtml("#FF9C0C");
                public static System.Drawing.Color CRISPY_HONEY = ColorTranslator.FromHtml("#5E3E24");
                public static System.Drawing.Color POD = ColorTranslator.FromHtml("#C8964A");
                public static System.Drawing.Color WOODEN_SPIKE = ColorTranslator.FromHtml("#734144");
                public static System.Drawing.Color PLANT = ColorTranslator.FromHtml("#6BB600");
                public static System.Drawing.Color CRIMSAND = ColorTranslator.FromHtml("#4D4C42");
                public static System.Drawing.Color TELEPORTER = ColorTranslator.FromHtml("#B3BB44");
                public static System.Drawing.Color LIFE_FRUIT = ColorTranslator.FromHtml("#CD733D");
                public static System.Drawing.Color LIHZAHRD_ALTAR = ColorTranslator.FromHtml("#FFF133");
                public static System.Drawing.Color PLANTERAS_BULB = ColorTranslator.FromHtml("#E180CE");
                public static System.Drawing.Color INGOTS = ColorTranslator.FromHtml("#CD8647");
                public static System.Drawing.Color PICTURE = ColorTranslator.FromHtml("#634732");
                public static System.Drawing.Color CATACOMB = ColorTranslator.FromHtml("#4D4A48");
                public static System.Drawing.Color PICTURE_2 = ColorTranslator.FromHtml("#454B45");
                public static System.Drawing.Color IMBUING_STATION = ColorTranslator.FromHtml("#C6C4AA");
                public static System.Drawing.Color BUBBLE_MACHINE = ColorTranslator.FromHtml("#C8F5FD");
                public static System.Drawing.Color PICTURE_3 = ColorTranslator.FromHtml("#554545");
                public static System.Drawing.Color PICTURE_4 = ColorTranslator.FromHtml("#6D5332");
                public static System.Drawing.Color AUTOHAMMER = ColorTranslator.FromHtml("#696969");
                public static System.Drawing.Color PALLADIUM_COLUMN = ColorTranslator.FromHtml("#E1623F");
                public static System.Drawing.Color BUBBLEGUM = ColorTranslator.FromHtml("#DF23DC");
                public static System.Drawing.Color TITANSTONE = ColorTranslator.FromHtml("#636169");
                public static System.Drawing.Color PUMPKIN = ColorTranslator.FromHtml("#FAAC0F");
                public static System.Drawing.Color HAY = ColorTranslator.FromHtml("#D7BA36");
                public static System.Drawing.Color SPOOKY_WOOD = ColorTranslator.FromHtml("#4F3E5C");
                public static System.Drawing.Color PUMPKIN_SEED = ColorTranslator.FromHtml("#1E9648");
                public static System.Drawing.Color AMETHYST_BLOCK = ColorTranslator.FromHtml("#6B319A");
                public static System.Drawing.Color TOPAZ_BLOCK = ColorTranslator.FromHtml("#9A9431");
                public static System.Drawing.Color SAPPHIRE_BLOCK = ColorTranslator.FromHtml("#31319A");
                public static System.Drawing.Color EMERALD_BLOCK = ColorTranslator.FromHtml("#319A44");
                public static System.Drawing.Color RUBY_BLOCK = ColorTranslator.FromHtml("#9A314D");
                public static System.Drawing.Color DIAMOND_BLOCK = ColorTranslator.FromHtml("#555976");
                public static System.Drawing.Color AMBER_BLOCK = ColorTranslator.FromHtml("#9A5331");
                public static System.Drawing.Color AMETHYST_GEMSPARK_BLOCK = ColorTranslator.FromHtml("#DD4FFF");
                public static System.Drawing.Color TOPAZ_GEMSPARK_BLOCK = ColorTranslator.FromHtml("#FAFF4F");
                public static System.Drawing.Color SAPPHIRE_GEMSPARK_BLOCK = ColorTranslator.FromHtml("#4F66FF");
                public static System.Drawing.Color EMERALD_GEMSPARK_BLOCK = ColorTranslator.FromHtml("#4FFF59");
                public static System.Drawing.Color RUBY_GEMSPARK_BLOCK = ColorTranslator.FromHtml("#FF4F4F");
                public static System.Drawing.Color DIAMOND_GEMSPARK_BLOCK = ColorTranslator.FromHtml("#F0F0F7");
                public static System.Drawing.Color AMBER_GEMSPARK_BLOCK = ColorTranslator.FromHtml("#FF914F");
                public static System.Drawing.Color WOMANNEQUIN = ColorTranslator.FromHtml("#906850");
                public static System.Drawing.Color FIREFLY_IN_A_BOTTLE = ColorTranslator.FromHtml("#3E6350");
                public static System.Drawing.Color LIGHTNING_BUG_IN_A_BOTTLE = ColorTranslator.FromHtml("#4BA8AF");
                public static System.Drawing.Color IRON_PLATING = ColorTranslator.FromHtml("#434343");
                public static System.Drawing.Color STONE_SLAB = ColorTranslator.FromHtml("#636363");
                public static System.Drawing.Color SANDSTONE_SLAB = ColorTranslator.FromHtml("#D3C66F");
                public static System.Drawing.Color BUNNY_CAGE = ColorTranslator.FromHtml("#3E7994");
                public static System.Drawing.Color SQUIRREL_CAGE = ColorTranslator.FromHtml("#3E7994");
                public static System.Drawing.Color MALLARD_DUCK_CAGE = ColorTranslator.FromHtml("#3E7994");
                public static System.Drawing.Color DUCK_CAGE = ColorTranslator.FromHtml("#3E7994");
                public static System.Drawing.Color BIRD_CAGE = ColorTranslator.FromHtml("#3E7994");
                public static System.Drawing.Color BLUE_JAY_CAGE = ColorTranslator.FromHtml("#3E7994");
                public static System.Drawing.Color CARDINAL_CAGE = ColorTranslator.FromHtml("#3E7994");
                public static System.Drawing.Color FISH_BOWL = ColorTranslator.FromHtml("#0839B5");
                public static System.Drawing.Color HEAVY_WORK_BENCH = ColorTranslator.FromHtml("#332E29");
                public static System.Drawing.Color COPPER_PLATING = ColorTranslator.FromHtml("#B75819");
                public static System.Drawing.Color SNAIL_CAGE = ColorTranslator.FromHtml("#3E7994");
                public static System.Drawing.Color GLOWING_SNAIL_CAGE = ColorTranslator.FromHtml("#3E7994");
                public static System.Drawing.Color AMMO_BOX = ColorTranslator.FromHtml("#40660D");
                public static System.Drawing.Color MONARCH_BUTTERFLY_JAR = ColorTranslator.FromHtml("#B7BBD8");
                public static System.Drawing.Color PURPLE_EMPEROR_BUTTERFLY_JAR = ColorTranslator.FromHtml("#B7BBD8");
                public static System.Drawing.Color RED_ADMIRAL_BUTTERFLY_JAR = ColorTranslator.FromHtml("#B7BBD8");
                public static System.Drawing.Color ULYSSES_BUTTERFLY_JAR = ColorTranslator.FromHtml("#B7BBD8");
                public static System.Drawing.Color SULPHUR_BUTTERFLY_JAR = ColorTranslator.FromHtml("#B7BBD8");
                public static System.Drawing.Color TREE_NYMPH_BUTTERFLY_JAR = ColorTranslator.FromHtml("#B7BBD8");
                public static System.Drawing.Color ZEBRA_SWALLOWTAIL_BUTTERFLY_JAR = ColorTranslator.FromHtml("#B7BBD8");
                public static System.Drawing.Color JULIA_BUTTERFLY_JAR = ColorTranslator.FromHtml("#B7BBD8");
                public static System.Drawing.Color SCORPION_CAGE = ColorTranslator.FromHtml("#3E7994");
                public static System.Drawing.Color BLACK_SCORPION_CAGE = ColorTranslator.FromHtml("#3E7994");
                public static System.Drawing.Color FROG_CAGE = ColorTranslator.FromHtml("#3E7994");
                public static System.Drawing.Color MOUSE_CAGE = ColorTranslator.FromHtml("#3E7994");
                public static System.Drawing.Color BONE_WELDER = ColorTranslator.FromHtml("#81815F");
                public static System.Drawing.Color FLESH_CLONING_VAT = ColorTranslator.FromHtml("#DEECAF");
                public static System.Drawing.Color GLASS_KILN = ColorTranslator.FromHtml("#C1CACB");
                public static System.Drawing.Color LIHZAHRD_FURNACE = ColorTranslator.FromHtml("#8D3800");
                public static System.Drawing.Color LIVING_LOOM = ColorTranslator.FromHtml("#345401");
                public static System.Drawing.Color SKY_MILL = ColorTranslator.FromHtml("#007FA6");
                public static System.Drawing.Color ICE_MACHINE = ColorTranslator.FromHtml("#687799");
                public static System.Drawing.Color STEAMPUNK_BOILER = ColorTranslator.FromHtml("#808080");
                public static System.Drawing.Color HONEY_DISPENSER = ColorTranslator.FromHtml("#73450C");
                public static System.Drawing.Color PENGUIN_CAGE = ColorTranslator.FromHtml("#3E7994");
                public static System.Drawing.Color WORM_CAGE = ColorTranslator.FromHtml("#3E7994");
                public static System.Drawing.Color DYNASTY_WOOD = ColorTranslator.FromHtml("#874C1F");
                public static System.Drawing.Color RED_DYNASTY_SHINGLES = ColorTranslator.FromHtml("#B4443E");
                public static System.Drawing.Color BLUE_DYNASTY_SHINGLES = ColorTranslator.FromHtml("#599BA8");

                //walls
                public static System.Drawing.Color STONE_WALL = ColorTranslator.FromHtml("#343434");
                public static System.Drawing.Color DIRT_WALL = ColorTranslator.FromHtml("#583D2E");
                public static System.Drawing.Color STONE_WALL_2 = ColorTranslator.FromHtml("#3D3A4E");
                public static System.Drawing.Color WOOD_WALL = ColorTranslator.FromHtml("#523C2D");
                public static System.Drawing.Color GRAY_BRICK_WALL = ColorTranslator.FromHtml("#3C3C3C");
                public static System.Drawing.Color RED_BRICK_WALL = ColorTranslator.FromHtml("#5B1E1E");
                public static System.Drawing.Color BLUE_BRICK_WALL = ColorTranslator.FromHtml("#3D4456");
                public static System.Drawing.Color GREEN_BRICK_WALL = ColorTranslator.FromHtml("#384147");
                public static System.Drawing.Color PINK_BRICK_WALL = ColorTranslator.FromHtml("#603E5C");
                public static System.Drawing.Color GOLD_BRICK_WALL = ColorTranslator.FromHtml("#64510A");
                public static System.Drawing.Color SILVER_BRICK_WALL = ColorTranslator.FromHtml("#616969");
                public static System.Drawing.Color COPPER_BRICK_WALL = ColorTranslator.FromHtml("#532E16");
                public static System.Drawing.Color HELLSTONE_BRICK_WALL = ColorTranslator.FromHtml("#492929");
                public static System.Drawing.Color OBSIDIAN_WALL = ColorTranslator.FromHtml("#020202");
                public static System.Drawing.Color MUD_WALL = ColorTranslator.FromHtml("#31282B");
                public static System.Drawing.Color DIRT_WALL_2 = ColorTranslator.FromHtml("#583D2E");
                public static System.Drawing.Color BLUE_BRICK_WALL_2 = ColorTranslator.FromHtml("#492929");
                public static System.Drawing.Color GREEN_BRICK_WALL_2 = ColorTranslator.FromHtml("#384147");
                public static System.Drawing.Color PINK_BRICK_WALL_2 = ColorTranslator.FromHtml("#603E5C");
                public static System.Drawing.Color OBSIDIAN_BRICK_WALL = ColorTranslator.FromHtml("#0F0F0F");
                public static System.Drawing.Color GLASS_WALL = ColorTranslator.FromHtml("#12242C");
                public static System.Drawing.Color PEARLSTONE_BRICK_WALL = ColorTranslator.FromHtml("#827C79");
                public static System.Drawing.Color IRIDESCENT_BRICK_WALL = ColorTranslator.FromHtml("#56494B");
                public static System.Drawing.Color MUDSTONE_BRICK_WALL = ColorTranslator.FromHtml("#403033");
                public static System.Drawing.Color COBALT_BRICK_WALL = ColorTranslator.FromHtml("#0B233E");
                public static System.Drawing.Color MYTHRIL_BRICK_WALL = ColorTranslator.FromHtml("#3C5B3A");
                public static System.Drawing.Color PLANKED_WALL = ColorTranslator.FromHtml("#3A291D");
                public static System.Drawing.Color PEARLSTONE_WALL = ColorTranslator.FromHtml("#515465");
                public static System.Drawing.Color CANDY_CANE_WALL = ColorTranslator.FromHtml("#581717");
                public static System.Drawing.Color GREEN_CANDY_CANE_WALL = ColorTranslator.FromHtml("#2C6A26");
                public static System.Drawing.Color SNOW_BRICK_WALL = ColorTranslator.FromHtml("#6B7577");
                public static System.Drawing.Color ADAMANTITE_BEAM_WALL = ColorTranslator.FromHtml("#5A122B");
                public static System.Drawing.Color DEMONITE_BRICK_WALL = ColorTranslator.FromHtml("#46455E");
                public static System.Drawing.Color SANDSTONE_BRICK_WALL = ColorTranslator.FromHtml("#6C6748");
                public static System.Drawing.Color EBONSTONE_BRICK_WALL = ColorTranslator.FromHtml("#333346");
                public static System.Drawing.Color RED_STUCCO_WALL = ColorTranslator.FromHtml("#704B46");
                public static System.Drawing.Color YELLOW_STUCCO_WALL = ColorTranslator.FromHtml("#574F30");
                public static System.Drawing.Color GREEN_STUCCO_WALL = ColorTranslator.FromHtml("#5A5D73");
                public static System.Drawing.Color GRAY_STUCCO_WALL = ColorTranslator.FromHtml("#6D6D6B");
                public static System.Drawing.Color SNOW_WALL = ColorTranslator.FromHtml("#577173");
                public static System.Drawing.Color EBONWOOD_WALL = ColorTranslator.FromHtml("#3A3944");
                public static System.Drawing.Color RICH_MAHOGANY_WALL = ColorTranslator.FromHtml("#4E1E20");
                public static System.Drawing.Color PEARLWOOD_WALL = ColorTranslator.FromHtml("#776C51");
                public static System.Drawing.Color RAINBOW_BRICK_WALL = ColorTranslator.FromHtml("#414141");
                public static System.Drawing.Color TIN_BRICK_WALL = ColorTranslator.FromHtml("#3C3B33");
                public static System.Drawing.Color TUNGSTEN_BRICK_WALL = ColorTranslator.FromHtml("#586758");
                public static System.Drawing.Color PLATINUM_BRICK_WALL = ColorTranslator.FromHtml("#666B75");
                public static System.Drawing.Color AMETHYST_WALL = ColorTranslator.FromHtml("#3C2452");
                public static System.Drawing.Color TOPAZ_WALL = ColorTranslator.FromHtml("#614B1C");
                public static System.Drawing.Color SAPPHIRE_WALL = ColorTranslator.FromHtml("#32527D");
                public static System.Drawing.Color EMERALD_WALL = ColorTranslator.FromHtml("#1C4924");
                public static System.Drawing.Color RUBY_WALL = ColorTranslator.FromHtml("#431D22");
                public static System.Drawing.Color DIAMOND_WALL = ColorTranslator.FromHtml("#234249");
                public static System.Drawing.Color MOSS_WALL = ColorTranslator.FromHtml("#304A42");
                public static System.Drawing.Color GREEN_MOSS_WALL = ColorTranslator.FromHtml("#4F4F33");
                public static System.Drawing.Color RED_MOSS_WALL = ColorTranslator.FromHtml("#4E3938");
                public static System.Drawing.Color BLUE_MOSS_WALL = ColorTranslator.FromHtml("#334452");
                public static System.Drawing.Color PURPLE_MOSS_WALL = ColorTranslator.FromHtml("#432F49");
                public static System.Drawing.Color ROCKY_DIRT_WALL = ColorTranslator.FromHtml("#583D2E");
                public static System.Drawing.Color SHRUB_WALL = ColorTranslator.FromHtml("#013E17");
                public static System.Drawing.Color ROCK_WALL = ColorTranslator.FromHtml("#362619");
                public static System.Drawing.Color SPIDER_CAVE_WALL = ColorTranslator.FromHtml("#242424");
                public static System.Drawing.Color SHRUB_WALL_2 = ColorTranslator.FromHtml("#235F39");
                public static System.Drawing.Color SHRUB_WALL_3 = ColorTranslator.FromHtml("#3F5F23");
                public static System.Drawing.Color SHRUB_WALL_4 = ColorTranslator.FromHtml("#1E5030");
                public static System.Drawing.Color GRASS_WALL = ColorTranslator.FromHtml("#1E5030");
                public static System.Drawing.Color JUNGLE_WALL = ColorTranslator.FromHtml("#35501E");
                public static System.Drawing.Color FLOWER_WALL = ColorTranslator.FromHtml("#1E5030");
                public static System.Drawing.Color SHRUB_WALL_5 = ColorTranslator.FromHtml("#333250");
                public static System.Drawing.Color SHRUB_WALL_6 = ColorTranslator.FromHtml("#143736");
                public static System.Drawing.Color ICE_WALL = ColorTranslator.FromHtml("#306085");
                public static System.Drawing.Color CACTUS_WALL = ColorTranslator.FromHtml("#34540C");
                public static System.Drawing.Color CLOUD_WALL = ColorTranslator.FromHtml("#E4ECEE");
                public static System.Drawing.Color MUSHROOM_WALL = ColorTranslator.FromHtml("#313B8C");
                public static System.Drawing.Color BONE_WALL = ColorTranslator.FromHtml("#63633C");
                public static System.Drawing.Color SLIME_WALL = ColorTranslator.FromHtml("#213C79");
                public static System.Drawing.Color FLESH_WALL = ColorTranslator.FromHtml("#3D0D10");
                public static System.Drawing.Color LIVING_WOOD_WALL = ColorTranslator.FromHtml("#513426");
                public static System.Drawing.Color CAVE_WALL = ColorTranslator.FromHtml("#332F60");
                public static System.Drawing.Color MUSHROOM_WALL_2 = ColorTranslator.FromHtml("#313B8C");
                public static System.Drawing.Color CLAY_WALL = ColorTranslator.FromHtml("#844545");
                public static System.Drawing.Color DISC_WALL = ColorTranslator.FromHtml("#4D4022");
                public static System.Drawing.Color HELLSTONE_WALL = ColorTranslator.FromHtml("#3F1C21");
                public static System.Drawing.Color ICE_BRICK_WALL = ColorTranslator.FromHtml("#596F72");
                public static System.Drawing.Color SHADEWOOD_WALL = ColorTranslator.FromHtml("#2A363F");
                public static System.Drawing.Color HIVE_WALL = ColorTranslator.FromHtml("#8C5131");
                public static System.Drawing.Color LIHZAHRD_BRICK_WALL = ColorTranslator.FromHtml("#320F08");
                public static System.Drawing.Color PURPLE_STAINED_GLASS = ColorTranslator.FromHtml("#6C3C82");
                public static System.Drawing.Color YELLOW_STAINED_GLASS = ColorTranslator.FromHtml("#787A45");
                public static System.Drawing.Color BLUE_STAINED_GLASS = ColorTranslator.FromHtml("#35498A");
                public static System.Drawing.Color GREEN_STAINED_GLASS = ColorTranslator.FromHtml("#45794F");
                public static System.Drawing.Color RED_STAINED_GLASS = ColorTranslator.FromHtml("#8A3535");
                public static System.Drawing.Color MULTICOLORED_STAINED_GLASS = ColorTranslator.FromHtml("#99255C");
                public static System.Drawing.Color SMALL_BLUE_BRICK_WALL = ColorTranslator.FromHtml("#303F46");
                public static System.Drawing.Color BLUE_BLOCK_WALL = ColorTranslator.FromHtml("#393643");
                public static System.Drawing.Color PINK_BLOCK_WALL = ColorTranslator.FromHtml("#593E5F");
                public static System.Drawing.Color SMALL_PINK_BRICK_WALL = ColorTranslator.FromHtml("#4E3245");
                public static System.Drawing.Color SMALL_GREEN_BRICK_WALL = ColorTranslator.FromHtml("#384745");
                public static System.Drawing.Color GREEN_BLOCK_WALL = ColorTranslator.FromHtml("#383E47");
                public static System.Drawing.Color BLUE_SLAB_WALL = ColorTranslator.FromHtml("#303F46");
                public static System.Drawing.Color BLUE_TILED_WALL = ColorTranslator.FromHtml("#393643");
                public static System.Drawing.Color PINK_SLAB_WALL = ColorTranslator.FromHtml("#48324D");
                public static System.Drawing.Color PINK_TILED_WALL = ColorTranslator.FromHtml("#4E3245");
                public static System.Drawing.Color GREEN_SLAB_WALL = ColorTranslator.FromHtml("#445747");
                public static System.Drawing.Color GREEN_TILED_WALL = ColorTranslator.FromHtml("#383E47");
                public static System.Drawing.Color WOODEN_FENCE = ColorTranslator.FromHtml("#604438");
                public static System.Drawing.Color METAL_FENCE = ColorTranslator.FromHtml("#3C3C3C");
                public static System.Drawing.Color HIVE_WALL_2 = ColorTranslator.FromHtml("#8C5131");
                public static System.Drawing.Color PALLADIUM_COLUMN_WALL = ColorTranslator.FromHtml("#5E1911");
                public static System.Drawing.Color BUBBLEGUM_WALL = ColorTranslator.FromHtml("#994996");
                public static System.Drawing.Color TITANSTONE_WALL = ColorTranslator.FromHtml("#1F1814");
                public static System.Drawing.Color LIHZAHRD_BRICK_WALL_2 = ColorTranslator.FromHtml("#320F08");
                public static System.Drawing.Color PUMPKIN_WALL = ColorTranslator.FromHtml("#803700");
                public static System.Drawing.Color HAY_WALL = ColorTranslator.FromHtml("#745E19");
                public static System.Drawing.Color SPOOKY_WOOD_WALL = ColorTranslator.FromHtml("#33243F");
                public static System.Drawing.Color CHRISTMAS_TREE_WALLPAPER = ColorTranslator.FromHtml("#5A1B1C");
                public static System.Drawing.Color ORNAMENT_WALLPAPER = ColorTranslator.FromHtml("#7D7A71");
                public static System.Drawing.Color CANDY_CANE_WALLPAPER = ColorTranslator.FromHtml("#073112");
                public static System.Drawing.Color FESTIVE_WALLPAPER = ColorTranslator.FromHtml("#69181B");
                public static System.Drawing.Color STARS_WALLPAPER = ColorTranslator.FromHtml("#111F41");
                public static System.Drawing.Color SQUIGGLES_WALLPAPER = ColorTranslator.FromHtml("#C6C3B7");
                public static System.Drawing.Color SNOWFLAKE_WALLPAPER = ColorTranslator.FromHtml("#6E6E7E");
                public static System.Drawing.Color KRAMPUS_HORN_WALLPAPER = ColorTranslator.FromHtml("#85755F");
                public static System.Drawing.Color BLUEGREEN_WALLPAPER = ColorTranslator.FromHtml("#100340");
                public static System.Drawing.Color GRINCH_FINGER_WALLPAPER = ColorTranslator.FromHtml("#748352");
                public static System.Drawing.Color FANCY_GREY_WALLPAPER = ColorTranslator.FromHtml("#8F919D");
                public static System.Drawing.Color ICE_FLOE_WALLPAPER = ColorTranslator.FromHtml("#8CB9E2");
                public static System.Drawing.Color MUSIC_WALLPAPER = ColorTranslator.FromHtml("#6F1975");
                public static System.Drawing.Color PURPLE_RAIN_WALLPAPER = ColorTranslator.FromHtml("#311B91");
                public static System.Drawing.Color RAINBOW_WALLPAPER = ColorTranslator.FromHtml("#6505C1");
                public static System.Drawing.Color SPARKLE_STONE_WALLPAPER = ColorTranslator.FromHtml("#575E7D");
                public static System.Drawing.Color STARLIT_HEAVEN_WALLPAPER = ColorTranslator.FromHtml("#060606");
                public static System.Drawing.Color BUBBLE_WALLPAPER = ColorTranslator.FromHtml("#4D3FA9");
                public static System.Drawing.Color COPPER_PIPE_WALLPAPER = ColorTranslator.FromHtml("#C25C17");
                public static System.Drawing.Color DUCKY_WALLPAPER = ColorTranslator.FromHtml("#177FA8");
                public static System.Drawing.Color WATERFALL_WALL = ColorTranslator.FromHtml("#285697");
                public static System.Drawing.Color LAVAFALL_WALL = ColorTranslator.FromHtml("#B7210F");
                public static System.Drawing.Color EBONWOOD_FENCE = ColorTranslator.FromHtml("#9989A5");
                public static System.Drawing.Color RICH_MAHOGANY_FENCE = ColorTranslator.FromHtml("#A36368");
                public static System.Drawing.Color PEARLWOOD_FENCE = ColorTranslator.FromHtml("#776C51");
                public static System.Drawing.Color SHADEWOOD_FENCE = ColorTranslator.FromHtml("#586976");
                public static System.Drawing.Color WHITE_DYNASTY_WALL = ColorTranslator.FromHtml("#E0DACC");
                public static System.Drawing.Color BLUE_DYNASTY_WALL = ColorTranslator.FromHtml("#516863");
                public static System.Drawing.Color ARCANE_RUNE_WALL = ColorTranslator.FromHtml("#3E1C57");

                //global
                public static System.Drawing.Color SKY = ColorTranslator.FromHtml("#84AAF8");
                public static System.Drawing.Color EARTH = ColorTranslator.FromHtml("#583D2E");
                public static System.Drawing.Color ROCK = ColorTranslator.FromHtml("#4A433C");
                public static System.Drawing.Color HELL = ColorTranslator.FromHtml("#000000");
                public static System.Drawing.Color LAVA = ColorTranslator.FromHtml("#FD2003");
                public static System.Drawing.Color WATER = ColorTranslator.FromHtml("#093DBF");

                //TODO?
                public static System.Drawing.Color LIQUID2 = ColorTranslator.FromHtml("#3B1D83");
                public static System.Drawing.Color LIQUID3 = ColorTranslator.FromHtml("#07918E");
                public static System.Drawing.Color LIQUID4 = ColorTranslator.FromHtml("#AB0BD1");
                public static System.Drawing.Color LIQUID5 = ColorTranslator.FromHtml("#0989BF");
                public static System.Drawing.Color LIQUID6 = ColorTranslator.FromHtml("#A86A20");
                public static System.Drawing.Color LIQUID7 = ColorTranslator.FromHtml("#243C94");
                public static System.Drawing.Color LIQUID8 = ColorTranslator.FromHtml("#413B65");
                public static System.Drawing.Color LIQUID9 = ColorTranslator.FromHtml("#C80000");
                public static System.Drawing.Color LIQUID10 = ColorTranslator.FromHtml("#B1364F");
            }
        }

        public void InitializeMapperDefs2() //Credits go to the authors of MoreTerra
        {
            ColorDefs = new Dictionary<int, System.Drawing.Color>(595 + Main.maxTilesY);

            //tiles
            ColorDefs[0] = Constants.Terrafirma_Color.DIRT;
            ColorDefs[1] = Constants.Terrafirma_Color.STONE;
            ColorDefs[2] = Constants.Terrafirma_Color.GRASS;
            ColorDefs[3] = Constants.Terrafirma_Color.WEED;
            ColorDefs[4] = Constants.Terrafirma_Color.TORCH;
            ColorDefs[5] = Constants.Terrafirma_Color.TREE;
            ColorDefs[6] = Constants.Terrafirma_Color.IRON_ORE;
            ColorDefs[7] = Constants.Terrafirma_Color.COPPER_ORE;
            ColorDefs[8] = Constants.Terrafirma_Color.GOLD_ORE;
            ColorDefs[9] = Constants.Terrafirma_Color.SILVER_ORE;
            ColorDefs[10] = Constants.Terrafirma_Color.CLOSED_DOORS;
            ColorDefs[11] = Constants.Terrafirma_Color.OPEN_DOORS;
            ColorDefs[12] = Constants.Terrafirma_Color.HEARTSTONE;
            ColorDefs[13] = Constants.Terrafirma_Color.BOTTLE;
            ColorDefs[14] = Constants.Terrafirma_Color.TABLES;
            ColorDefs[15] = Constants.Terrafirma_Color.CHAIRS;
            ColorDefs[16] = Constants.Terrafirma_Color.IRON_ANVIL;
            ColorDefs[17] = Constants.Terrafirma_Color.FURNACE;
            ColorDefs[18] = Constants.Terrafirma_Color.WORK_BENCH;
            ColorDefs[19] = Constants.Terrafirma_Color.WOOD_PLATFORM;
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
            ColorDefs[35] = Constants.Terrafirma_Color.JACK_O_LANTERN;
            ColorDefs[36] = Constants.Terrafirma_Color.PRESENT;
            ColorDefs[37] = Constants.Terrafirma_Color.METEORITE;
            ColorDefs[38] = Constants.Terrafirma_Color.GRAY_BRICK;
            ColorDefs[39] = Constants.Terrafirma_Color.RED_BRICK;
            ColorDefs[40] = Constants.Terrafirma_Color.CLAY;
            ColorDefs[41] = Constants.Terrafirma_Color.BLUE_BRICK;
            ColorDefs[42] = Constants.Terrafirma_Color.CHAIN_LANTERN;
            ColorDefs[43] = Constants.Terrafirma_Color.GREEN_BRICK;
            ColorDefs[44] = Constants.Terrafirma_Color.PINK_BRICK;
            ColorDefs[45] = Constants.Terrafirma_Color.GOLD_BRICK;
            ColorDefs[46] = Constants.Terrafirma_Color.SILVER_BRICK;
            ColorDefs[47] = Constants.Terrafirma_Color.COPPER_BRICK;
            ColorDefs[48] = Constants.Terrafirma_Color.SPIKE;
            ColorDefs[49] = Constants.Terrafirma_Color.WATER_CANDLE;
            ColorDefs[50] = Constants.Terrafirma_Color.BOOK;
            ColorDefs[51] = Constants.Terrafirma_Color.COBWEB;
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
            ColorDefs[82] = Constants.Terrafirma_Color.HERB_SPROUT;
            ColorDefs[83] = Constants.Terrafirma_Color.HERB;
            ColorDefs[84] = Constants.Terrafirma_Color.HERB_BLOSSOM;
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
            ColorDefs[106] = Constants.Terrafirma_Color.SAWMILL;
            ColorDefs[107] = Constants.Terrafirma_Color.COBALT_ORE;
            ColorDefs[108] = Constants.Terrafirma_Color.MYTHRIL_ORE;
            ColorDefs[109] = Constants.Terrafirma_Color.HALLOWED_GRASS;
            ColorDefs[110] = Constants.Terrafirma_Color.WEEDS;
            ColorDefs[111] = Constants.Terrafirma_Color.ADAMANTITE_ORE;
            ColorDefs[112] = Constants.Terrafirma_Color.EBONSAND;
            ColorDefs[113] = Constants.Terrafirma_Color.WEEDS_2;
            ColorDefs[114] = Constants.Terrafirma_Color.TINKERERS_WORKSHOP;
            ColorDefs[115] = Constants.Terrafirma_Color.VINES_2;
            ColorDefs[116] = Constants.Terrafirma_Color.PEARLSAND;
            ColorDefs[117] = Constants.Terrafirma_Color.PEARLSTONE;
            ColorDefs[118] = Constants.Terrafirma_Color.PEARLSTONE_BRICK;
            ColorDefs[119] = Constants.Terrafirma_Color.IRIDESCENT_BRICK;
            ColorDefs[120] = Constants.Terrafirma_Color.MUDSTONE;
            ColorDefs[121] = Constants.Terrafirma_Color.COBALT_BRICK;
            ColorDefs[122] = Constants.Terrafirma_Color.MYTHRIL_BRICK;
            ColorDefs[123] = Constants.Terrafirma_Color.SILT;
            ColorDefs[124] = Constants.Terrafirma_Color.WOODEN_BEAM;
            ColorDefs[125] = Constants.Terrafirma_Color.CRYSTAL_BALL;
            ColorDefs[126] = Constants.Terrafirma_Color.DISCO_BALL;
            ColorDefs[127] = Constants.Terrafirma_Color.GLASS_2;
            ColorDefs[128] = Constants.Terrafirma_Color.MANNEQUIN;
            ColorDefs[129] = Constants.Terrafirma_Color.CRYSTAL_SHARD;
            ColorDefs[130] = Constants.Terrafirma_Color.ACTIVE_STONE;
            ColorDefs[131] = Constants.Terrafirma_Color.INACTIVE_STONE;
            ColorDefs[132] = Constants.Terrafirma_Color.LEVER;
            ColorDefs[133] = Constants.Terrafirma_Color.ADAMANTITE_FORGE;
            ColorDefs[134] = Constants.Terrafirma_Color.MYTHRIL_ANVIL;
            ColorDefs[135] = Constants.Terrafirma_Color.PRESSURE_PLATE;
            ColorDefs[136] = Constants.Terrafirma_Color.SWITCH;
            ColorDefs[137] = Constants.Terrafirma_Color.DART_TRAP;
            ColorDefs[138] = Constants.Terrafirma_Color.BOULDER;
            ColorDefs[139] = Constants.Terrafirma_Color.MUSIC_BOX;
            ColorDefs[140] = Constants.Terrafirma_Color.DEMONITE_BRICK;
            ColorDefs[141] = Constants.Terrafirma_Color.EXPLOSIVES;
            ColorDefs[142] = Constants.Terrafirma_Color.INLET_PUMP;
            ColorDefs[143] = Constants.Terrafirma_Color.OUTLET_PUMP;
            ColorDefs[144] = Constants.Terrafirma_Color.TIMER;
            ColorDefs[145] = Constants.Terrafirma_Color.CANDY_CANE;
            ColorDefs[146] = Constants.Terrafirma_Color.GREEN_CANDY_CANE;
            ColorDefs[147] = Constants.Terrafirma_Color.SNOW;
            ColorDefs[148] = Constants.Terrafirma_Color.SNOW_BRICK;
            ColorDefs[149] = Constants.Terrafirma_Color.LIGHTS;
            ColorDefs[150] = Constants.Terrafirma_Color.ADAMANTITE_BEAM;
            ColorDefs[151] = Constants.Terrafirma_Color.SANDSTONE_BRICK;
            ColorDefs[152] = Constants.Terrafirma_Color.EBONSTONE_BRICK;
            ColorDefs[153] = Constants.Terrafirma_Color.RED_STUCCO;
            ColorDefs[154] = Constants.Terrafirma_Color.YELLOW_STUCCO;
            ColorDefs[155] = Constants.Terrafirma_Color.GREEN_STUCCO;
            ColorDefs[156] = Constants.Terrafirma_Color.GRAY_STUCCO;
            ColorDefs[157] = Constants.Terrafirma_Color.EBONWOOD;
            ColorDefs[158] = Constants.Terrafirma_Color.RICH_MAHOGANY;
            ColorDefs[159] = Constants.Terrafirma_Color.PEARLWOOD;
            ColorDefs[160] = Constants.Terrafirma_Color.RAINBOW_BRICK;
            ColorDefs[161] = Constants.Terrafirma_Color.ICE;
            ColorDefs[162] = Constants.Terrafirma_Color.BREAKABLE_ICE;
            ColorDefs[163] = Constants.Terrafirma_Color.PURPLE_ICE;
            ColorDefs[164] = Constants.Terrafirma_Color.PINK_ICE;
            ColorDefs[165] = Constants.Terrafirma_Color.STALAGTITE;
            ColorDefs[166] = Constants.Terrafirma_Color.TIN_ORE;
            ColorDefs[167] = Constants.Terrafirma_Color.LEAD_ORE;
            ColorDefs[168] = Constants.Terrafirma_Color.TUNGSTEN_ORE;
            ColorDefs[169] = Constants.Terrafirma_Color.PLATINUM_ORE;
            ColorDefs[170] = Constants.Terrafirma_Color.PINE_TREE_BLOCK;
            ColorDefs[171] = Constants.Terrafirma_Color.CHRISTMAS_TREE;
            ColorDefs[172] = Constants.Terrafirma_Color.HAUNTED_PLATINUM_CHANDELIER;
            ColorDefs[173] = Constants.Terrafirma_Color.PLATINUM_CANDELABRA;
            ColorDefs[174] = Constants.Terrafirma_Color.PLATINUM_CANDLE;
            ColorDefs[175] = Constants.Terrafirma_Color.TIN_BRICK;
            ColorDefs[176] = Constants.Terrafirma_Color.TUNGSTEN_BRICK;
            ColorDefs[177] = Constants.Terrafirma_Color.PLATINUM_BRICK;
            ColorDefs[178] = Constants.Terrafirma_Color.GEM;
            ColorDefs[179] = Constants.Terrafirma_Color.MOSS;
            ColorDefs[180] = Constants.Terrafirma_Color.BROWN_MOSS;
            ColorDefs[181] = Constants.Terrafirma_Color.RED_MOSS;
            ColorDefs[182] = Constants.Terrafirma_Color.BLUE_MOSS;
            ColorDefs[183] = Constants.Terrafirma_Color.PURPLE_MOSS;
            ColorDefs[184] = Constants.Terrafirma_Color.FLOWERS;
            ColorDefs[185] = Constants.Terrafirma_Color.RUBBLE;
            ColorDefs[186] = Constants.Terrafirma_Color.RUBBLE_2;
            ColorDefs[187] = Constants.Terrafirma_Color.RUBBLE_3;
            ColorDefs[188] = Constants.Terrafirma_Color.CACTUS_2;
            ColorDefs[189] = Constants.Terrafirma_Color.CLOUD;
            ColorDefs[190] = Constants.Terrafirma_Color.GLOWING_MUSHROOM;
            ColorDefs[191] = Constants.Terrafirma_Color.LIVING_WOOD;
            ColorDefs[192] = Constants.Terrafirma_Color.LEAVES;
            ColorDefs[193] = Constants.Terrafirma_Color.SLIME;
            ColorDefs[194] = Constants.Terrafirma_Color.BONE;
            ColorDefs[195] = Constants.Terrafirma_Color.FLESH;
            ColorDefs[196] = Constants.Terrafirma_Color.RAIN_CLOUD;
            ColorDefs[197] = Constants.Terrafirma_Color.FROZEN_SLIME;
            ColorDefs[198] = Constants.Terrafirma_Color.ASPHALT;
            ColorDefs[199] = Constants.Terrafirma_Color.FLESH_GRASS;
            ColorDefs[200] = Constants.Terrafirma_Color.RED_ICE;
            ColorDefs[201] = Constants.Terrafirma_Color.FLESH_WEEDS;
            ColorDefs[202] = Constants.Terrafirma_Color.SUNPLATE;
            ColorDefs[203] = Constants.Terrafirma_Color.CRIMSTONE;
            ColorDefs[204] = Constants.Terrafirma_Color.CRIMTANE;
            ColorDefs[205] = Constants.Terrafirma_Color.CRIMSTONE_VINES;
            ColorDefs[206] = Constants.Terrafirma_Color.ICE_BRICK;
            ColorDefs[207] = Constants.Terrafirma_Color.PURE_WATER_FOUNTAIN;
            ColorDefs[208] = Constants.Terrafirma_Color.SHADEWOOD;
            ColorDefs[209] = Constants.Terrafirma_Color.CANNON;
            ColorDefs[210] = Constants.Terrafirma_Color.LAND_MINE;
            ColorDefs[211] = Constants.Terrafirma_Color.CHLOROPHYTE_ORE;
            ColorDefs[212] = Constants.Terrafirma_Color.SNOWBALL_LAUNCHER;
            ColorDefs[213] = Constants.Terrafirma_Color.ROPE;
            ColorDefs[214] = Constants.Terrafirma_Color.CHAIN;
            ColorDefs[215] = Constants.Terrafirma_Color.CAMPFIRE;
            ColorDefs[216] = Constants.Terrafirma_Color.RED_ROCKET;
            ColorDefs[217] = Constants.Terrafirma_Color.BLEND_O_MATIC;
            ColorDefs[218] = Constants.Terrafirma_Color.MEAT_GRINDER;
            ColorDefs[219] = Constants.Terrafirma_Color.SILT_EXTRACTINATOR;
            ColorDefs[220] = Constants.Terrafirma_Color.SOLIDIFIER;
            ColorDefs[221] = Constants.Terrafirma_Color.PALLADIUM_ORE;
            ColorDefs[222] = Constants.Terrafirma_Color.ORICHALCUM_ORE;
            ColorDefs[223] = Constants.Terrafirma_Color.TITANIUM_ORE;
            ColorDefs[224] = Constants.Terrafirma_Color.SLUSH;
            ColorDefs[225] = Constants.Terrafirma_Color.HIVE;
            ColorDefs[226] = Constants.Terrafirma_Color.LIHZAHRD_BRICK;
            ColorDefs[227] = Constants.Terrafirma_Color.DYE_FLOWERS;
            ColorDefs[228] = Constants.Terrafirma_Color.DYE_VAT;
            ColorDefs[229] = Constants.Terrafirma_Color.HONEY;
            ColorDefs[230] = Constants.Terrafirma_Color.CRISPY_HONEY;
            ColorDefs[231] = Constants.Terrafirma_Color.POD;
            ColorDefs[232] = Constants.Terrafirma_Color.WOODEN_SPIKE;
            ColorDefs[233] = Constants.Terrafirma_Color.PLANT;
            ColorDefs[234] = Constants.Terrafirma_Color.CRIMSAND;
            ColorDefs[235] = Constants.Terrafirma_Color.TELEPORTER;
            ColorDefs[236] = Constants.Terrafirma_Color.LIFE_FRUIT;
            ColorDefs[237] = Constants.Terrafirma_Color.LIHZAHRD_ALTAR;
            ColorDefs[238] = Constants.Terrafirma_Color.PLANTERAS_BULB;
            ColorDefs[239] = Constants.Terrafirma_Color.INGOTS;
            ColorDefs[240] = Constants.Terrafirma_Color.PICTURE;
            ColorDefs[241] = Constants.Terrafirma_Color.CATACOMB;
            ColorDefs[242] = Constants.Terrafirma_Color.PICTURE_2;
            ColorDefs[243] = Constants.Terrafirma_Color.IMBUING_STATION;
            ColorDefs[244] = Constants.Terrafirma_Color.BUBBLE_MACHINE;
            ColorDefs[245] = Constants.Terrafirma_Color.PICTURE_3;
            ColorDefs[246] = Constants.Terrafirma_Color.PICTURE_4;
            ColorDefs[247] = Constants.Terrafirma_Color.AUTOHAMMER;
            ColorDefs[248] = Constants.Terrafirma_Color.PALLADIUM_COLUMN;
            ColorDefs[249] = Constants.Terrafirma_Color.BUBBLEGUM;
            ColorDefs[250] = Constants.Terrafirma_Color.TITANSTONE;
            ColorDefs[251] = Constants.Terrafirma_Color.PUMPKIN;
            ColorDefs[252] = Constants.Terrafirma_Color.HAY;
            ColorDefs[253] = Constants.Terrafirma_Color.SPOOKY_WOOD;
            ColorDefs[254] = Constants.Terrafirma_Color.PUMPKIN_SEED;
            ColorDefs[255] = Constants.Terrafirma_Color.AMETHYST_BLOCK;
            ColorDefs[256] = Constants.Terrafirma_Color.TOPAZ_BLOCK;
            ColorDefs[257] = Constants.Terrafirma_Color.SAPPHIRE_BLOCK;
            ColorDefs[258] = Constants.Terrafirma_Color.EMERALD_BLOCK;
            ColorDefs[259] = Constants.Terrafirma_Color.RUBY_BLOCK;
            ColorDefs[260] = Constants.Terrafirma_Color.DIAMOND_BLOCK;
            ColorDefs[261] = Constants.Terrafirma_Color.AMBER_BLOCK;
            ColorDefs[262] = Constants.Terrafirma_Color.AMETHYST_GEMSPARK_BLOCK;
            ColorDefs[263] = Constants.Terrafirma_Color.TOPAZ_GEMSPARK_BLOCK;
            ColorDefs[264] = Constants.Terrafirma_Color.SAPPHIRE_GEMSPARK_BLOCK;
            ColorDefs[265] = Constants.Terrafirma_Color.EMERALD_GEMSPARK_BLOCK;
            ColorDefs[266] = Constants.Terrafirma_Color.RUBY_GEMSPARK_BLOCK;
            ColorDefs[267] = Constants.Terrafirma_Color.DIAMOND_GEMSPARK_BLOCK;
            ColorDefs[268] = Constants.Terrafirma_Color.AMBER_GEMSPARK_BLOCK;
            ColorDefs[269] = Constants.Terrafirma_Color.WOMANNEQUIN;
            ColorDefs[270] = Constants.Terrafirma_Color.FIREFLY_IN_A_BOTTLE;
            ColorDefs[271] = Constants.Terrafirma_Color.LIGHTNING_BUG_IN_A_BOTTLE;
            ColorDefs[272] = Constants.Terrafirma_Color.IRON_PLATING;
            ColorDefs[273] = Constants.Terrafirma_Color.STONE_SLAB;
            ColorDefs[274] = Constants.Terrafirma_Color.SANDSTONE_SLAB;
            ColorDefs[275] = Constants.Terrafirma_Color.BUNNY_CAGE;
            ColorDefs[276] = Constants.Terrafirma_Color.SQUIRREL_CAGE;
            ColorDefs[277] = Constants.Terrafirma_Color.MALLARD_DUCK_CAGE;
            ColorDefs[278] = Constants.Terrafirma_Color.DUCK_CAGE;
            ColorDefs[279] = Constants.Terrafirma_Color.BIRD_CAGE;
            ColorDefs[280] = Constants.Terrafirma_Color.BLUE_JAY_CAGE;
            ColorDefs[281] = Constants.Terrafirma_Color.CARDINAL_CAGE;
            ColorDefs[282] = Constants.Terrafirma_Color.FISH_BOWL;
            ColorDefs[283] = Constants.Terrafirma_Color.HEAVY_WORK_BENCH;
            ColorDefs[284] = Constants.Terrafirma_Color.COPPER_PLATING;
            ColorDefs[285] = Constants.Terrafirma_Color.SNAIL_CAGE;
            ColorDefs[286] = Constants.Terrafirma_Color.GLOWING_SNAIL_CAGE;
            ColorDefs[287] = Constants.Terrafirma_Color.AMMO_BOX;
            ColorDefs[288] = Constants.Terrafirma_Color.MONARCH_BUTTERFLY_JAR;
            ColorDefs[289] = Constants.Terrafirma_Color.PURPLE_EMPEROR_BUTTERFLY_JAR;
            ColorDefs[290] = Constants.Terrafirma_Color.RED_ADMIRAL_BUTTERFLY_JAR;
            ColorDefs[291] = Constants.Terrafirma_Color.ULYSSES_BUTTERFLY_JAR;
            ColorDefs[292] = Constants.Terrafirma_Color.SULPHUR_BUTTERFLY_JAR;
            ColorDefs[293] = Constants.Terrafirma_Color.TREE_NYMPH_BUTTERFLY_JAR;
            ColorDefs[294] = Constants.Terrafirma_Color.ZEBRA_SWALLOWTAIL_BUTTERFLY_JAR;
            ColorDefs[295] = Constants.Terrafirma_Color.JULIA_BUTTERFLY_JAR;
            ColorDefs[296] = Constants.Terrafirma_Color.SCORPION_CAGE;
            ColorDefs[297] = Constants.Terrafirma_Color.BLACK_SCORPION_CAGE;
            ColorDefs[298] = Constants.Terrafirma_Color.FROG_CAGE;
            ColorDefs[299] = Constants.Terrafirma_Color.MOUSE_CAGE;
            ColorDefs[300] = Constants.Terrafirma_Color.BONE_WELDER;
            ColorDefs[301] = Constants.Terrafirma_Color.FLESH_CLONING_VAT;
            ColorDefs[302] = Constants.Terrafirma_Color.GLASS_KILN;
            ColorDefs[303] = Constants.Terrafirma_Color.LIHZAHRD_FURNACE;
            ColorDefs[304] = Constants.Terrafirma_Color.LIVING_LOOM;
            ColorDefs[305] = Constants.Terrafirma_Color.SKY_MILL;
            ColorDefs[306] = Constants.Terrafirma_Color.ICE_MACHINE;
            ColorDefs[307] = Constants.Terrafirma_Color.STEAMPUNK_BOILER;
            ColorDefs[308] = Constants.Terrafirma_Color.HONEY_DISPENSER;
            ColorDefs[309] = Constants.Terrafirma_Color.PENGUIN_CAGE;
            ColorDefs[310] = Constants.Terrafirma_Color.WORM_CAGE;
            ColorDefs[311] = Constants.Terrafirma_Color.DYNASTY_WOOD;
            ColorDefs[312] = Constants.Terrafirma_Color.RED_DYNASTY_SHINGLES;
            ColorDefs[313] = Constants.Terrafirma_Color.BLUE_DYNASTY_SHINGLES;

            for (int i = 314; i < 451; i++)
            {
                ColorDefs[i] = System.Drawing.Color.Magenta;
            }

            //walls
            ColorDefs[451] = Constants.Terrafirma_Color.STONE_WALL;
            ColorDefs[452] = Constants.Terrafirma_Color.DIRT_WALL;
            ColorDefs[453] = Constants.Terrafirma_Color.STONE_WALL_2;
            ColorDefs[454] = Constants.Terrafirma_Color.WOOD_WALL;
            ColorDefs[455] = Constants.Terrafirma_Color.GRAY_BRICK_WALL;
            ColorDefs[456] = Constants.Terrafirma_Color.RED_BRICK_WALL;
            ColorDefs[457] = Constants.Terrafirma_Color.BLUE_BRICK_WALL;
            ColorDefs[458] = Constants.Terrafirma_Color.GREEN_BRICK_WALL;
            ColorDefs[459] = Constants.Terrafirma_Color.PINK_BRICK_WALL;
            ColorDefs[460] = Constants.Terrafirma_Color.GOLD_BRICK_WALL;
            ColorDefs[461] = Constants.Terrafirma_Color.SILVER_BRICK_WALL;
            ColorDefs[462] = Constants.Terrafirma_Color.COPPER_BRICK_WALL;
            ColorDefs[463] = Constants.Terrafirma_Color.HELLSTONE_BRICK_WALL;
            ColorDefs[464] = Constants.Terrafirma_Color.OBSIDIAN_WALL;
            ColorDefs[465] = Constants.Terrafirma_Color.MUD_WALL;
            ColorDefs[466] = Constants.Terrafirma_Color.DIRT_WALL_2;
            ColorDefs[467] = Constants.Terrafirma_Color.BLUE_BRICK_WALL_2;
            ColorDefs[468] = Constants.Terrafirma_Color.GREEN_BRICK_WALL_2;
            ColorDefs[469] = Constants.Terrafirma_Color.PINK_BRICK_WALL_2;
            ColorDefs[470] = Constants.Terrafirma_Color.OBSIDIAN_BRICK_WALL;
            ColorDefs[471] = Constants.Terrafirma_Color.GLASS_WALL;
            ColorDefs[472] = Constants.Terrafirma_Color.PEARLSTONE_BRICK_WALL;
            ColorDefs[473] = Constants.Terrafirma_Color.IRIDESCENT_BRICK_WALL;
            ColorDefs[474] = Constants.Terrafirma_Color.MUDSTONE_BRICK_WALL;
            ColorDefs[475] = Constants.Terrafirma_Color.COBALT_BRICK_WALL;
            ColorDefs[476] = Constants.Terrafirma_Color.MYTHRIL_BRICK_WALL;
            ColorDefs[477] = Constants.Terrafirma_Color.PLANKED_WALL;
            ColorDefs[478] = Constants.Terrafirma_Color.PEARLSTONE_WALL;
            ColorDefs[479] = Constants.Terrafirma_Color.CANDY_CANE_WALL;
            ColorDefs[480] = Constants.Terrafirma_Color.GREEN_CANDY_CANE_WALL;
            ColorDefs[481] = Constants.Terrafirma_Color.SNOW_BRICK_WALL;
            ColorDefs[482] = Constants.Terrafirma_Color.ADAMANTITE_BEAM_WALL;
            ColorDefs[483] = Constants.Terrafirma_Color.DEMONITE_BRICK_WALL;
            ColorDefs[484] = Constants.Terrafirma_Color.SANDSTONE_BRICK_WALL;
            ColorDefs[485] = Constants.Terrafirma_Color.EBONSTONE_BRICK_WALL;
            ColorDefs[486] = Constants.Terrafirma_Color.RED_STUCCO_WALL;
            ColorDefs[487] = Constants.Terrafirma_Color.YELLOW_STUCCO_WALL;
            ColorDefs[488] = Constants.Terrafirma_Color.GREEN_STUCCO_WALL;
            ColorDefs[489] = Constants.Terrafirma_Color.GRAY_STUCCO_WALL;
            ColorDefs[490] = Constants.Terrafirma_Color.SNOW_WALL;
            ColorDefs[491] = Constants.Terrafirma_Color.EBONWOOD_WALL;
            ColorDefs[492] = Constants.Terrafirma_Color.RICH_MAHOGANY_WALL;
            ColorDefs[493] = Constants.Terrafirma_Color.PEARLWOOD_WALL;
            ColorDefs[494] = Constants.Terrafirma_Color.RAINBOW_BRICK_WALL;
            ColorDefs[495] = Constants.Terrafirma_Color.TIN_BRICK_WALL;
            ColorDefs[496] = Constants.Terrafirma_Color.TUNGSTEN_BRICK_WALL;
            ColorDefs[497] = Constants.Terrafirma_Color.PLATINUM_BRICK_WALL;
            ColorDefs[498] = Constants.Terrafirma_Color.AMETHYST_WALL;
            ColorDefs[499] = Constants.Terrafirma_Color.TOPAZ_WALL;
            ColorDefs[500] = Constants.Terrafirma_Color.SAPPHIRE_WALL;
            ColorDefs[501] = Constants.Terrafirma_Color.EMERALD_WALL;
            ColorDefs[502] = Constants.Terrafirma_Color.RUBY_WALL;
            ColorDefs[503] = Constants.Terrafirma_Color.DIAMOND_WALL;
            ColorDefs[504] = Constants.Terrafirma_Color.MOSS_WALL;
            ColorDefs[505] = Constants.Terrafirma_Color.GREEN_MOSS_WALL;
            ColorDefs[506] = Constants.Terrafirma_Color.RED_MOSS_WALL;
            ColorDefs[507] = Constants.Terrafirma_Color.BLUE_MOSS_WALL;
            ColorDefs[508] = Constants.Terrafirma_Color.PURPLE_MOSS_WALL;
            ColorDefs[509] = Constants.Terrafirma_Color.ROCKY_DIRT_WALL;
            ColorDefs[510] = Constants.Terrafirma_Color.SHRUB_WALL;
            ColorDefs[511] = Constants.Terrafirma_Color.ROCK_WALL;
            ColorDefs[512] = Constants.Terrafirma_Color.SPIDER_CAVE_WALL;
            ColorDefs[513] = Constants.Terrafirma_Color.SHRUB_WALL_2;
            ColorDefs[514] = Constants.Terrafirma_Color.SHRUB_WALL_3;
            ColorDefs[515] = Constants.Terrafirma_Color.SHRUB_WALL_4;
            ColorDefs[516] = Constants.Terrafirma_Color.GRASS_WALL;
            ColorDefs[517] = Constants.Terrafirma_Color.JUNGLE_WALL;
            ColorDefs[518] = Constants.Terrafirma_Color.FLOWER_WALL;
            ColorDefs[519] = Constants.Terrafirma_Color.SHRUB_WALL_5;
            ColorDefs[520] = Constants.Terrafirma_Color.SHRUB_WALL_6;
            ColorDefs[521] = Constants.Terrafirma_Color.ICE_WALL;
            ColorDefs[522] = Constants.Terrafirma_Color.CACTUS_WALL;
            ColorDefs[523] = Constants.Terrafirma_Color.CLOUD_WALL;
            ColorDefs[524] = Constants.Terrafirma_Color.MUSHROOM_WALL;
            ColorDefs[525] = Constants.Terrafirma_Color.BONE_WALL;
            ColorDefs[526] = Constants.Terrafirma_Color.SLIME_WALL;
            ColorDefs[527] = Constants.Terrafirma_Color.FLESH_WALL;
            ColorDefs[528] = Constants.Terrafirma_Color.LIVING_WOOD_WALL;
            ColorDefs[529] = Constants.Terrafirma_Color.CAVE_WALL;
            ColorDefs[530] = Constants.Terrafirma_Color.MUSHROOM_WALL_2;
            ColorDefs[531] = Constants.Terrafirma_Color.CLAY_WALL;
            ColorDefs[532] = Constants.Terrafirma_Color.DISC_WALL;
            ColorDefs[533] = Constants.Terrafirma_Color.HELLSTONE_WALL;
            ColorDefs[534] = Constants.Terrafirma_Color.ICE_BRICK_WALL;
            ColorDefs[535] = Constants.Terrafirma_Color.SHADEWOOD_WALL;
            ColorDefs[536] = Constants.Terrafirma_Color.HIVE_WALL;
            ColorDefs[537] = Constants.Terrafirma_Color.LIHZAHRD_BRICK_WALL;
            ColorDefs[538] = Constants.Terrafirma_Color.PURPLE_STAINED_GLASS;
            ColorDefs[539] = Constants.Terrafirma_Color.YELLOW_STAINED_GLASS;
            ColorDefs[540] = Constants.Terrafirma_Color.BLUE_STAINED_GLASS;
            ColorDefs[541] = Constants.Terrafirma_Color.GREEN_STAINED_GLASS;
            ColorDefs[542] = Constants.Terrafirma_Color.RED_STAINED_GLASS;
            ColorDefs[543] = Constants.Terrafirma_Color.MULTICOLORED_STAINED_GLASS;
            ColorDefs[544] = Constants.Terrafirma_Color.SMALL_BLUE_BRICK_WALL;
            ColorDefs[545] = Constants.Terrafirma_Color.BLUE_BLOCK_WALL;
            ColorDefs[546] = Constants.Terrafirma_Color.PINK_BLOCK_WALL;
            ColorDefs[547] = Constants.Terrafirma_Color.SMALL_PINK_BRICK_WALL;
            ColorDefs[548] = Constants.Terrafirma_Color.SMALL_GREEN_BRICK_WALL;
            ColorDefs[549] = Constants.Terrafirma_Color.GREEN_BLOCK_WALL;
            ColorDefs[550] = Constants.Terrafirma_Color.BLUE_SLAB_WALL;
            ColorDefs[551] = Constants.Terrafirma_Color.BLUE_TILED_WALL;
            ColorDefs[552] = Constants.Terrafirma_Color.PINK_SLAB_WALL;
            ColorDefs[553] = Constants.Terrafirma_Color.PINK_TILED_WALL;
            ColorDefs[554] = Constants.Terrafirma_Color.GREEN_SLAB_WALL;
            ColorDefs[555] = Constants.Terrafirma_Color.GREEN_TILED_WALL;
            ColorDefs[556] = Constants.Terrafirma_Color.WOODEN_FENCE;
            ColorDefs[557] = Constants.Terrafirma_Color.METAL_FENCE;
            ColorDefs[558] = Constants.Terrafirma_Color.HIVE_WALL_2;
            ColorDefs[559] = Constants.Terrafirma_Color.PALLADIUM_COLUMN_WALL;
            ColorDefs[560] = Constants.Terrafirma_Color.BUBBLEGUM_WALL;
            ColorDefs[561] = Constants.Terrafirma_Color.TITANSTONE_WALL;
            ColorDefs[562] = Constants.Terrafirma_Color.LIHZAHRD_BRICK_WALL_2;
            ColorDefs[563] = Constants.Terrafirma_Color.PUMPKIN_WALL;
            ColorDefs[564] = Constants.Terrafirma_Color.HAY_WALL;
            ColorDefs[565] = Constants.Terrafirma_Color.SPOOKY_WOOD_WALL;
            ColorDefs[566] = Constants.Terrafirma_Color.CHRISTMAS_TREE_WALLPAPER;
            ColorDefs[567] = Constants.Terrafirma_Color.ORNAMENT_WALLPAPER;
            ColorDefs[568] = Constants.Terrafirma_Color.CANDY_CANE_WALLPAPER;
            ColorDefs[569] = Constants.Terrafirma_Color.FESTIVE_WALLPAPER;
            ColorDefs[570] = Constants.Terrafirma_Color.STARS_WALLPAPER;
            ColorDefs[571] = Constants.Terrafirma_Color.SQUIGGLES_WALLPAPER;
            ColorDefs[572] = Constants.Terrafirma_Color.SNOWFLAKE_WALLPAPER;
            ColorDefs[573] = Constants.Terrafirma_Color.KRAMPUS_HORN_WALLPAPER;
            ColorDefs[574] = Constants.Terrafirma_Color.BLUEGREEN_WALLPAPER;
            ColorDefs[575] = Constants.Terrafirma_Color.GRINCH_FINGER_WALLPAPER;
            ColorDefs[576] = Constants.Terrafirma_Color.FANCY_GREY_WALLPAPER;
            ColorDefs[577] = Constants.Terrafirma_Color.ICE_FLOE_WALLPAPER;
            ColorDefs[578] = Constants.Terrafirma_Color.MUSIC_WALLPAPER;
            ColorDefs[579] = Constants.Terrafirma_Color.PURPLE_RAIN_WALLPAPER;
            ColorDefs[580] = Constants.Terrafirma_Color.RAINBOW_WALLPAPER;
            ColorDefs[581] = Constants.Terrafirma_Color.SPARKLE_STONE_WALLPAPER;
            ColorDefs[582] = Constants.Terrafirma_Color.STARLIT_HEAVEN_WALLPAPER;
            ColorDefs[583] = Constants.Terrafirma_Color.BUBBLE_WALLPAPER;
            ColorDefs[584] = Constants.Terrafirma_Color.COPPER_PIPE_WALLPAPER;
            ColorDefs[585] = Constants.Terrafirma_Color.DUCKY_WALLPAPER;
            ColorDefs[586] = Constants.Terrafirma_Color.WATERFALL_WALL;
            ColorDefs[587] = Constants.Terrafirma_Color.LAVAFALL_WALL;
            ColorDefs[588] = Constants.Terrafirma_Color.EBONWOOD_FENCE;
            ColorDefs[589] = Constants.Terrafirma_Color.RICH_MAHOGANY_FENCE;
            ColorDefs[590] = Constants.Terrafirma_Color.PEARLWOOD_FENCE;
            ColorDefs[591] = Constants.Terrafirma_Color.SHADEWOOD_FENCE;
            ColorDefs[592] = Constants.Terrafirma_Color.WHITE_DYNASTY_WALL;
            ColorDefs[593] = Constants.Terrafirma_Color.BLUE_DYNASTY_WALL;
            ColorDefs[594] = Constants.Terrafirma_Color.ARCANE_RUNE_WALL;

            // this is for faster performace
            // rather than converting from Color to UInt32 alot.
            UInt32Defs = new Dictionary<int, UInt32>(595 + Main.maxTilesY);

            //adds sky and earth

            for (int i = 595; i < Main.worldSurface + 595; i++)
            {
                UInt32Defs[i] = 0x84AAF8;
                ColorDefs[i] = Constants.Terrafirma_Color.SKY;
            }
            for (int i = (int)Main.worldSurface + 595; i < (int)Main.rockLayer + 595; i++)
            {
                UInt32Defs[i] = 0x583D2E;
                ColorDefs[i] = Constants.Terrafirma_Color.EARTH;
            }
            for (int i = (int)Main.rockLayer + 595; i < Main.maxTilesY + 595; i++)
            {
                UInt32Defs[i] = 0x000000;
                ColorDefs[i] = Constants.Terrafirma_Color.HELL;
            }

            //adds the background fade in both ColorDefs and UInt32Defs
            for (int y = (int)Main.rockLayer; y < Main.maxTilesY; y++)
            {
                double alpha = (double)(y - Main.rockLayer) / (double)(Main.maxTilesY - Main.rockLayer);
                UInt32 c = alphaBlend(0x4A433C, 0x000000, alpha);   // (rockcolor, hellcolor, alpha)
                UInt32Defs[y + 595] = c;
                ColorDefs[y + 595] = toColor(c);
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
            UInt32Defs[35] = 0xEAAD53;
            UInt32Defs[36] = 0xC22E32;
            UInt32Defs[37] = 0x685654;
            UInt32Defs[38] = 0x8C8C8C;
            UInt32Defs[39] = 0xC37057;
            UInt32Defs[40] = 0x925144;
            UInt32Defs[41] = 0x454E63;
            UInt32Defs[42] = 0xF99851;
            UInt32Defs[43] = 0x526556;
            UInt32Defs[44] = 0x8A4469;
            UInt32Defs[45] = 0x947E18;
            UInt32Defs[46] = 0xAEC1C2;
            UInt32Defs[47] = 0xD5651A;
            UInt32Defs[48] = 0xAFAFAF;
            UInt32Defs[49] = 0x0B2EFF;
            UInt32Defs[50] = 0x3095AA;
            UInt32Defs[51] = 0x9EADAE;
            UInt32Defs[52] = 0x1E9648;
            UInt32Defs[53] = 0xD3C66F;
            UInt32Defs[54] = 0xC8F6FE;
            UInt32Defs[55] = 0x7F5C45;
            UInt32Defs[56] = 0x41414D;
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
            UInt32Defs[75] = 0x0B0B0B;
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
            UInt32Defs[106] = 0x563E2C;
            UInt32Defs[107] = 0x0B508F;
            UInt32Defs[108] = 0x5BA9A9;
            UInt32Defs[109] = 0x4EC1E3;
            UInt32Defs[110] = 0x1E9648;
            UInt32Defs[111] = 0x801A34;
            UInt32Defs[112] = 0x67627A;
            UInt32Defs[113] = 0x1E9648;
            UInt32Defs[114] = 0x7F5C45;
            UInt32Defs[115] = 0x327FA1;
            UInt32Defs[116] = 0xD5C4C5;
            UInt32Defs[117] = 0xB5ACBE;
            UInt32Defs[118] = 0xD5C4C5;
            UInt32Defs[119] = 0x3F3F49;
            UInt32Defs[120] = 0x967A7D;
            UInt32Defs[121] = 0x2576AB;
            UInt32Defs[122] = 0x91BF75;
            UInt32Defs[123] = 0x595353;
            UInt32Defs[124] = 0x5C4436;
            UInt32Defs[125] = 0x81A5FF;
            UInt32Defs[126] = 0xDBDBDB;
            UInt32Defs[127] = 0x68B3C8;
            UInt32Defs[128] = 0x906850;
            UInt32Defs[129] = 0x004979;
            UInt32Defs[130] = 0xA5A5A5;
            UInt32Defs[131] = 0x1A1A1A;
            UInt32Defs[132] = 0xC90303;
            UInt32Defs[133] = 0x891012;
            UInt32Defs[134] = 0x96AE87;
            UInt32Defs[135] = 0xFD7272;
            UInt32Defs[136] = 0xCCC0C0;
            UInt32Defs[137] = 0x8C8C8C;
            UInt32Defs[138] = 0x636363;
            UInt32Defs[139] = 0x996343;
            UInt32Defs[140] = 0x7875B3;
            UInt32Defs[141] = 0xAD2323;
            UInt32Defs[142] = 0xC90303;
            UInt32Defs[143] = 0xC90303;
            UInt32Defs[144] = 0xC90303;
            UInt32Defs[145] = 0xC01E1E;
            UInt32Defs[146] = 0x2BC01E;
            UInt32Defs[147] = 0xC7DCDF;
            UInt32Defs[148] = 0xD3ECF1;
            UInt32Defs[149] = 0xffffff;
            UInt32Defs[150] = 0x731736;
            UInt32Defs[151] = 0xbaa854;
            UInt32Defs[152] = 0x5f5f95;
            UInt32Defs[153] = 0xef8d7e;
            UInt32Defs[154] = 0xdfdb93;
            UInt32Defs[155] = 0x83a2a1;
            UInt32Defs[156] = 0xbcbcb1;
            UInt32Defs[157] = 0x9989a5;
            UInt32Defs[158] = 0x915155;
            UInt32Defs[159] = 0x57503f;
            UInt32Defs[160] = 0xd4d4d4;
            UInt32Defs[161] = 0x90c3e8;
            UInt32Defs[162] = 0x92aebf;
            UInt32Defs[163] = 0x9188cb;
            UInt32Defs[164] = 0xd197bc;
            UInt32Defs[165] = 0x5c8faf;
            UInt32Defs[166] = 0x817d5d;
            UInt32Defs[167] = 0x2f3e57;
            UInt32Defs[168] = 0x5a7d53;
            UInt32Defs[169] = 0x8097b8;
            UInt32Defs[170] = 0x003F2C;
            UInt32Defs[171] = 0x269660;
            UInt32Defs[172] = 0x4A3D26;
            UInt32Defs[173] = 0x8097b8;
            UInt32Defs[174] = 0xfe7902;
            UInt32Defs[175] = 0xbba57c;
            UInt32Defs[176] = 0x9cc09d;
            UInt32Defs[177] = 0xb5c2d9;
            UInt32Defs[178] = 0x892880;
            UInt32Defs[179] = 0x318672;
            UInt32Defs[180] = 0x7e8631;
            UInt32Defs[181] = 0x863b31;
            UInt32Defs[182] = 0x2b568c;
            UInt32Defs[183] = 0x793186;
            UInt32Defs[184] = 0x208376;
            UInt32Defs[185] = 0x808080;
            UInt32Defs[186] = 0x999979;
            UInt32Defs[187] = 0x63971f;
            UInt32Defs[188] = 0x497811;
            UInt32Defs[189] = 0xffffff;
            UInt32Defs[190] = 0xb6af82;
            UInt32Defs[191] = 0x9e7354;
            UInt32Defs[192] = 0x0d6524;
            UInt32Defs[193] = 0x3879ff;
            UInt32Defs[194] = 0xb2b28a;
            UInt32Defs[195] = 0xb74d70;
            UInt32Defs[196] = 0x9390b2;
            UInt32Defs[197] = 0x61c8e1;
            UInt32Defs[198] = 0x202122;
            UInt32Defs[199] = 0x9f3a3a;
            UInt32Defs[200] = 0xe6bab7;
            UInt32Defs[201] = 0xa63f3f;
            UInt32Defs[202] = 0x171594;
            UInt32Defs[203] = 0xc34343;
            UInt32Defs[204] = 0x85212e;
            UInt32Defs[205] = 0xb74544;
            UInt32Defs[206] = 0xcafc9;
            UInt32Defs[207] = 0x838383;
            UInt32Defs[208] = 0x687986;
            UInt32Defs[209] = 0x676767;
            UInt32Defs[210] = 0xed1c24;
            UInt32Defs[211] = 0x4fbf2d;
            UInt32Defs[212] = 0xf5f5f5;
            UInt32Defs[213] = 0x897843;
            UInt32Defs[214] = 0x676767;
            UInt32Defs[215] = 0xfd3e03;
            UInt32Defs[216] = 0xbe303e;
            UInt32Defs[217] = 0x676767;
            UInt32Defs[218] = 0x4d4d4d;
            UInt32Defs[219] = 0x676767;
            UInt32Defs[220] = 0x563a01;
            UInt32Defs[221] = 0xf35e36;
            UInt32Defs[222] = 0x841380;
            UInt32Defs[223] = 0xa7d29f;
            UInt32Defs[224] = 0x7e989d;
            UInt32Defs[225] = 0xc86c10;
            UInt32Defs[226] = 0x8d3800;
            UInt32Defs[227] = 0x46bb93;
            UInt32Defs[228] = 0xa87858;
            UInt32Defs[229] = 0xff9c0c;
            UInt32Defs[230] = 0x5e3e24;
            UInt32Defs[231] = 0xc8964a;
            UInt32Defs[232] = 0x734144;
            UInt32Defs[233] = 0x6bb600;
            UInt32Defs[234] = 0x4d4c42;
            UInt32Defs[235] = 0xb3bb44;
            UInt32Defs[236] = 0xcd733d;
            UInt32Defs[237] = 0xfff133;
            UInt32Defs[238] = 0xe180ce;
            UInt32Defs[239] = 0xcd8647;
            UInt32Defs[240] = 0x634732;
            UInt32Defs[241] = 0x4d4a48;
            UInt32Defs[242] = 0x454b45;
            UInt32Defs[243] = 0xc6c4aa;
            UInt32Defs[244] = 0xc8f5fd;
            UInt32Defs[245] = 0x554545;
            UInt32Defs[246] = 0x6d5332;
            UInt32Defs[247] = 0x696969;
            UInt32Defs[248] = 0xe1623f;
            UInt32Defs[249] = 0xdf23dc;
            UInt32Defs[250] = 0x636169;
            UInt32Defs[251] = 0xFAAC0F;
            UInt32Defs[252] = 0xD7BA36;
            UInt32Defs[253] = 0x4F3E5C;
            UInt32Defs[254] = 0x1E9648;
            UInt32Defs[255] = 0x6B319A;
            UInt32Defs[256] = 0x9A9431;
            UInt32Defs[257] = 0x31319A;
            UInt32Defs[258] = 0x319A44;
            UInt32Defs[259] = 0x9A314D;
            UInt32Defs[260] = 0x555976;
            UInt32Defs[261] = 0x9A5331;
            UInt32Defs[262] = 0xDD4FFF;
            UInt32Defs[263] = 0xFAFF4F;
            UInt32Defs[264] = 0x4F66FF;
            UInt32Defs[265] = 0x4FFF59;
            UInt32Defs[266] = 0xFF4F4F;
            UInt32Defs[267] = 0xF0F0F7;
            UInt32Defs[268] = 0xFF914F;
            UInt32Defs[269] = 0x906850;
            UInt32Defs[270] = 0x3E6350;
            UInt32Defs[271] = 0x4BA8AF;
            UInt32Defs[272] = 0x434343;
            UInt32Defs[273] = 0x636363;
            UInt32Defs[274] = 0xD3C66F;
            UInt32Defs[275] = 0x3E7994;
            UInt32Defs[276] = 0x3E7994;
            UInt32Defs[277] = 0x3E7994;
            UInt32Defs[278] = 0x3E7994;
            UInt32Defs[279] = 0x3E7994;
            UInt32Defs[280] = 0x3E7994;
            UInt32Defs[281] = 0x3E7994;
            UInt32Defs[282] = 0x0839B5;
            UInt32Defs[283] = 0x332E29;
            UInt32Defs[284] = 0xB75819;
            UInt32Defs[285] = 0x3E7994;
            UInt32Defs[286] = 0x3E7994;
            UInt32Defs[287] = 0x40660D;
            UInt32Defs[288] = 0xB7BBD8;
            UInt32Defs[289] = 0xB7BBD8;
            UInt32Defs[290] = 0xB7BBD8;
            UInt32Defs[291] = 0xB7BBD8;
            UInt32Defs[292] = 0xB7BBD8;
            UInt32Defs[293] = 0xB7BBD8;
            UInt32Defs[294] = 0xB7BBD8;
            UInt32Defs[295] = 0xB7BBD8;
            UInt32Defs[296] = 0x3E7994;
            UInt32Defs[297] = 0x3E7994;
            UInt32Defs[298] = 0x3E7994;
            UInt32Defs[299] = 0x3E7994;
            UInt32Defs[300] = 0x81815F;
            UInt32Defs[301] = 0xDEECAF;
            UInt32Defs[302] = 0xC1CACB;
            UInt32Defs[303] = 0x8D3800;
            UInt32Defs[304] = 0x345401;
            UInt32Defs[305] = 0x007FA6;
            UInt32Defs[306] = 0x687799;
            UInt32Defs[307] = 0x808080;
            UInt32Defs[308] = 0x73450C;
            UInt32Defs[309] = 0x3E7994;
            UInt32Defs[310] = 0x3E7994;
            UInt32Defs[311] = 0x874C1F;
            UInt32Defs[312] = 0xB4443E;
            UInt32Defs[313] = 0x599BA8;

            // unknown
            for (int i = 314; i < 451; i++)
            {
                UInt32Defs[i] = 0xFF00FF;
            }

            //walls
            UInt32Defs[451] = 0x343434;
            UInt32Defs[452] = 0x583D2E;
            UInt32Defs[453] = 0x3D3A4E;
            UInt32Defs[454] = 0x523C2D;
            UInt32Defs[455] = 0x3C3C3C;
            UInt32Defs[456] = 0x5B1E1E;
            UInt32Defs[457] = 0x3D4456;
            UInt32Defs[458] = 0x384147;
            UInt32Defs[459] = 0x603E5C;
            UInt32Defs[460] = 0x64510A;
            UInt32Defs[461] = 0x616969;
            UInt32Defs[462] = 0x532E16;
            UInt32Defs[463] = 0x492929;
            UInt32Defs[464] = 0x020202;
            UInt32Defs[465] = 0x31282B;
            UInt32Defs[466] = 0x583D2E;
            UInt32Defs[467] = 0x492929;
            UInt32Defs[468] = 0x384147;
            UInt32Defs[469] = 0x603E5C;
            UInt32Defs[470] = 0x0F0F0F;
            UInt32Defs[471] = 0x12242C;
            UInt32Defs[472] = 0x827C79;
            UInt32Defs[473] = 0x56494B;
            UInt32Defs[474] = 0x403033;
            UInt32Defs[475] = 0x0B233E;
            UInt32Defs[476] = 0x3C5B3A;
            UInt32Defs[477] = 0x3A291D;
            UInt32Defs[478] = 0x515465;
            UInt32Defs[479] = 0x581717;
            UInt32Defs[480] = 0x2C6A26;
            UInt32Defs[481] = 0x6B7577;
            UInt32Defs[482] = 0x5a122b;
            UInt32Defs[483] = 0x46455e;
            UInt32Defs[484] = 0x6c6748;
            UInt32Defs[485] = 0x333346;
            UInt32Defs[486] = 0x704b46;
            UInt32Defs[487] = 0x574f30;
            UInt32Defs[488] = 0x5a5d73;
            UInt32Defs[489] = 0x6d6d6b;
            UInt32Defs[490] = 0x577173;
            UInt32Defs[491] = 0x3a3944;
            UInt32Defs[492] = 0x4e1e20;
            UInt32Defs[493] = 0x776c51;
            UInt32Defs[494] = 0x414141;
            UInt32Defs[495] = 0x3c3b33;
            UInt32Defs[496] = 0x586758;
            UInt32Defs[497] = 0x666b75;
            UInt32Defs[498] = 0x3c2452;
            UInt32Defs[499] = 0x614b1c;
            UInt32Defs[500] = 0x32527d;
            UInt32Defs[501] = 0x1c4924;
            UInt32Defs[502] = 0x431d22;
            UInt32Defs[503] = 0x234249;
            UInt32Defs[504] = 0x304a42;
            UInt32Defs[505] = 0x4f4f33;
            UInt32Defs[506] = 0x4e3938;
            UInt32Defs[507] = 0x334452;
            UInt32Defs[508] = 0x432f49;
            UInt32Defs[509] = 0x83d2e;
            UInt32Defs[510] = 0x013e17;
            UInt32Defs[511] = 0x362619;
            UInt32Defs[512] = 0x242424;
            UInt32Defs[513] = 0x235f39;
            UInt32Defs[514] = 0x3f5f23;
            UInt32Defs[515] = 0x1e5030;
            UInt32Defs[516] = 0x1e5030;
            UInt32Defs[517] = 0x35501e;
            UInt32Defs[518] = 0x1e5030;
            UInt32Defs[519] = 0x333250;
            UInt32Defs[520] = 0x143736;
            UInt32Defs[521] = 0x306085;
            UInt32Defs[522] = 0x34540c;
            UInt32Defs[523] = 0xe4ecee;
            UInt32Defs[524] = 0x313b8c;
            UInt32Defs[525] = 0x63633c;
            UInt32Defs[526] = 0x213c79;
            UInt32Defs[527] = 0x3d0d10;
            UInt32Defs[528] = 0x513426;
            UInt32Defs[529] = 0x332f60;
            UInt32Defs[530] = 0x313b8c;
            UInt32Defs[531] = 0x844545;
            UInt32Defs[532] = 0x4d4022;
            UInt32Defs[533] = 0x3f1c21;
            UInt32Defs[534] = 0x596f72;
            UInt32Defs[535] = 0x2a363f;
            UInt32Defs[536] = 0x8c5131;
            UInt32Defs[537] = 0x320f08;
            UInt32Defs[538] = 0x6c3c82;
            UInt32Defs[539] = 0x787a45;
            UInt32Defs[540] = 0x35498a;
            UInt32Defs[541] = 0x45794f;
            UInt32Defs[542] = 0x8a3535;
            UInt32Defs[543] = 0x99255c;
            UInt32Defs[544] = 0x303f46;
            UInt32Defs[545] = 0x393643;
            UInt32Defs[546] = 0x593e5f;
            UInt32Defs[547] = 0x4e3245;
            UInt32Defs[548] = 0x384745;
            UInt32Defs[549] = 0x383e47;
            UInt32Defs[550] = 0x303f46;
            UInt32Defs[551] = 0x393643;
            UInt32Defs[552] = 0x48324d;
            UInt32Defs[553] = 0x4e3245;
            UInt32Defs[554] = 0x445747;
            UInt32Defs[555] = 0x383e47;
            UInt32Defs[556] = 0x604438;
            UInt32Defs[557] = 0x3c3c3c;
            UInt32Defs[558] = 0x8c5131;
            UInt32Defs[559] = 0x5e1911;
            UInt32Defs[560] = 0x994996;
            UInt32Defs[561] = 0x1f1814;
            UInt32Defs[562] = 0x320F08;
            UInt32Defs[563] = 0x803700;
            UInt32Defs[564] = 0x745E19;
            UInt32Defs[565] = 0x33243F;
            UInt32Defs[566] = 0x5A1B1C;
            UInt32Defs[567] = 0x7D7A71;
            UInt32Defs[568] = 0x073112;
            UInt32Defs[569] = 0x69181B;
            UInt32Defs[570] = 0x111F41;
            UInt32Defs[571] = 0xC6C3B7;
            UInt32Defs[572] = 0x6E6E7E;
            UInt32Defs[573] = 0x85755F;
            UInt32Defs[574] = 0x100340;
            UInt32Defs[575] = 0x748352;
            UInt32Defs[576] = 0x8F919D;
            UInt32Defs[577] = 0x8CB9E2;
            UInt32Defs[578] = 0x6F1975;
            UInt32Defs[579] = 0x311B91;
            UInt32Defs[580] = 0x6505C1;
            UInt32Defs[581] = 0x575E7D;
            UInt32Defs[582] = 0x060606;
            UInt32Defs[583] = 0x4D3FA9;
            UInt32Defs[584] = 0xC25C17;
            UInt32Defs[585] = 0x177FA8;
            UInt32Defs[586] = 0x285697;
            UInt32Defs[587] = 0xB7210F;
            UInt32Defs[588] = 0x9989A5;
            UInt32Defs[589] = 0xA36368;
            UInt32Defs[590] = 0x776C51;
            UInt32Defs[591] = 0x586976;
            UInt32Defs[592] = 0xE0DACC;
            UInt32Defs[593] = 0x516863;
            UInt32Defs[594] = 0x3E1C57;

            //list for when dimming the world for highlighting
            DimColorDefs = new Dictionary<int, System.Drawing.Color>(595 + Main.maxTilesY);
            DimUInt32Defs = new Dictionary<int, UInt32>(595 + Main.maxTilesY);
        }
    }
}
