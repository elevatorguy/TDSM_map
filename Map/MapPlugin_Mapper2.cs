using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using Terraria;
using Color = Microsoft.Xna.Framework.Color;

namespace Map
{
    public partial class MapPlugin
    {
        const int TILE_END_INDEX = 469;
        const int WALL_START_INDEX = 1000;
        const int WALL_END_INDEX = 1229;
        const int FADE_START_INDEX = WALL_END_INDEX + 1;

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
        public static Dictionary<UInt32, System.Drawing.Color> honeyblendlist = new Dictionary<UInt32, System.Drawing.Color>();
        //better to have a separate list for dim liquid lists
        public static Dictionary<UInt32, System.Drawing.Color> waterdimlist = new Dictionary<UInt32, System.Drawing.Color>();
        public static Dictionary<UInt32, System.Drawing.Color> lavadimlist = new Dictionary<UInt32, System.Drawing.Color>();
        public static Dictionary<UInt32, System.Drawing.Color> honeydimlist = new Dictionary<UInt32, System.Drawing.Color>();

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
            if (MapPlugin.bmp != null)
            {
                MapPlugin.bmp.Dispose();
                MapPlugin.bmp = null;
            }
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
            if (!api_call)
            {
                utils.SendLogs("Saving Image...", Color.WhiteSmoke);
                stopwatch.Start();
            }

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

                //if has hell
                if (state != 2 && state != 3 && state != 6)
                {
                    int y = 0;
                    //this fades the background from rock to hell
                    try
                    {
                        System.Drawing.Color dimColor;
                        for (y = (int)(Main.rockLayer - y1); (y + y1) < y2; y++)
                        {
                            dimColor = dimC(UInt32Defs[FADE_START_INDEX + (y + y1)]);
                            graphicsHandle.DrawLine(new Pen(dimColor), 0, y, bmp.Width, y);
                        }
                    }
                    catch (KeyNotFoundException e)
                    {
                        utils.SendLogs("<map> ERROR: could not fade the dimmed background from rock to hell.", Color.Red);
                        utils.SendLogs(e.StackTrace.ToString() + ", with key " + (FADE_START_INDEX + (y + y1)).ToString(), Color.WhiteSmoke);
                        //continue and see if we keep getting errors when painting the actual world
                    }
                }
            }
            else
            {
                int y = 0;
                int state = 2;
                try
                {
                    state = paintbackground(Constants.Terrafirma_Color.SKY, Constants.Terrafirma_Color.EARTH, Constants.Terrafirma_Color.HELL);
                }
                catch (KeyNotFoundException e)
                {
                    utils.SendLogs("<map> ERROR: could not paint the background.", Color.Red);
                    utils.SendLogs(e.StackTrace.ToString(), Color.WhiteSmoke);
                }

                try
                {
                    //if has hell
                    if (state != 2 && state != 3 && state != 6)
                    {
                        //this fades the background from rock to hell
                        for (y = (int)(Main.rockLayer - y1); (y + y1) < y2; y++)
                        {
                            graphicsHandle.DrawLine(new Pen(ColorDefs[FADE_START_INDEX + (y + y1)]), 0, y, bmp.Width, y);
                        }
                    }
                }
                catch (KeyNotFoundException e)
                {
                    utils.SendLogs("<map> ERROR: could not fade the background from rock to hell.", Color.Red);
                    utils.SendLogs(e.StackTrace.ToString() + ", with key " + (FADE_START_INDEX + (y + y1)).ToString(), Color.WhiteSmoke);

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
                part1.IsBackground = true;
                part2.IsBackground = true;
                part3.IsBackground = true;
                part4.IsBackground = true;
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
            part1.Join();
            part2.Join();
            part3.Join();
            part4.Join();

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

            if (pixelfailureflag)
            {
                utils.SendLogs("<map> WARNING: pixel fail write on hlchests.", Color.Yellow);
                pixelfailureflag = false;
            }
            if (!api_call)
            {
                utils.SendLogs("Saving Data...", Color.WhiteSmoke);
                if (generate_tiles)
                {
                    //create directory and make sure it's empty.
                    CreateDirectory(string.Concat(p, Path.DirectorySeparatorChar, "map", Path.DirectorySeparatorChar, "map-tiles"));
                    System.IO.DirectoryInfo map_tiles = new DirectoryInfo(string.Concat(p, Path.DirectorySeparatorChar, "map", Path.DirectorySeparatorChar, "map-tiles"));
                    foreach (FileInfo file in map_tiles.GetFiles())
                    {
                        file.Delete();
                    }

                    Stopwatch watch = new Stopwatch();
                    watch.Start();
                    int countx = 0;
                    int county = 0;
                    int filecount = 0;
                    Bitmap tile = null;
                    System.Drawing.Imaging.PixelFormat format = bmp.PixelFormat;
                    int xsize = 256;
                    int ysize = 256;
                    Bitmap blank = new Bitmap(256, 256, format);
                    System.Drawing.Point zero = new System.Drawing.Point(0, 0);
                    for (int x = 0; x < Main.maxTilesX; x = x + 256)
                    {
                        county = 0;
                        for (int y = 0; y < Main.maxTilesY; y = y + 256)
                        {
                            if (tile != null)
                            {
                                tile.Dispose();
                                tile = null;
                            }
                            xsize = 256;
                            ysize = 256;
                            if (x + 256 > Main.maxTilesX)
                            {
                                xsize = Main.maxTilesX - x;
                            }
                            if (y + 256 > Main.maxTilesY)
                            {
                                ysize = Main.maxTilesY - y;
                            }
                            System.Drawing.Rectangle size = new System.Drawing.Rectangle(x, y, xsize, ysize);
                            tile = bmp.Clone(size, format);
                            using (var graphics = Graphics.FromImage(blank))
                            {
                                graphics.DrawImage(tile, zero);
                            }
                            blank.Save(string.Concat(p, Path.DirectorySeparatorChar, "map", Path.DirectorySeparatorChar, "map-tiles", Path.DirectorySeparatorChar, "map_18_" + countx + "_" + county + ".png"));
                            using (var graphics = Graphics.FromImage(blank))
                            {
                                graphics.FillRectangle(new SolidBrush(System.Drawing.Color.FromArgb(221, 221, 221)), 0, 0, 256, 256);
                            }
                            filecount++;
                            county++;
                        }
                        countx++;
                    }
                    Bitmap zoom16 = null;
                    countx = 0;
                    for (int x = 0; x < Main.maxTilesX; x = x + 1024)
                    {
                        county = 0;
                        for (int y = 0; y < Main.maxTilesY; y = y + 1024)
                        {
                            if (tile != null)
                            {
                                tile.Dispose();
                                tile = null;
                            }
                            xsize = 1024;
                            ysize = 1024;
                            if (x + 1024 > Main.maxTilesX)
                            {
                                xsize = Main.maxTilesX - x;
                            }
                            if (y + 1024 > Main.maxTilesY)
                            {
                                ysize = Main.maxTilesY - y;
                            }
                            Size tilesize = new Size(xsize / 4, ysize / 4);
                            System.Drawing.Rectangle size = new System.Drawing.Rectangle(x, y, xsize, ysize);
                            tile = bmp.Clone(size, format);

                            if (zoom16 != null)
                            {
                                zoom16.Dispose();
                                zoom16 = null;
                            }
                            zoom16 = new Bitmap(tile, tilesize);
                            using (var graphics = Graphics.FromImage(blank))
                            {
                                graphics.DrawImage(zoom16, zero);
                            }
                            blank.Save(string.Concat(p, Path.DirectorySeparatorChar, "map", Path.DirectorySeparatorChar, "map-tiles", Path.DirectorySeparatorChar, "map_16_" + countx + "_" + county + ".png"));
                            using (var graphics = Graphics.FromImage(blank))
                            {
                                graphics.FillRectangle(new SolidBrush(System.Drawing.Color.FromArgb(221, 221, 221)), 0, 0, 256, 256);
                            }
                            filecount++;
                            county++;
                        }
                        countx++;
                    }
                    Bitmap zoom17 = null;
                    countx = 0;
                    for (int x = 0; x < Main.maxTilesX; x = x + 512)
                    {
                        county = 0;
                        for (int y = 0; y < Main.maxTilesY; y = y + 512)
                        {
                            if (tile != null)
                            {
                                tile.Dispose();
                                tile = null;
                            }
                            xsize = 512;
                            ysize = 512;
                            if (x + 512 > Main.maxTilesX)
                            {
                                xsize = Main.maxTilesX - x;
                            }
                            if (y + 512 > Main.maxTilesY)
                            {
                                ysize = Main.maxTilesY - y;
                            }
                            Size tilesize = new Size(xsize / 2, ysize / 2);
                            System.Drawing.Rectangle size = new System.Drawing.Rectangle(x, y, xsize, ysize);
                            tile = bmp.Clone(size, format);

                            if (zoom17 != null)
                            {
                                zoom17.Dispose();
                                zoom17 = null;
                            }
                            zoom17 = new Bitmap(tile, tilesize);
                            using (var graphics = Graphics.FromImage(blank))
                            {
                                graphics.DrawImage(zoom17, zero);
                            }
                            blank.Save(string.Concat(p, Path.DirectorySeparatorChar, "map", Path.DirectorySeparatorChar, "map-tiles", Path.DirectorySeparatorChar, "map_17_" + countx + "_" + county + ".png"));
                            using (var graphics = Graphics.FromImage(blank))
                            {
                                graphics.FillRectangle(new SolidBrush(System.Drawing.Color.FromArgb(221, 221, 221)), 0, 0, 256, 256);
                            }
                            filecount++;
                            county++;
                        }
                        countx++;
                    }
                    /*Bitmap zoom19 = null;
                    countx = 0;
                    for (int x = 0; x < Main.maxTilesX; x = x + 128)
                    {
                        county = 0;
                        for (int y = 0; y < Main.maxTilesY; y = y + 128)
                        {
                            if (tile != null)
                            {
                                tile.Dispose();
                                tile = null;
                            }
                            xsize = 128;
                            ysize = 128;
                            if (x + 128 > Main.maxTilesX)
                            {
                                xsize = Main.maxTilesX - x;
                            }
                            if (y + 128 > Main.maxTilesY)
                            {
                                ysize = Main.maxTilesY - y;
                            }
                            Size tilesize = new Size(xsize * 2, ysize * 2);
                            System.Drawing.Rectangle size = new System.Drawing.Rectangle(x, y, xsize, ysize);
                            tile = bmp.Clone(size, format);

                            if (zoom19 != null)
                            {
                                zoom19.Dispose();
                                zoom19 = null;
                            }
                            zoom19 = new Bitmap(tile, tilesize);
                            using (var graphics = Graphics.FromImage(blank))
                            {
                                graphics.DrawImage(zoom19, zero);
                            }
                            blank.Save(string.Concat(p, Path.DirectorySeparatorChar, "map", Path.DirectorySeparatorChar, "map-tiles", Path.DirectorySeparatorChar, "map_19_" + countx + "_" + county + ".png"));
                            using (var graphics = Graphics.FromImage(blank))
                            {
                                graphics.FillRectangle(new SolidBrush(System.Drawing.Color.FromArgb(221,221,221)), 0, 0, 256, 256);
                            }
                            filecount++;
                            county++;
                        }
                        countx++;
                    }*/
                    string html = "<html>\r\n<head>\r\n<link rel=\"stylesheet\" href=\"https://unpkg.com/leaflet@1.0.3/dist/leaflet.css\"/>\r\n<title>" + Main.worldName + "</title>\r\n</head>\r\n<body style=\"margin: 0; padding: 0;\">\r\n<div id=\"map\" style=\"height: 100%;\"></div>\r\n<script src=\"https://unpkg.com/leaflet@1.0.3/dist/leaflet.js\"></script>\r\n<script>\r\n\tvar map = L.map('map', {\r\n\t\tmaxZoom: 18,\r\n\t\tminZoom: 16,\r\n\t\tcrs: L.CRS.Simple\r\n\t});\r\n\tvar southWest = map.unproject([0, " + Main.maxTilesY + "], map.getMaxZoom());\r\n\tvar northEast = map.unproject([" + Main.maxTilesX + ", 0], map.getMaxZoom());\r\n\tmap.setMaxBounds(new L.LatLngBounds(southWest, northEast));\r\n\t\tL.tileLayer('map-tiles/map_{z}_{x}_{y}.png', {\r\n\t\tattribution: 'Imagery: <a href=\"http://github.com/elevatorguy/TDSM_map/tree/tshock\">Map</a>, using <a href=\"https://github.com/mrkite/TerraFirma\">Terrafirma</a> colorscheme.',\r\n\t}).addTo(map);\r\n\tmap.setView([0, 0], 18);\r\n\tmap.whenReady(function() {\r\n\t\tmap.setView(map.unproject([3205, 402]), 18);\r\n\t});\r\n</script>\r\n</body>\r\n</html>\r\n";
                    System.IO.File.WriteAllText(string.Concat(p, Path.DirectorySeparatorChar, "map", Path.DirectorySeparatorChar, "map.html"), html);
                    watch.Stop();
                    utils.SendLogs("Saved " + filecount + " file(s) in " + watch.Elapsed.Seconds + "." + (watch.ElapsedMilliseconds - 1000 * watch.Elapsed.Seconds) + "s", Color.WhiteSmoke);
                }
                else
                {
                    bmp.Save(string.Concat(p, Path.DirectorySeparatorChar, filename));
                    bmp.Dispose();
                    bmp = null;
                }
                stopwatch.Stop();
                utils.SendLogs("Total duration: " + stopwatch.Elapsed.Seconds + " Second(s)", Color.WhiteSmoke);
                utils.SendLogs("Saving Complete.", Color.WhiteSmoke);
            }
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
                int quarter = ((x2 - x1) / 4);
                using (var prog = new ProgressLogger(quarter - 1, "Saving image data"))
                    for (int i = 0; i < quarter; i++)
                    {
                        prog.Value = i; // each thread finished about the same time so I put the progress logger on one of them
                        maprenderloop(x1 + i, piece1, 0, y1, y2);
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
                    maprenderloop(x1 + i + quarter, piece2, quarter, y1, y2);
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
                    maprenderloop(x1 + i + 2 * quarter, piece3, 2 * quarter, y1, y2);
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
                    maprenderloop(x1 + i + 3 * quarter, piece4, 3 * quarter, y1, y2);
                }
            }
        }

        /**
         * This maps a 1 pixel vertical slice at x = i;
         */
        private void maprenderloop(int i, Bitmap bmp, int piece)
        {
            maprenderloop(i, bmp, piece, 0, Main.maxTilesY);
        }

        /**
         * This maps a 1 pixel vertical slice at x = i; (from y = ymin to y = ymax)
         */
        private void maprenderloop(int i, Bitmap bmp, int piece, int ymin, int ymax)
        {
            UInt32 tempColor;
            List<int> list;
            int x = i - x1;
            int j = 0;
            try
            {
                for (j = ymin; j < ymax; j++)
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
                                tempColor = DimUInt32Defs[j + FADE_START_INDEX];
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
                                SetPixel(bmp, x - piece, j - ymin, DimColorDefs[Main.tile[i, j].wall + (WALL_START_INDEX - 1)], false);
                                tempColor = DimUInt32Defs[Main.tile[i, j].wall + (WALL_START_INDEX - 1)];
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
                                tempColor = UInt32Defs[j + FADE_START_INDEX];
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
                                SetPixel(bmp, x - piece, j - ymin, ColorDefs[Main.tile[i, j].wall + (WALL_START_INDEX - 1)], false);
                                tempColor = UInt32Defs[Main.tile[i, j].wall + (WALL_START_INDEX - 1)];
                            }
                        }
                        // lookup blendcolor of color just drawn, and draw again
                        if (Main.tile[i, j].liquid > 0)
                        {
                            if (lavablendlist.ContainsKey(tempColor))
                            {  // incase the map has hacked data
                                if (Main.tile[i, j].honey())
                                    SetPixel(bmp, x - piece, j - ymin, honeyblendlist[tempColor], false);
                                else if (Main.tile[i, j].lava())
                                    SetPixel(bmp, x - piece, j - ymin, lavablendlist[tempColor], false);
                                else
                                    SetPixel(bmp, x - piece, j - ymin, waterblendlist[tempColor], false);
                            }
                        }
                    }
                }
            }
            catch (KeyNotFoundException e)
            {
                utils.SendLogs("<map> ERROR: Problem with pixel lookup at (x,y): (" + i + "," + j + "). Key: " + e.Data.Keys.ToString(), Color.Red);
                utils.SendLogs(e.StackTrace.ToString(), Color.WhiteSmoke);

                //continue and see how many pixels are bad...
                //this might be a new item added to the game that we havn't added to the plugin yet.
            }

            if (pixelfailureflag)
            {
                utils.SendLogs("<map> WARNING: Could not draw certain pixel at row (" + i + ",y).", Color.Yellow);
                pixelfailureflag = false;
            }

        }

        //pre-blends colors when loading plugin so making the image is faster
        public void initBList()
        {
            //adds all the colors for walls/tiles/global
            UInt32 waterColor = 0x093DBF;
            UInt32 lavaColor = 0xFD2003;
            UInt32 honeyColor = 0xFF9C0C;
            //blends water and lava with UInt32Defs
            using (var blendprog = new ProgressLogger(Main.maxTilesX - 1, "[map] Blending colors"))
                for (int y = 0; y <= Main.maxTilesY + FADE_START_INDEX; y++)
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

                        doBlendResult(c, waterColor, lavaColor, honeyColor, "regular");
                        doBlendResult(d, waterColor, lavaColor, honeyColor, "dim");
                    }
                }
        }

        private void doBlendResult(UInt32 c, UInt32 waterColor, UInt32 lavaColor, UInt32 honeyColor, string type)
        {
            if (type == "regular" && !(lavablendlist.ContainsKey(c)))
            {
                System.Drawing.Color waterblendresult = toColor(alphaBlend(c, waterColor, 0.5));

                waterblendlist.Add(c, waterblendresult);
                lavablendlist.Add(c, toColor(alphaBlend(c, lavaColor, 0.5)));
                honeyblendlist.Add(c, toColor(alphaBlend(c, honeyColor, 0.5)));
            }
            if (type == "dim" && !(lavadimlist.ContainsKey(c)))
            {
                UInt32 waterdimresult = alphaBlend(c, dimI(waterColor), 0.5);

                waterdimlist.Add(c, toColor(waterdimresult));
                lavadimlist.Add(c, toColor(alphaBlend(c, dimI(lavaColor), 0.5)));
                honeydimlist.Add(c, toColor(alphaBlend(c, dimI(honeyColor), 0.5)));
            }
        }

        public partial class Constants //credits go to the authors of Terrafirma				
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
                public static System.Drawing.Color WOODEN_SINK = ColorTranslator.FromHtml("#8C6850");
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
                public static System.Drawing.Color MINECART_TRACKS = ColorTranslator.FromHtml("#A3A6A8");
                public static System.Drawing.Color CORALSTONE_BLOCK = ColorTranslator.FromHtml("#C94642");
                public static System.Drawing.Color BLUE_JELLYFISH_JAR = ColorTranslator.FromHtml("#7792E2");
                public static System.Drawing.Color GREEN_JELLYFISH_JAR = ColorTranslator.FromHtml("#76E381");
                public static System.Drawing.Color PINK_JELLYFISH_JAR = ColorTranslator.FromHtml("#E2A0DA");
                public static System.Drawing.Color SHIP_IN_A_BOTTLE = ColorTranslator.FromHtml("#C8F6FE");
                public static System.Drawing.Color SEAWEED_PLANTER = ColorTranslator.FromHtml("#CBB997");
                public static System.Drawing.Color BOREAL_WOOD = ColorTranslator.FromHtml("#604D40");
                public static System.Drawing.Color PALM_WOOD = ColorTranslator.FromHtml("#8F6D3F");
                public static System.Drawing.Color PILLAR = ColorTranslator.FromHtml("#B68D56");
                public static System.Drawing.Color SEASHELL = ColorTranslator.FromHtml("#E4D5AD");
                public static System.Drawing.Color TIN_PLATING = ColorTranslator.FromHtml("#817D5D");
                public static System.Drawing.Color WATERFALL_BLOCK = ColorTranslator.FromHtml("#093DBF");
                public static System.Drawing.Color LAVAFALL_BLOCK = ColorTranslator.FromHtml("#FD2003");
                public static System.Drawing.Color CONFETTI_BLOCK = ColorTranslator.FromHtml("#C8F6FE");
                public static System.Drawing.Color MIDNIGHT_CONFETTI_BLOCK = ColorTranslator.FromHtml("#FF20D8");
                public static System.Drawing.Color COPPER_COIN_PILE = ColorTranslator.FromHtml("#E2764C");
                public static System.Drawing.Color SILVER_COIN_PILE = ColorTranslator.FromHtml("#464D50");
                public static System.Drawing.Color GOLD_COIN_PILE = ColorTranslator.FromHtml("#CCB548");
                public static System.Drawing.Color PLATINUM_COIN_PILE = ColorTranslator.FromHtml("#BEBEB2");
                public static System.Drawing.Color WEAPON_RACK = ColorTranslator.FromHtml("#78553C");
                public static System.Drawing.Color FIREWORKS_BOX = ColorTranslator.FromHtml("#E3B994");
                public static System.Drawing.Color LIVING_FIRE_BLOCK = ColorTranslator.FromHtml("#FE7902");
                public static System.Drawing.Color CHARACTER_STATUES = ColorTranslator.FromHtml("#606460");
                public static System.Drawing.Color FIREWORK_FOUNTAIN = ColorTranslator.FromHtml("#19C762");
                public static System.Drawing.Color GRASSHOPPER_CAGE = ColorTranslator.FromHtml("#57ADBD");
                public static System.Drawing.Color LIVING_CURSED_FIRE_BLOCK = ColorTranslator.FromHtml("#B3FC00");
                public static System.Drawing.Color LIVING_DEMON_FIRE_BLOCK = ColorTranslator.FromHtml("#660CD4");
                public static System.Drawing.Color LIVING_FROST_FIRE_BLOCK = ColorTranslator.FromHtml("#00BAF2");
                public static System.Drawing.Color LIVING_ICHOR_BLOCK = ColorTranslator.FromHtml("#FECA50");
                public static System.Drawing.Color LIVING_ULTRABRIGHT_FIRE_BLOCK = ColorTranslator.FromHtml("#83FCF5");
                public static System.Drawing.Color HONEYFALL_BLOCK = ColorTranslator.FromHtml("#FF9C0C");
                public static System.Drawing.Color CHLOROPHYTE_BRICK = ColorTranslator.FromHtml("#246133");
                public static System.Drawing.Color CRIMTANE_BRICK = ColorTranslator.FromHtml("#A42A49");
                public static System.Drawing.Color SHROOMITE_PLATING = ColorTranslator.FromHtml("#2215A4");
                public static System.Drawing.Color SHROOMITE = ColorTranslator.FromHtml("#37589D");
                public static System.Drawing.Color MARTIAN_CONDUIT_PLATING = ColorTranslator.FromHtml("#629AB3");
                public static System.Drawing.Color SMOKE_BLOCK = ColorTranslator.FromHtml("#191919");
                public static System.Drawing.Color ROOTS = ColorTranslator.FromHtml("#883231");
                public static System.Drawing.Color VINE_ROPE = ColorTranslator.FromHtml("#1E9648");
                public static System.Drawing.Color BEWITCHING_TABLE = ColorTranslator.FromHtml("#4C2200");
                public static System.Drawing.Color ALCHEMY_TABLE = ColorTranslator.FromHtml("#6D4E47");
                public static System.Drawing.Color SUNDIAL = ColorTranslator.FromHtml("#006887");
                public static System.Drawing.Color MARBLE_BLOCK = ColorTranslator.FromHtml("#A8B2CC");
                public static System.Drawing.Color GOLD_BIRD_CAGE = ColorTranslator.FromHtml("#CBB349");
                public static System.Drawing.Color GOLD_BUNNY_CAGE = ColorTranslator.FromHtml("#CBB349");
                public static System.Drawing.Color GOLD_BUTTERFLY_JAR = ColorTranslator.FromHtml("#CBB349");
                public static System.Drawing.Color GOLD_FROG_CAGE = ColorTranslator.FromHtml("#CBB349");
                public static System.Drawing.Color GOLD_GRASSHOPPER_CAGE = ColorTranslator.FromHtml("#CBB349");
                public static System.Drawing.Color GOLD_MOUSE_CAGE = ColorTranslator.FromHtml("#CBB349");
                public static System.Drawing.Color GOLD_WORM_CAGE = ColorTranslator.FromHtml("#CBB349");
                public static System.Drawing.Color SILK_ROPE = ColorTranslator.FromHtml("#757FB9");
                public static System.Drawing.Color WEB_ROPE = ColorTranslator.FromHtml("#DFE8E9");
                public static System.Drawing.Color MARBLE = ColorTranslator.FromHtml("#C3CEE3");
                public static System.Drawing.Color GRANITE = ColorTranslator.FromHtml("#322E68");
                public static System.Drawing.Color GRANITE_BLOCK = ColorTranslator.FromHtml("#221F47");
                public static System.Drawing.Color METEORITE_BRICK = ColorTranslator.FromHtml("#7F74C2");
                public static System.Drawing.Color PINK_SLIME_BLOCK = ColorTranslator.FromHtml("#F97FC8");
                public static System.Drawing.Color PEACE_CANDLE = ColorTranslator.FromHtml("#FE95D2");
                public static System.Drawing.Color MAGIC_WATER_DROPPER = ColorTranslator.FromHtml("1"); // todo: fix me...
                public static System.Drawing.Color MAGIC_LAVA_DROPPER = ColorTranslator.FromHtml("1"); // todo: fix me...
                public static System.Drawing.Color MAGIC_HONEY_DROPPER = ColorTranslator.FromHtml("1"); // todo: fix me...
                public static System.Drawing.Color CRATE = ColorTranslator.FromHtml("#906850");
                public static System.Drawing.Color SHARPENING_STATION = ColorTranslator.FromHtml("#6C6257");
                public static System.Drawing.Color TARGET_DUMMY = ColorTranslator.FromHtml("#DDB487");
                public static System.Drawing.Color BUBBLE = ColorTranslator.FromHtml("#D3D2FF");
                public static System.Drawing.Color PLANTER_BOXES = ColorTranslator.FromHtml("#946B50");
                public static System.Drawing.Color HEATED_STONE = ColorTranslator.FromHtml("#FE7902");
                public static System.Drawing.Color FLOWER_VINES = ColorTranslator.FromHtml("#1E9648");
                public static System.Drawing.Color LIVING_MAHOGANY = ColorTranslator.FromHtml("#DD8890");
                public static System.Drawing.Color RICH_MAHOGANY_LEAF = ColorTranslator.FromHtml("#476D0B");
                public static System.Drawing.Color CRYSTAL_BLOCK = ColorTranslator.FromHtml("#0B377F");
                public static System.Drawing.Color OPEN_TRAP_DOOR = ColorTranslator.FromHtml("#4F3A2E");
                public static System.Drawing.Color CLOSED_TRAP_DOOR = ColorTranslator.FromHtml("#6B4F3F");
                public static System.Drawing.Color CLOSED_TALL_GATE = ColorTranslator.FromHtml("#503B30");
                public static System.Drawing.Color OPEN_TALL_GATE = ColorTranslator.FromHtml("#2E2119");
                public static System.Drawing.Color LAVA_LAMP = ColorTranslator.FromHtml("#FD2003");
                public static System.Drawing.Color ENCHANTED_NIGHTCRAWLER_CAGE = ColorTranslator.FromHtml("#57ADBD");
                public static System.Drawing.Color BUGGY_CAGE = ColorTranslator.FromHtml("#57ADBD");
                public static System.Drawing.Color GRUBBY_CAGE = ColorTranslator.FromHtml("#57ADBD");
                public static System.Drawing.Color SLUGGY_CAGE = ColorTranslator.FromHtml("#57ADBD");
                public static System.Drawing.Color ITEM_FRAME = ColorTranslator.FromHtml("#634732");
                public static System.Drawing.Color SANDSTONE = ColorTranslator.FromHtml("#B36741");
                public static System.Drawing.Color HARDENED_SAND = ColorTranslator.FromHtml("#D49458");
                public static System.Drawing.Color CORRUPT_HARDENED_SAND = ColorTranslator.FromHtml("#604475");
                public static System.Drawing.Color CRIMSON_HARDENED_SAND = ColorTranslator.FromHtml("#4D4C42");
                public static System.Drawing.Color CORRUPT_SANDSTONE = ColorTranslator.FromHtml("#604475");
                public static System.Drawing.Color CRISMON_SANDSTONE = ColorTranslator.FromHtml("#573937");
                public static System.Drawing.Color HALLOW_HARDENED_SAND = ColorTranslator.FromHtml("#B18ABA");
                public static System.Drawing.Color HALLOW_SANDSTONE = ColorTranslator.FromHtml("#9E71A4");
                public static System.Drawing.Color DESERT_FOSSIL_BLOCK = ColorTranslator.FromHtml("#8C543C");
                public static System.Drawing.Color FIREPLACE = ColorTranslator.FromHtml("#FD3E03");
                public static System.Drawing.Color CHIMNEY = ColorTranslator.FromHtml("#8C8C8C");
                public static System.Drawing.Color FOSSIL_ORE = ColorTranslator.FromHtml("#FFE384");
                public static System.Drawing.Color LUNAR_ORE = ColorTranslator.FromHtml("#5EE5A3");
                public static System.Drawing.Color LUNAR_BRICK = ColorTranslator.FromHtml("#3A3736");
                public static System.Drawing.Color MONOLITHS = ColorTranslator.FromHtml("#22DD97");
                public static System.Drawing.Color DETONATOR = ColorTranslator.FromHtml("#C90303");
                public static System.Drawing.Color LUNAR_CRAFTING_STATION = ColorTranslator.FromHtml("#936857");
                public static System.Drawing.Color RED_SQUIRREL_CAGE = ColorTranslator.FromHtml("#57ADBD");
                public static System.Drawing.Color GOLD_SQUIRREL_CAGE = ColorTranslator.FromHtml("#CBB349");
                public static System.Drawing.Color SOLAR_FRAGMENT_BLOCK = ColorTranslator.FromHtml("#FE9E23");
                public static System.Drawing.Color VORTEX_FRAGMENT_BLOCK = ColorTranslator.FromHtml("#00A0AA");
                public static System.Drawing.Color NEBULA_FRAGMENT_BLOCK = ColorTranslator.FromHtml("#A057EA");
                public static System.Drawing.Color STARDUST_FRAGMENT_BLOCK = ColorTranslator.FromHtml("#5057B6");
                public static System.Drawing.Color LOGIC_GATE_LAMP = ColorTranslator.FromHtml("#585F70");
                public static System.Drawing.Color LOGIC_GATES = ColorTranslator.FromHtml("#757D97");
                public static System.Drawing.Color CONVEYOR_BELT_CLOCKWISE = ColorTranslator.FromHtml("#494646");
                public static System.Drawing.Color CONVEYOR_BELT_COUNTER_CLOCKWISE = ColorTranslator.FromHtml("#494646");
                public static System.Drawing.Color LOGIC_SENSORS = ColorTranslator.FromHtml("#9F2500");
                public static System.Drawing.Color JUNCTION_BOX = ColorTranslator.FromHtml("#929BBB");
                public static System.Drawing.Color ANNOUNCEMENT_BOX = ColorTranslator.FromHtml("#AEC3D7");
                public static System.Drawing.Color RED_TEAM_BLOCK = ColorTranslator.FromHtml("#4D0B23");
                public static System.Drawing.Color RED_TEAM_PLATFORM = ColorTranslator.FromHtml("#771634");
                public static System.Drawing.Color WEIGHTED_PRESSURE_PLATES = ColorTranslator.FromHtml("#FCA259");
                public static System.Drawing.Color WIRE_BULB = ColorTranslator.FromHtml("#3F3F3F");
                public static System.Drawing.Color GREEN_TEAM_BLOCK = ColorTranslator.FromHtml("#17774F");
                public static System.Drawing.Color BLUE_TEAM_BLOCK = ColorTranslator.FromHtml("#173677");
                public static System.Drawing.Color YELLOW_TEAM_BLOCK = ColorTranslator.FromHtml("#774417");
                public static System.Drawing.Color PINK_TEAM_BLOCK = ColorTranslator.FromHtml("#4A1777");
                public static System.Drawing.Color WHITE_TEAM_BLOCK = ColorTranslator.FromHtml("#4E526D");
                public static System.Drawing.Color GREEN_TEAM_PLATFORM = ColorTranslator.FromHtml("#27A860");
                public static System.Drawing.Color BLUE_TEAM_PLATFORM = ColorTranslator.FromHtml("#275EA8");
                public static System.Drawing.Color YELLOW_TEAM_PLATFORM = ColorTranslator.FromHtml("#A87927");
                public static System.Drawing.Color PINK_TEAM_PLATFORM = ColorTranslator.FromHtml("#6F27A8");
                public static System.Drawing.Color WHITE_TEAM_PLATFORM = ColorTranslator.FromHtml("#9694AE");
                public static System.Drawing.Color GEM_LOCKS = ColorTranslator.FromHtml("#9B1512");
                public static System.Drawing.Color TRAPPED_CHESTS = ColorTranslator.FromHtml("#946B50");
                public static System.Drawing.Color TEAL_PRESSURE_PAD = ColorTranslator.FromHtml("#0390C9");
                public static System.Drawing.Color GEYSER = ColorTranslator.FromHtml("#7B7B7B");
                public static System.Drawing.Color BEE_HIVE = ColorTranslator.FromHtml("#BFB07C");
                public static System.Drawing.Color PIXEL_BOX = ColorTranslator.FromHtml("#373749");
                public static System.Drawing.Color SILLY_PINK_BALLOON = ColorTranslator.FromHtml("#FF4298");
                public static System.Drawing.Color SILLY_PURPLE_BALLOON = ColorTranslator.FromHtml("#B384FF");
                public static System.Drawing.Color SILLY_GREEN_BALLOON = ColorTranslator.FromHtml("#00CEB4");
                public static System.Drawing.Color BLUE_STREAMER = ColorTranslator.FromHtml("#5BBAF0");
                public static System.Drawing.Color GREEN_STREAMER = ColorTranslator.FromHtml("#5CF05B");
                public static System.Drawing.Color PINK_STREAMER = ColorTranslator.FromHtml("#F05B93");
                public static System.Drawing.Color SILLY_BALLOON_MACHINE = ColorTranslator.FromHtml("#FF96B5");
                public static System.Drawing.Color SILLY_TIED_BALLOON = ColorTranslator.FromHtml("#B384FF");
                public static System.Drawing.Color PIGRONATA = ColorTranslator.FromHtml("#AE10B0");
                public static System.Drawing.Color PARTY_CENTER = ColorTranslator.FromHtml("#30FF6E");
                public static System.Drawing.Color SILLY_TIED_BUNDLE_OF_BALLOONS = ColorTranslator.FromHtml("#B384FF");
                public static System.Drawing.Color PARTY_PRESENT = ColorTranslator.FromHtml("#96A4CE");
                public static System.Drawing.Color SANDFALL = ColorTranslator.FromHtml("#D3C66F");
                public static System.Drawing.Color SNOWFALL = ColorTranslator.FromHtml("#BEDFE8");
                public static System.Drawing.Color SNOW_CLOUD = ColorTranslator.FromHtml("#8DA3B5");
                public static System.Drawing.Color SAND_DRIP = ColorTranslator.FromHtml("#FFDE64");
                public static System.Drawing.Color DESERT_SPIRIT_LAMP = ColorTranslator.FromHtml("#E7B21C");
                public static System.Drawing.Color DEFENDERS_FORGE = ColorTranslator.FromHtml("#9BD6F0");
                public static System.Drawing.Color WAR_TABLE = ColorTranslator.FromHtml("#E9B780");
                public static System.Drawing.Color WAR_TABLE_BANNER = ColorTranslator.FromHtml("#3354C3");
                public static System.Drawing.Color ELDER_CYSTAL_STAND = ColorTranslator.FromHtml("#CD9949");
                public static System.Drawing.Color CHESTS_2 = ColorTranslator.FromHtml("#4B137E");
                public static System.Drawing.Color TRAPPED_CHESTS_2 = ColorTranslator.FromHtml("#4B137E");
                public static System.Drawing.Color TABLES_2 = ColorTranslator.FromHtml("#7F5C45");

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
                public static System.Drawing.Color LIVING_LEAF_WALL = ColorTranslator.FromHtml("#013E17");
                public static System.Drawing.Color ROCK_WALL = ColorTranslator.FromHtml("#362619");
                public static System.Drawing.Color SPIDER_CAVE_WALL = ColorTranslator.FromHtml("#242424");
                public static System.Drawing.Color SHRUB_WALL = ColorTranslator.FromHtml("#235F39");
                public static System.Drawing.Color SHRUB_WALL_2 = ColorTranslator.FromHtml("#3F5F23");
                public static System.Drawing.Color SHRUB_WALL_3 = ColorTranslator.FromHtml("#1E5030");
                public static System.Drawing.Color GRASS_WALL = ColorTranslator.FromHtml("#1E5030");
                public static System.Drawing.Color JUNGLE_WALL = ColorTranslator.FromHtml("#35501E");
                public static System.Drawing.Color FLOWER_WALL = ColorTranslator.FromHtml("#1E5030");
                public static System.Drawing.Color SHRUB_WALL_4 = ColorTranslator.FromHtml("#333250");
                public static System.Drawing.Color SHRUB_WALL_5 = ColorTranslator.FromHtml("#143736");
                public static System.Drawing.Color ICE_WALL = ColorTranslator.FromHtml("#306085");
                public static System.Drawing.Color CACTUS_WALL = ColorTranslator.FromHtml("#34540C");
                public static System.Drawing.Color CLOUD_WALL = ColorTranslator.FromHtml("#E4ECEE");
                public static System.Drawing.Color MUSHROOM_WALL = ColorTranslator.FromHtml("#313B8C");
                public static System.Drawing.Color BONE_BLOCK_WALL = ColorTranslator.FromHtml("#63633C");
                public static System.Drawing.Color SLIME_BLOCK_WALL = ColorTranslator.FromHtml("#213C79");
                public static System.Drawing.Color FLESH_BLOCK_WALL = ColorTranslator.FromHtml("#3D0D10");
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
                public static System.Drawing.Color LEAD_FENCE = ColorTranslator.FromHtml("#3C3C3C");
                public static System.Drawing.Color HIVE_WALL_2 = ColorTranslator.FromHtml("#8C5131");
                public static System.Drawing.Color PALLADIUM_COLUMN_WALL = ColorTranslator.FromHtml("#5E1911");
                public static System.Drawing.Color BUBBLEGUM_BLOCK_WALL = ColorTranslator.FromHtml("#994996");
                public static System.Drawing.Color TITANSTONE_BLOCK_WALL = ColorTranslator.FromHtml("#1F1814");
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
                public static System.Drawing.Color IRON_FENCE = ColorTranslator.FromHtml("#919191");
                public static System.Drawing.Color COPPER_PLATING_WALL = ColorTranslator.FromHtml("#703712");
                public static System.Drawing.Color STONE_SLAB_WALL = ColorTranslator.FromHtml("#4C4C4C");
                public static System.Drawing.Color SAIL = ColorTranslator.FromHtml("#E5DAA1");
                public static System.Drawing.Color BOREAL_WOOD_WALL = ColorTranslator.FromHtml("#504237");
                public static System.Drawing.Color BOREAL_WOOD_FENCE = ColorTranslator.FromHtml("#6B5647");
                public static System.Drawing.Color PALM_WOOD_WALL = ColorTranslator.FromHtml("#664B22");
                public static System.Drawing.Color PALM_WOOD_FENCE = ColorTranslator.FromHtml("#B68D56");
                public static System.Drawing.Color AMBER_GEMSPARK_WALL = ColorTranslator.FromHtml("#FF743F");
                public static System.Drawing.Color AMETHYST_GEMSPARK_WALL = ColorTranslator.FromHtml("#BF3FFF");
                public static System.Drawing.Color DIAMOND_GEMSPARK_WALL = ColorTranslator.FromHtml("#DBDBE8");
                public static System.Drawing.Color EMERALD_GEMSPARK_WALL = ColorTranslator.FromHtml("#3FFF47");
                public static System.Drawing.Color OFFLINE_AMBER_GEMSPARK_WALL = ColorTranslator.FromHtml("#763F25");
                public static System.Drawing.Color OFFLINE_AMETHYST_GEMSPARK_WALL = ColorTranslator.FromHtml("#512576");
                public static System.Drawing.Color OFFLINE_DIAMOND_GEMSPARK_WALL = ColorTranslator.FromHtml("#404359");
                public static System.Drawing.Color OFFLINE_EMERALD_GEMSPARK_WALL = ColorTranslator.FromHtml("#257634");
                public static System.Drawing.Color OFFLINE_RUBY_GEMSPARK_WALL = ColorTranslator.FromHtml("#76253A");
                public static System.Drawing.Color OFFLINE_SAPPHIRE_GEMSPARK_WALL = ColorTranslator.FromHtml("#252576");
                public static System.Drawing.Color OFFLINE_TOPAZ_GEMSPARK_WALL = ColorTranslator.FromHtml("#767125");
                public static System.Drawing.Color RUBY_GEMSPARK_WALL = ColorTranslator.FromHtml("#FF3F3F");
                public static System.Drawing.Color SAPPHIRE_GEMSPARK_WALL = ColorTranslator.FromHtml("#3F51FF");
                public static System.Drawing.Color TOPAZ_GEMSPARK_WALL = ColorTranslator.FromHtml("#EFFF3F");
                public static System.Drawing.Color TIN_PLATING_WALL = ColorTranslator.FromHtml("#464433");
                public static System.Drawing.Color CONFETTI_WALL = ColorTranslator.FromHtml("#0C6A8A");
                public static System.Drawing.Color MIDNIGHT_CONFETTI_WALL = ColorTranslator.FromHtml("#850C77");
                public static System.Drawing.Color WALL_170 = ColorTranslator.FromHtml("#3B2716");
                public static System.Drawing.Color WALL_171 = ColorTranslator.FromHtml("#3B2716");
                public static System.Drawing.Color HONEYFALL_WALL = ColorTranslator.FromHtml("#A36000");
                public static System.Drawing.Color CHLOROPHYTE_BRICK_WALL = ColorTranslator.FromHtml("#082A27");
                public static System.Drawing.Color CRIMTANE_BRICK_WALL = ColorTranslator.FromHtml("#451D26");
                public static System.Drawing.Color SHROOMITE_PLATING_WALL = ColorTranslator.FromHtml("#0F0969");
                public static System.Drawing.Color MARTIAN_CONDUIT_WALL = ColorTranslator.FromHtml("#2C2934");
                public static System.Drawing.Color HELLSTONE_BRICK_WALL_2 = ColorTranslator.FromHtml("#5D2B2B");
                public static System.Drawing.Color MARBLE_WALL = ColorTranslator.FromHtml("#868DA0");
                public static System.Drawing.Color MARBLE_BLOCK_WALL = ColorTranslator.FromHtml("#9AA2B1");
                public static System.Drawing.Color GRANITE_WALL = ColorTranslator.FromHtml("#0C0A19");
                public static System.Drawing.Color GRANITE_BLOCK_WALL = ColorTranslator.FromHtml("#1A1733");
                public static System.Drawing.Color METEORITE_BRICK_WALL = ColorTranslator.FromHtml("#4A4781");
                public static System.Drawing.Color MARBLE_WALL_2 = ColorTranslator.FromHtml("#9AA2B1");
                public static System.Drawing.Color GRANITE_WALL_2 = ColorTranslator.FromHtml("#141D49");
                public static System.Drawing.Color WALL_185 = ColorTranslator.FromHtml("#525252");
                public static System.Drawing.Color CRYSTAL_WALL = ColorTranslator.FromHtml("#260942");
                public static System.Drawing.Color SANDSTONE_WALL = ColorTranslator.FromHtml("#A8603B");
                public static System.Drawing.Color WALL_188 = ColorTranslator.FromHtml("#523F50");
                public static System.Drawing.Color WALL_189 = ColorTranslator.FromHtml("#41334D");
                public static System.Drawing.Color WALL_190 = ColorTranslator.FromHtml("#3E3A51");
                public static System.Drawing.Color WALL_191 = ColorTranslator.FromHtml("#5E4364");
                public static System.Drawing.Color WALL_192 = ColorTranslator.FromHtml("#904334");
                public static System.Drawing.Color WALL_193 = ColorTranslator.FromHtml("#4F1317");
                public static System.Drawing.Color WALL_194 = ColorTranslator.FromHtml("#2F0A0C");
                public static System.Drawing.Color WALL_195 = ColorTranslator.FromHtml("#792A2F");
                public static System.Drawing.Color WALL_196 = ColorTranslator.FromHtml("#70503E");
                public static System.Drawing.Color WALL_197 = ColorTranslator.FromHtml("#7F5E4C");
                public static System.Drawing.Color WALL_198 = ColorTranslator.FromHtml("#614333");
                public static System.Drawing.Color WALL_199 = ColorTranslator.FromHtml("#70503E");
                public static System.Drawing.Color WALL_200 = ColorTranslator.FromHtml("#553952");
                public static System.Drawing.Color WALL_201 = ColorTranslator.FromHtml("#7B6474");
                public static System.Drawing.Color WALL_202 = ColorTranslator.FromHtml("#A7307E");
                public static System.Drawing.Color WALL_203 = ColorTranslator.FromHtml("#96407E");
                public static System.Drawing.Color WALL_204 = ColorTranslator.FromHtml("#53311E");
                public static System.Drawing.Color WALL_205 = ColorTranslator.FromHtml("#5F7652");
                public static System.Drawing.Color WALL_206 = ColorTranslator.FromHtml("#464541");
                public static System.Drawing.Color WALL_207 = ColorTranslator.FromHtml("#53311E");
                public static System.Drawing.Color WALL_208 = ColorTranslator.FromHtml("#3E3D3C");
                public static System.Drawing.Color WALL_209 = ColorTranslator.FromHtml("#554246");
                public static System.Drawing.Color WALL_210 = ColorTranslator.FromHtml("#393434");
                public static System.Drawing.Color WALL_211 = ColorTranslator.FromHtml("#58433B");
                public static System.Drawing.Color WALL_212 = ColorTranslator.FromHtml("#585750");
                public static System.Drawing.Color WALL_213 = ColorTranslator.FromHtml("#383735");
                public static System.Drawing.Color WALL_214 = ColorTranslator.FromHtml("#4C343C");
                public static System.Drawing.Color WALL_215 = ColorTranslator.FromHtml("#524A4D");
                public static System.Drawing.Color HARDENED_SAND_WALL = ColorTranslator.FromHtml("#B97F3B");
                public static System.Drawing.Color CORRUPT_HARDENED_SAND_WALL = ColorTranslator.FromHtml("#463F5E");
                public static System.Drawing.Color CRIMSON_HARDENED_SAND_WALL = ColorTranslator.FromHtml("#33332D");
                public static System.Drawing.Color HALLOW_HARDENED_SAND_WALL = ColorTranslator.FromHtml("#72618C");
                public static System.Drawing.Color CORRUPT_SANDSTONE_WALL = ColorTranslator.FromHtml("#53356A");
                public static System.Drawing.Color CRIMSON_SANDSTONE_WALL = ColorTranslator.FromHtml("#56372F");
                public static System.Drawing.Color HALLOW_SANDSTONE_WALL = ColorTranslator.FromHtml("#3E4765");
                public static System.Drawing.Color DESERT_FOSSIL_WALL = ColorTranslator.FromHtml("#572612");
                public static System.Drawing.Color LUNAR_BRICK_WALL = ColorTranslator.FromHtml("#262424");
                public static System.Drawing.Color COG_WALL = ColorTranslator.FromHtml("#444444");
                public static System.Drawing.Color SANDFALL_WALL = ColorTranslator.FromHtml("#948A4A");
                public static System.Drawing.Color SNOWFALL_WALL = ColorTranslator.FromHtml("#5F89BF");
                public static System.Drawing.Color SILLY_PINK_BALLOON_WALL = ColorTranslator.FromHtml("#A0024B");
                public static System.Drawing.Color SILLY_PURPLE_BALLOON_WALL = ColorTranslator.FromHtml("#6437A4");
                public static System.Drawing.Color SILLY_GREEN_BALLOON_WALL = ColorTranslator.FromHtml("#007565");

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
                public static System.Drawing.Color LIQUID_HONEY = ColorTranslator.FromHtml("#FF9C0C");
                public static System.Drawing.Color WIRE_1 = ColorTranslator.FromHtml("#ac0205");
                public static System.Drawing.Color WIRE_2 = ColorTranslator.FromHtml("#0257ac");
                public static System.Drawing.Color WIRE_3 = ColorTranslator.FromHtml("#2fac02");
            }
        }

        public void InitializeMapperDefs2() //Credits go to the authors of MoreTerra
        {
            ColorDefs = new Dictionary<int, System.Drawing.Color>(FADE_START_INDEX + Main.maxTilesY);

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
            ColorDefs[172] = Constants.Terrafirma_Color.WOODEN_SINK;
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
            ColorDefs[314] = Constants.Terrafirma_Color.MINECART_TRACKS;
            ColorDefs[315] = Constants.Terrafirma_Color.CORALSTONE_BLOCK;
            ColorDefs[316] = Constants.Terrafirma_Color.BLUE_JELLYFISH_JAR;
            ColorDefs[317] = Constants.Terrafirma_Color.GREEN_JELLYFISH_JAR;
            ColorDefs[318] = Constants.Terrafirma_Color.PINK_JELLYFISH_JAR;
            ColorDefs[319] = Constants.Terrafirma_Color.SHIP_IN_A_BOTTLE;
            ColorDefs[320] = Constants.Terrafirma_Color.SEAWEED_PLANTER;
            ColorDefs[321] = Constants.Terrafirma_Color.BOREAL_WOOD;
            ColorDefs[322] = Constants.Terrafirma_Color.PALM_WOOD;
            ColorDefs[323] = Constants.Terrafirma_Color.PILLAR;
            ColorDefs[324] = Constants.Terrafirma_Color.SEASHELL;
            ColorDefs[325] = Constants.Terrafirma_Color.TIN_PLATING;
            ColorDefs[326] = Constants.Terrafirma_Color.WATERFALL_BLOCK;
            ColorDefs[327] = Constants.Terrafirma_Color.LAVAFALL_BLOCK;
            ColorDefs[328] = Constants.Terrafirma_Color.CONFETTI_BLOCK;
            ColorDefs[329] = Constants.Terrafirma_Color.MIDNIGHT_CONFETTI_BLOCK;
            ColorDefs[330] = Constants.Terrafirma_Color.COPPER_COIN_PILE;
            ColorDefs[331] = Constants.Terrafirma_Color.SILVER_COIN_PILE;
            ColorDefs[332] = Constants.Terrafirma_Color.GOLD_COIN_PILE;
            ColorDefs[333] = Constants.Terrafirma_Color.PLATINUM_COIN_PILE;
            ColorDefs[334] = Constants.Terrafirma_Color.WEAPON_RACK;
            ColorDefs[335] = Constants.Terrafirma_Color.FIREWORKS_BOX;
            ColorDefs[336] = Constants.Terrafirma_Color.LIVING_FIRE_BLOCK;
            ColorDefs[337] = Constants.Terrafirma_Color.CHARACTER_STATUES;
            ColorDefs[338] = Constants.Terrafirma_Color.FIREWORK_FOUNTAIN;
            ColorDefs[339] = Constants.Terrafirma_Color.GRASSHOPPER_CAGE;
            ColorDefs[340] = Constants.Terrafirma_Color.LIVING_CURSED_FIRE_BLOCK;
            ColorDefs[341] = Constants.Terrafirma_Color.LIVING_DEMON_FIRE_BLOCK;
            ColorDefs[342] = Constants.Terrafirma_Color.LIVING_FROST_FIRE_BLOCK;
            ColorDefs[343] = Constants.Terrafirma_Color.LIVING_ICHOR_BLOCK;
            ColorDefs[344] = Constants.Terrafirma_Color.LIVING_ULTRABRIGHT_FIRE_BLOCK;
            ColorDefs[345] = Constants.Terrafirma_Color.HONEYFALL_BLOCK;
            ColorDefs[346] = Constants.Terrafirma_Color.CHLOROPHYTE_BRICK;
            ColorDefs[347] = Constants.Terrafirma_Color.CRIMTANE_BRICK;
            ColorDefs[348] = Constants.Terrafirma_Color.SHROOMITE_PLATING;
            ColorDefs[349] = Constants.Terrafirma_Color.SHROOMITE;
            ColorDefs[350] = Constants.Terrafirma_Color.MARTIAN_CONDUIT_PLATING;
            ColorDefs[351] = Constants.Terrafirma_Color.SMOKE_BLOCK;
            ColorDefs[352] = Constants.Terrafirma_Color.ROOTS;
            ColorDefs[353] = Constants.Terrafirma_Color.VINE_ROPE;
            ColorDefs[354] = Constants.Terrafirma_Color.BEWITCHING_TABLE;
            ColorDefs[355] = Constants.Terrafirma_Color.ALCHEMY_TABLE;
            ColorDefs[356] = Constants.Terrafirma_Color.SUNDIAL;
            ColorDefs[357] = Constants.Terrafirma_Color.MARBLE_BLOCK;
            ColorDefs[358] = Constants.Terrafirma_Color.GOLD_BIRD_CAGE;
            ColorDefs[359] = Constants.Terrafirma_Color.GOLD_BUNNY_CAGE;
            ColorDefs[360] = Constants.Terrafirma_Color.GOLD_BUTTERFLY_JAR;
            ColorDefs[361] = Constants.Terrafirma_Color.GOLD_FROG_CAGE;
            ColorDefs[362] = Constants.Terrafirma_Color.GOLD_GRASSHOPPER_CAGE;
            ColorDefs[363] = Constants.Terrafirma_Color.GOLD_MOUSE_CAGE;
            ColorDefs[364] = Constants.Terrafirma_Color.GOLD_WORM_CAGE;
            ColorDefs[365] = Constants.Terrafirma_Color.SILK_ROPE;
            ColorDefs[366] = Constants.Terrafirma_Color.WEB_ROPE;
            ColorDefs[367] = Constants.Terrafirma_Color.MARBLE;
            ColorDefs[368] = Constants.Terrafirma_Color.GRANITE;
            ColorDefs[369] = Constants.Terrafirma_Color.GRANITE_BLOCK;
            ColorDefs[370] = Constants.Terrafirma_Color.METEORITE_BRICK;
            ColorDefs[371] = Constants.Terrafirma_Color.PINK_SLIME_BLOCK;
            ColorDefs[372] = Constants.Terrafirma_Color.PEACE_CANDLE;
            ColorDefs[373] = Constants.Terrafirma_Color.MAGIC_WATER_DROPPER;
            ColorDefs[374] = Constants.Terrafirma_Color.MAGIC_LAVA_DROPPER;
            ColorDefs[375] = Constants.Terrafirma_Color.MAGIC_HONEY_DROPPER;
            ColorDefs[376] = Constants.Terrafirma_Color.CRATE;
            ColorDefs[377] = Constants.Terrafirma_Color.SHARPENING_STATION;
            ColorDefs[378] = Constants.Terrafirma_Color.TARGET_DUMMY;
            ColorDefs[379] = Constants.Terrafirma_Color.BUBBLE;
            ColorDefs[380] = Constants.Terrafirma_Color.PLANTER_BOXES;
            ColorDefs[381] = Constants.Terrafirma_Color.HEATED_STONE;
            ColorDefs[382] = Constants.Terrafirma_Color.FLOWER_VINES;
            ColorDefs[383] = Constants.Terrafirma_Color.LIVING_MAHOGANY;
            ColorDefs[384] = Constants.Terrafirma_Color.RICH_MAHOGANY_LEAF;
            ColorDefs[385] = Constants.Terrafirma_Color.CRYSTAL_BLOCK;
            ColorDefs[386] = Constants.Terrafirma_Color.OPEN_TRAP_DOOR;
            ColorDefs[387] = Constants.Terrafirma_Color.CLOSED_TRAP_DOOR;
            ColorDefs[388] = Constants.Terrafirma_Color.CLOSED_TALL_GATE;
            ColorDefs[389] = Constants.Terrafirma_Color.OPEN_TALL_GATE;
            ColorDefs[390] = Constants.Terrafirma_Color.LAVA_LAMP;
            ColorDefs[391] = Constants.Terrafirma_Color.ENCHANTED_NIGHTCRAWLER_CAGE;
            ColorDefs[392] = Constants.Terrafirma_Color.BUGGY_CAGE;
            ColorDefs[393] = Constants.Terrafirma_Color.GRUBBY_CAGE;
            ColorDefs[394] = Constants.Terrafirma_Color.SLUGGY_CAGE;
            ColorDefs[395] = Constants.Terrafirma_Color.ITEM_FRAME;
            ColorDefs[396] = Constants.Terrafirma_Color.SANDSTONE;
            ColorDefs[397] = Constants.Terrafirma_Color.HARDENED_SAND;
            ColorDefs[398] = Constants.Terrafirma_Color.CORRUPT_HARDENED_SAND;
            ColorDefs[399] = Constants.Terrafirma_Color.CRIMSON_HARDENED_SAND;
            ColorDefs[400] = Constants.Terrafirma_Color.CORRUPT_SANDSTONE;
            ColorDefs[401] = Constants.Terrafirma_Color.CRISMON_SANDSTONE;
            ColorDefs[402] = Constants.Terrafirma_Color.HALLOW_HARDENED_SAND;
            ColorDefs[403] = Constants.Terrafirma_Color.HALLOW_SANDSTONE;
            ColorDefs[404] = Constants.Terrafirma_Color.DESERT_FOSSIL_BLOCK;
            ColorDefs[405] = Constants.Terrafirma_Color.FIREPLACE;
            ColorDefs[406] = Constants.Terrafirma_Color.CHIMNEY;
            ColorDefs[407] = Constants.Terrafirma_Color.FOSSIL_ORE;
            ColorDefs[408] = Constants.Terrafirma_Color.LUNAR_ORE;
            ColorDefs[409] = Constants.Terrafirma_Color.LUNAR_BRICK;
            ColorDefs[410] = Constants.Terrafirma_Color.MONOLITHS;
            ColorDefs[411] = Constants.Terrafirma_Color.DETONATOR;
            ColorDefs[412] = Constants.Terrafirma_Color.LUNAR_CRAFTING_STATION;
            ColorDefs[413] = Constants.Terrafirma_Color.RED_SQUIRREL_CAGE;
            ColorDefs[414] = Constants.Terrafirma_Color.GOLD_SQUIRREL_CAGE;
            ColorDefs[415] = Constants.Terrafirma_Color.SOLAR_FRAGMENT_BLOCK;
            ColorDefs[416] = Constants.Terrafirma_Color.VORTEX_FRAGMENT_BLOCK;
            ColorDefs[417] = Constants.Terrafirma_Color.NEBULA_FRAGMENT_BLOCK;
            ColorDefs[418] = Constants.Terrafirma_Color.STARDUST_FRAGMENT_BLOCK;
            ColorDefs[419] = Constants.Terrafirma_Color.LOGIC_GATE_LAMP;
            ColorDefs[420] = Constants.Terrafirma_Color.LOGIC_GATES;
            ColorDefs[421] = Constants.Terrafirma_Color.CONVEYOR_BELT_CLOCKWISE;
            ColorDefs[422] = Constants.Terrafirma_Color.CONVEYOR_BELT_COUNTER_CLOCKWISE;
            ColorDefs[423] = Constants.Terrafirma_Color.LOGIC_SENSORS;
            ColorDefs[424] = Constants.Terrafirma_Color.JUNCTION_BOX;
            ColorDefs[425] = Constants.Terrafirma_Color.ANNOUNCEMENT_BOX;
            ColorDefs[426] = Constants.Terrafirma_Color.RED_TEAM_BLOCK;
            ColorDefs[427] = Constants.Terrafirma_Color.RED_TEAM_PLATFORM;
            ColorDefs[428] = Constants.Terrafirma_Color.WEIGHTED_PRESSURE_PLATES;
            ColorDefs[429] = Constants.Terrafirma_Color.WIRE_BULB;
            ColorDefs[430] = Constants.Terrafirma_Color.GREEN_TEAM_BLOCK;
            ColorDefs[431] = Constants.Terrafirma_Color.BLUE_TEAM_BLOCK;
            ColorDefs[432] = Constants.Terrafirma_Color.YELLOW_TEAM_BLOCK;
            ColorDefs[433] = Constants.Terrafirma_Color.PINK_TEAM_BLOCK;
            ColorDefs[434] = Constants.Terrafirma_Color.WHITE_TEAM_BLOCK;
            ColorDefs[435] = Constants.Terrafirma_Color.GREEN_TEAM_PLATFORM;
            ColorDefs[436] = Constants.Terrafirma_Color.BLUE_TEAM_PLATFORM;
            ColorDefs[437] = Constants.Terrafirma_Color.YELLOW_TEAM_PLATFORM;
            ColorDefs[438] = Constants.Terrafirma_Color.PINK_TEAM_PLATFORM;
            ColorDefs[439] = Constants.Terrafirma_Color.WHITE_TEAM_PLATFORM;
            ColorDefs[440] = Constants.Terrafirma_Color.GEM_LOCKS;
            ColorDefs[441] = Constants.Terrafirma_Color.TRAPPED_CHESTS;
            ColorDefs[442] = Constants.Terrafirma_Color.TEAL_PRESSURE_PAD;
            ColorDefs[443] = Constants.Terrafirma_Color.GEYSER;
            ColorDefs[444] = Constants.Terrafirma_Color.BEE_HIVE;
            ColorDefs[445] = Constants.Terrafirma_Color.PIXEL_BOX;
            ColorDefs[446] = Constants.Terrafirma_Color.SILLY_PINK_BALLOON;
            ColorDefs[447] = Constants.Terrafirma_Color.SILLY_PURPLE_BALLOON;
            ColorDefs[448] = Constants.Terrafirma_Color.SILLY_GREEN_BALLOON;
            ColorDefs[449] = Constants.Terrafirma_Color.BLUE_STREAMER;
            ColorDefs[450] = Constants.Terrafirma_Color.GREEN_STREAMER;
            ColorDefs[451] = Constants.Terrafirma_Color.PINK_STREAMER;
            ColorDefs[452] = Constants.Terrafirma_Color.SILLY_BALLOON_MACHINE;
            ColorDefs[453] = Constants.Terrafirma_Color.SILLY_TIED_BALLOON;
            ColorDefs[454] = Constants.Terrafirma_Color.PIGRONATA;
            ColorDefs[455] = Constants.Terrafirma_Color.PARTY_CENTER;
            ColorDefs[456] = Constants.Terrafirma_Color.SILLY_TIED_BUNDLE_OF_BALLOONS;
            ColorDefs[457] = Constants.Terrafirma_Color.PARTY_PRESENT;
            ColorDefs[458] = Constants.Terrafirma_Color.SANDFALL;
            ColorDefs[459] = Constants.Terrafirma_Color.SNOWFALL;
            ColorDefs[460] = Constants.Terrafirma_Color.SNOW_CLOUD;
            ColorDefs[461] = Constants.Terrafirma_Color.SAND_DRIP;
            ColorDefs[462] = Constants.Terrafirma_Color.DESERT_SPIRIT_LAMP;
            ColorDefs[463] = Constants.Terrafirma_Color.DEFENDERS_FORGE;
            ColorDefs[464] = Constants.Terrafirma_Color.WAR_TABLE;
            ColorDefs[465] = Constants.Terrafirma_Color.WAR_TABLE_BANNER;
            ColorDefs[466] = Constants.Terrafirma_Color.ELDER_CYSTAL_STAND;
            ColorDefs[467] = Constants.Terrafirma_Color.CHESTS_2;
            ColorDefs[468] = Constants.Terrafirma_Color.TRAPPED_CHESTS_2;
            ColorDefs[469] = Constants.Terrafirma_Color.TABLES_2;

            for (int i = TILE_END_INDEX + 1; i < WALL_START_INDEX; i++)
            {
                ColorDefs[i] = System.Drawing.Color.Magenta;
            }

            //walls
            ColorDefs[WALL_START_INDEX] = Constants.Terrafirma_Color.STONE_WALL;
            ColorDefs[1001] = Constants.Terrafirma_Color.DIRT_WALL;
            ColorDefs[1002] = Constants.Terrafirma_Color.STONE_WALL_2;
            ColorDefs[1003] = Constants.Terrafirma_Color.WOOD_WALL;
            ColorDefs[1004] = Constants.Terrafirma_Color.GRAY_BRICK_WALL;
            ColorDefs[1005] = Constants.Terrafirma_Color.RED_BRICK_WALL;
            ColorDefs[1006] = Constants.Terrafirma_Color.BLUE_BRICK_WALL;
            ColorDefs[1007] = Constants.Terrafirma_Color.GREEN_BRICK_WALL;
            ColorDefs[1008] = Constants.Terrafirma_Color.PINK_BRICK_WALL;
            ColorDefs[1009] = Constants.Terrafirma_Color.GOLD_BRICK_WALL;
            ColorDefs[1010] = Constants.Terrafirma_Color.SILVER_BRICK_WALL;
            ColorDefs[1011] = Constants.Terrafirma_Color.COPPER_BRICK_WALL;
            ColorDefs[1012] = Constants.Terrafirma_Color.HELLSTONE_BRICK_WALL;
            ColorDefs[1013] = Constants.Terrafirma_Color.OBSIDIAN_WALL;
            ColorDefs[1014] = Constants.Terrafirma_Color.MUD_WALL;
            ColorDefs[1015] = Constants.Terrafirma_Color.DIRT_WALL_2;
            ColorDefs[1016] = Constants.Terrafirma_Color.BLUE_BRICK_WALL_2;
            ColorDefs[1017] = Constants.Terrafirma_Color.GREEN_BRICK_WALL_2;
            ColorDefs[1018] = Constants.Terrafirma_Color.PINK_BRICK_WALL_2;
            ColorDefs[1019] = Constants.Terrafirma_Color.OBSIDIAN_BRICK_WALL;
            ColorDefs[1020] = Constants.Terrafirma_Color.GLASS_WALL;
            ColorDefs[1021] = Constants.Terrafirma_Color.PEARLSTONE_BRICK_WALL;
            ColorDefs[1022] = Constants.Terrafirma_Color.IRIDESCENT_BRICK_WALL;
            ColorDefs[1023] = Constants.Terrafirma_Color.MUDSTONE_BRICK_WALL;
            ColorDefs[1024] = Constants.Terrafirma_Color.COBALT_BRICK_WALL;
            ColorDefs[1025] = Constants.Terrafirma_Color.MYTHRIL_BRICK_WALL;
            ColorDefs[1026] = Constants.Terrafirma_Color.PLANKED_WALL;
            ColorDefs[1027] = Constants.Terrafirma_Color.PEARLSTONE_WALL;
            ColorDefs[1028] = Constants.Terrafirma_Color.CANDY_CANE_WALL;
            ColorDefs[1029] = Constants.Terrafirma_Color.GREEN_CANDY_CANE_WALL;
            ColorDefs[1030] = Constants.Terrafirma_Color.SNOW_BRICK_WALL;
            ColorDefs[1031] = Constants.Terrafirma_Color.ADAMANTITE_BEAM_WALL;
            ColorDefs[1032] = Constants.Terrafirma_Color.DEMONITE_BRICK_WALL;
            ColorDefs[1033] = Constants.Terrafirma_Color.SANDSTONE_BRICK_WALL;
            ColorDefs[1034] = Constants.Terrafirma_Color.EBONSTONE_BRICK_WALL;
            ColorDefs[1035] = Constants.Terrafirma_Color.RED_STUCCO_WALL;
            ColorDefs[1036] = Constants.Terrafirma_Color.YELLOW_STUCCO_WALL;
            ColorDefs[1037] = Constants.Terrafirma_Color.GREEN_STUCCO_WALL;
            ColorDefs[1038] = Constants.Terrafirma_Color.GRAY_STUCCO_WALL;
            ColorDefs[1039] = Constants.Terrafirma_Color.SNOW_WALL;
            ColorDefs[1040] = Constants.Terrafirma_Color.EBONWOOD_WALL;
            ColorDefs[1041] = Constants.Terrafirma_Color.RICH_MAHOGANY_WALL;
            ColorDefs[1042] = Constants.Terrafirma_Color.PEARLWOOD_WALL;
            ColorDefs[1043] = Constants.Terrafirma_Color.RAINBOW_BRICK_WALL;
            ColorDefs[1044] = Constants.Terrafirma_Color.TIN_BRICK_WALL;
            ColorDefs[1045] = Constants.Terrafirma_Color.TUNGSTEN_BRICK_WALL;
            ColorDefs[1046] = Constants.Terrafirma_Color.PLATINUM_BRICK_WALL;
            ColorDefs[1047] = Constants.Terrafirma_Color.AMETHYST_WALL;
            ColorDefs[1048] = Constants.Terrafirma_Color.TOPAZ_WALL;
            ColorDefs[1049] = Constants.Terrafirma_Color.SAPPHIRE_WALL;
            ColorDefs[1050] = Constants.Terrafirma_Color.EMERALD_WALL;
            ColorDefs[1051] = Constants.Terrafirma_Color.RUBY_WALL;
            ColorDefs[1052] = Constants.Terrafirma_Color.DIAMOND_WALL;
            ColorDefs[1053] = Constants.Terrafirma_Color.MOSS_WALL;
            ColorDefs[1054] = Constants.Terrafirma_Color.GREEN_MOSS_WALL;
            ColorDefs[1055] = Constants.Terrafirma_Color.RED_MOSS_WALL;
            ColorDefs[1056] = Constants.Terrafirma_Color.BLUE_MOSS_WALL;
            ColorDefs[1057] = Constants.Terrafirma_Color.PURPLE_MOSS_WALL;
            ColorDefs[1058] = Constants.Terrafirma_Color.ROCKY_DIRT_WALL;
            ColorDefs[1059] = Constants.Terrafirma_Color.LIVING_LEAF_WALL;
            ColorDefs[1060] = Constants.Terrafirma_Color.ROCK_WALL;
            ColorDefs[1061] = Constants.Terrafirma_Color.SPIDER_CAVE_WALL;
            ColorDefs[1062] = Constants.Terrafirma_Color.SHRUB_WALL;
            ColorDefs[1063] = Constants.Terrafirma_Color.SHRUB_WALL_2;
            ColorDefs[1064] = Constants.Terrafirma_Color.SHRUB_WALL_3;
            ColorDefs[1065] = Constants.Terrafirma_Color.GRASS_WALL;
            ColorDefs[1066] = Constants.Terrafirma_Color.JUNGLE_WALL;
            ColorDefs[1067] = Constants.Terrafirma_Color.FLOWER_WALL;
            ColorDefs[1068] = Constants.Terrafirma_Color.SHRUB_WALL_4;
            ColorDefs[1069] = Constants.Terrafirma_Color.SHRUB_WALL_5;
            ColorDefs[1070] = Constants.Terrafirma_Color.ICE_WALL;
            ColorDefs[1071] = Constants.Terrafirma_Color.CACTUS_WALL;
            ColorDefs[1072] = Constants.Terrafirma_Color.CLOUD_WALL;
            ColorDefs[1073] = Constants.Terrafirma_Color.MUSHROOM_WALL;
            ColorDefs[1074] = Constants.Terrafirma_Color.BONE_BLOCK_WALL;
            ColorDefs[1075] = Constants.Terrafirma_Color.SLIME_BLOCK_WALL;
            ColorDefs[1076] = Constants.Terrafirma_Color.FLESH_BLOCK_WALL;
            ColorDefs[1077] = Constants.Terrafirma_Color.LIVING_WOOD_WALL;
            ColorDefs[1078] = Constants.Terrafirma_Color.CAVE_WALL;
            ColorDefs[1079] = Constants.Terrafirma_Color.MUSHROOM_WALL_2;
            ColorDefs[1080] = Constants.Terrafirma_Color.CLAY_WALL;
            ColorDefs[1081] = Constants.Terrafirma_Color.DISC_WALL;
            ColorDefs[1082] = Constants.Terrafirma_Color.HELLSTONE_WALL;
            ColorDefs[1083] = Constants.Terrafirma_Color.ICE_BRICK_WALL;
            ColorDefs[1084] = Constants.Terrafirma_Color.SHADEWOOD_WALL;
            ColorDefs[1085] = Constants.Terrafirma_Color.HIVE_WALL;
            ColorDefs[1086] = Constants.Terrafirma_Color.LIHZAHRD_BRICK_WALL;
            ColorDefs[1087] = Constants.Terrafirma_Color.PURPLE_STAINED_GLASS;
            ColorDefs[1088] = Constants.Terrafirma_Color.YELLOW_STAINED_GLASS;
            ColorDefs[1089] = Constants.Terrafirma_Color.BLUE_STAINED_GLASS;
            ColorDefs[1090] = Constants.Terrafirma_Color.GREEN_STAINED_GLASS;
            ColorDefs[1091] = Constants.Terrafirma_Color.RED_STAINED_GLASS;
            ColorDefs[1092] = Constants.Terrafirma_Color.MULTICOLORED_STAINED_GLASS;
            ColorDefs[1093] = Constants.Terrafirma_Color.SMALL_BLUE_BRICK_WALL;
            ColorDefs[1094] = Constants.Terrafirma_Color.BLUE_BLOCK_WALL;
            ColorDefs[1095] = Constants.Terrafirma_Color.PINK_BLOCK_WALL;
            ColorDefs[1096] = Constants.Terrafirma_Color.SMALL_PINK_BRICK_WALL;
            ColorDefs[1097] = Constants.Terrafirma_Color.SMALL_GREEN_BRICK_WALL;
            ColorDefs[1098] = Constants.Terrafirma_Color.GREEN_BLOCK_WALL;
            ColorDefs[1099] = Constants.Terrafirma_Color.BLUE_SLAB_WALL;
            ColorDefs[1100] = Constants.Terrafirma_Color.BLUE_TILED_WALL;
            ColorDefs[1101] = Constants.Terrafirma_Color.PINK_SLAB_WALL;
            ColorDefs[1102] = Constants.Terrafirma_Color.PINK_TILED_WALL;
            ColorDefs[1103] = Constants.Terrafirma_Color.GREEN_SLAB_WALL;
            ColorDefs[1104] = Constants.Terrafirma_Color.GREEN_TILED_WALL;
            ColorDefs[1105] = Constants.Terrafirma_Color.WOODEN_FENCE;
            ColorDefs[1106] = Constants.Terrafirma_Color.LEAD_FENCE;
            ColorDefs[1107] = Constants.Terrafirma_Color.HIVE_WALL_2;
            ColorDefs[1108] = Constants.Terrafirma_Color.PALLADIUM_COLUMN_WALL;
            ColorDefs[1109] = Constants.Terrafirma_Color.BUBBLEGUM_BLOCK_WALL;
            ColorDefs[1110] = Constants.Terrafirma_Color.TITANSTONE_BLOCK_WALL;
            ColorDefs[1111] = Constants.Terrafirma_Color.LIHZAHRD_BRICK_WALL_2;
            ColorDefs[1112] = Constants.Terrafirma_Color.PUMPKIN_WALL;
            ColorDefs[1113] = Constants.Terrafirma_Color.HAY_WALL;
            ColorDefs[1114] = Constants.Terrafirma_Color.SPOOKY_WOOD_WALL;
            ColorDefs[1115] = Constants.Terrafirma_Color.CHRISTMAS_TREE_WALLPAPER;
            ColorDefs[1116] = Constants.Terrafirma_Color.ORNAMENT_WALLPAPER;
            ColorDefs[1117] = Constants.Terrafirma_Color.CANDY_CANE_WALLPAPER;
            ColorDefs[1118] = Constants.Terrafirma_Color.FESTIVE_WALLPAPER;
            ColorDefs[1119] = Constants.Terrafirma_Color.STARS_WALLPAPER;
            ColorDefs[1120] = Constants.Terrafirma_Color.SQUIGGLES_WALLPAPER;
            ColorDefs[1121] = Constants.Terrafirma_Color.SNOWFLAKE_WALLPAPER;
            ColorDefs[1122] = Constants.Terrafirma_Color.KRAMPUS_HORN_WALLPAPER;
            ColorDefs[1123] = Constants.Terrafirma_Color.BLUEGREEN_WALLPAPER;
            ColorDefs[1124] = Constants.Terrafirma_Color.GRINCH_FINGER_WALLPAPER;
            ColorDefs[1125] = Constants.Terrafirma_Color.FANCY_GREY_WALLPAPER;
            ColorDefs[1126] = Constants.Terrafirma_Color.ICE_FLOE_WALLPAPER;
            ColorDefs[1127] = Constants.Terrafirma_Color.MUSIC_WALLPAPER;
            ColorDefs[1128] = Constants.Terrafirma_Color.PURPLE_RAIN_WALLPAPER;
            ColorDefs[1129] = Constants.Terrafirma_Color.RAINBOW_WALLPAPER;
            ColorDefs[1130] = Constants.Terrafirma_Color.SPARKLE_STONE_WALLPAPER;
            ColorDefs[1131] = Constants.Terrafirma_Color.STARLIT_HEAVEN_WALLPAPER;
            ColorDefs[1132] = Constants.Terrafirma_Color.BUBBLE_WALLPAPER;
            ColorDefs[1133] = Constants.Terrafirma_Color.COPPER_PIPE_WALLPAPER;
            ColorDefs[1134] = Constants.Terrafirma_Color.DUCKY_WALLPAPER;
            ColorDefs[1135] = Constants.Terrafirma_Color.WATERFALL_WALL;
            ColorDefs[1136] = Constants.Terrafirma_Color.LAVAFALL_WALL;
            ColorDefs[1137] = Constants.Terrafirma_Color.EBONWOOD_FENCE;
            ColorDefs[1138] = Constants.Terrafirma_Color.RICH_MAHOGANY_FENCE;
            ColorDefs[1139] = Constants.Terrafirma_Color.PEARLWOOD_FENCE;
            ColorDefs[1140] = Constants.Terrafirma_Color.SHADEWOOD_FENCE;
            ColorDefs[1141] = Constants.Terrafirma_Color.WHITE_DYNASTY_WALL;
            ColorDefs[1142] = Constants.Terrafirma_Color.BLUE_DYNASTY_WALL;
            ColorDefs[1143] = Constants.Terrafirma_Color.ARCANE_RUNE_WALL;
            ColorDefs[1144] = Constants.Terrafirma_Color.IRON_FENCE;
            ColorDefs[1145] = Constants.Terrafirma_Color.COPPER_PLATING_WALL;
            ColorDefs[1146] = Constants.Terrafirma_Color.STONE_SLAB_WALL;
            ColorDefs[1147] = Constants.Terrafirma_Color.SAIL;
            ColorDefs[1148] = Constants.Terrafirma_Color.BOREAL_WOOD_WALL;
            ColorDefs[1149] = Constants.Terrafirma_Color.BOREAL_WOOD_FENCE;
            ColorDefs[1150] = Constants.Terrafirma_Color.PALM_WOOD_WALL;
            ColorDefs[1151] = Constants.Terrafirma_Color.PALM_WOOD_FENCE;
            ColorDefs[1152] = Constants.Terrafirma_Color.AMBER_GEMSPARK_WALL;
            ColorDefs[1153] = Constants.Terrafirma_Color.AMETHYST_GEMSPARK_WALL;
            ColorDefs[1154] = Constants.Terrafirma_Color.DIAMOND_GEMSPARK_WALL;
            ColorDefs[1155] = Constants.Terrafirma_Color.EMERALD_GEMSPARK_WALL;
            ColorDefs[1156] = Constants.Terrafirma_Color.OFFLINE_AMBER_GEMSPARK_WALL;
            ColorDefs[1157] = Constants.Terrafirma_Color.OFFLINE_AMETHYST_GEMSPARK_WALL;
            ColorDefs[1158] = Constants.Terrafirma_Color.OFFLINE_DIAMOND_GEMSPARK_WALL;
            ColorDefs[1159] = Constants.Terrafirma_Color.OFFLINE_EMERALD_GEMSPARK_WALL;
            ColorDefs[1160] = Constants.Terrafirma_Color.OFFLINE_RUBY_GEMSPARK_WALL;
            ColorDefs[1161] = Constants.Terrafirma_Color.OFFLINE_SAPPHIRE_GEMSPARK_WALL;
            ColorDefs[1162] = Constants.Terrafirma_Color.OFFLINE_TOPAZ_GEMSPARK_WALL;
            ColorDefs[1163] = Constants.Terrafirma_Color.RUBY_GEMSPARK_WALL;
            ColorDefs[1164] = Constants.Terrafirma_Color.SAPPHIRE_GEMSPARK_WALL;
            ColorDefs[1165] = Constants.Terrafirma_Color.TOPAZ_GEMSPARK_WALL;
            ColorDefs[1166] = Constants.Terrafirma_Color.TIN_PLATING_WALL;
            ColorDefs[1167] = Constants.Terrafirma_Color.CONFETTI_WALL;
            ColorDefs[1168] = Constants.Terrafirma_Color.MIDNIGHT_CONFETTI_WALL;
            ColorDefs[1169] = Constants.Terrafirma_Color.WALL_170;
            ColorDefs[1170] = Constants.Terrafirma_Color.WALL_171;
            ColorDefs[1171] = Constants.Terrafirma_Color.HONEYFALL_WALL;
            ColorDefs[1172] = Constants.Terrafirma_Color.CHLOROPHYTE_BRICK_WALL;
            ColorDefs[1173] = Constants.Terrafirma_Color.CRIMTANE_BRICK_WALL;
            ColorDefs[1174] = Constants.Terrafirma_Color.SHROOMITE_PLATING_WALL;
            ColorDefs[1175] = Constants.Terrafirma_Color.MARTIAN_CONDUIT_WALL;
            ColorDefs[1176] = Constants.Terrafirma_Color.HELLSTONE_BRICK_WALL_2;
            ColorDefs[1177] = Constants.Terrafirma_Color.MARBLE_WALL;
            ColorDefs[1178] = Constants.Terrafirma_Color.MARBLE_BLOCK_WALL;
            ColorDefs[1179] = Constants.Terrafirma_Color.GRANITE_WALL;
            ColorDefs[1180] = Constants.Terrafirma_Color.GRANITE_BLOCK_WALL;
            ColorDefs[1181] = Constants.Terrafirma_Color.METEORITE_BRICK_WALL;
            ColorDefs[1182] = Constants.Terrafirma_Color.MARBLE_WALL_2;
            ColorDefs[1183] = Constants.Terrafirma_Color.GRANITE_WALL_2;
            ColorDefs[1184] = Constants.Terrafirma_Color.WALL_185;
            ColorDefs[1185] = Constants.Terrafirma_Color.CRYSTAL_WALL;
            ColorDefs[1186] = Constants.Terrafirma_Color.SANDSTONE_WALL;
            ColorDefs[1187] = Constants.Terrafirma_Color.WALL_188;
            ColorDefs[1188] = Constants.Terrafirma_Color.WALL_189;
            ColorDefs[1189] = Constants.Terrafirma_Color.WALL_190;
            ColorDefs[1190] = Constants.Terrafirma_Color.WALL_191;
            ColorDefs[1191] = Constants.Terrafirma_Color.WALL_192;
            ColorDefs[1192] = Constants.Terrafirma_Color.WALL_193;
            ColorDefs[1193] = Constants.Terrafirma_Color.WALL_194;
            ColorDefs[1194] = Constants.Terrafirma_Color.WALL_195;
            ColorDefs[1195] = Constants.Terrafirma_Color.WALL_196;
            ColorDefs[1196] = Constants.Terrafirma_Color.WALL_197;
            ColorDefs[1197] = Constants.Terrafirma_Color.WALL_198;
            ColorDefs[1198] = Constants.Terrafirma_Color.WALL_199;
            ColorDefs[1199] = Constants.Terrafirma_Color.WALL_200;
            ColorDefs[1200] = Constants.Terrafirma_Color.WALL_201;
            ColorDefs[1201] = Constants.Terrafirma_Color.WALL_202;
            ColorDefs[1202] = Constants.Terrafirma_Color.WALL_203;
            ColorDefs[1203] = Constants.Terrafirma_Color.WALL_204;
            ColorDefs[1204] = Constants.Terrafirma_Color.WALL_205;
            ColorDefs[1205] = Constants.Terrafirma_Color.WALL_206;
            ColorDefs[1206] = Constants.Terrafirma_Color.WALL_207;
            ColorDefs[1207] = Constants.Terrafirma_Color.WALL_208;
            ColorDefs[1208] = Constants.Terrafirma_Color.WALL_209;
            ColorDefs[1209] = Constants.Terrafirma_Color.WALL_210;
            ColorDefs[1210] = Constants.Terrafirma_Color.WALL_211;
            ColorDefs[1211] = Constants.Terrafirma_Color.WALL_212;
            ColorDefs[1212] = Constants.Terrafirma_Color.WALL_213;
            ColorDefs[1213] = Constants.Terrafirma_Color.WALL_214;
            ColorDefs[1214] = Constants.Terrafirma_Color.WALL_215;
            ColorDefs[1215] = Constants.Terrafirma_Color.HARDENED_SAND_WALL;
            ColorDefs[1216] = Constants.Terrafirma_Color.CORRUPT_HARDENED_SAND_WALL;
            ColorDefs[1217] = Constants.Terrafirma_Color.CRIMSON_HARDENED_SAND_WALL;
            ColorDefs[1218] = Constants.Terrafirma_Color.HALLOW_HARDENED_SAND_WALL;
            ColorDefs[1219] = Constants.Terrafirma_Color.CORRUPT_SANDSTONE_WALL;
            ColorDefs[1220] = Constants.Terrafirma_Color.CRIMSON_SANDSTONE_WALL;
            ColorDefs[1221] = Constants.Terrafirma_Color.HALLOW_SANDSTONE_WALL;
            ColorDefs[1222] = Constants.Terrafirma_Color.DESERT_FOSSIL_WALL;
            ColorDefs[1223] = Constants.Terrafirma_Color.LUNAR_BRICK_WALL;
            ColorDefs[1224] = Constants.Terrafirma_Color.COG_WALL;
            ColorDefs[1225] = Constants.Terrafirma_Color.SANDFALL_WALL;
            ColorDefs[1226] = Constants.Terrafirma_Color.SNOWFALL_WALL;
            ColorDefs[1227] = Constants.Terrafirma_Color.SILLY_PINK_BALLOON_WALL;
            ColorDefs[1228] = Constants.Terrafirma_Color.SILLY_PURPLE_BALLOON_WALL;
            ColorDefs[1229] = Constants.Terrafirma_Color.SILLY_GREEN_BALLOON_WALL;

            // this is for faster performace
            // rather than converting from Color to UInt32 alot.
            UInt32Defs = new Dictionary<int, UInt32>(FADE_START_INDEX + Main.maxTilesY);

            //adds sky and earth

            for (int i = FADE_START_INDEX; i < Main.worldSurface + FADE_START_INDEX; i++)
            {
                UInt32Defs[i] = 0x84AAF8;
                ColorDefs[i] = Constants.Terrafirma_Color.SKY;
            }
            for (int i = (int)Main.worldSurface + FADE_START_INDEX; i < (int)Main.rockLayer + FADE_START_INDEX; i++)
            {
                UInt32Defs[i] = 0x583D2E;
                ColorDefs[i] = Constants.Terrafirma_Color.EARTH;
            }
            for (int i = (int)Main.rockLayer + FADE_START_INDEX; i < Main.maxTilesY + FADE_START_INDEX; i++)
            {
                UInt32Defs[i] = 0x000000;
                ColorDefs[i] = Constants.Terrafirma_Color.HELL;
            }

            //adds the background fade in both ColorDefs and UInt32Defs
            for (int y = (int)Main.rockLayer; y < Main.maxTilesY; y++)
            {
                double alpha = (double)(y - Main.rockLayer) / (double)(Main.maxTilesY - Main.rockLayer);
                UInt32 c = alphaBlend(0x4A433C, 0x000000, alpha);   // (rockcolor, hellcolor, alpha)
                UInt32Defs[y + FADE_START_INDEX] = c;
                ColorDefs[y + FADE_START_INDEX] = toColor(c);
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
            UInt32Defs[172] = 0x8C6850;
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
            UInt32Defs[206] = 0x7cafc9;
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
            UInt32Defs[314] = 0xA3A6A8;
            UInt32Defs[315] = 0xC94642;
            UInt32Defs[316] = 0x7792E2;
            UInt32Defs[317] = 0x76E381;
            UInt32Defs[318] = 0xE2A0DA;
            UInt32Defs[319] = 0xC8F6FE;
            UInt32Defs[320] = 0xCBB997;
            UInt32Defs[321] = 0x604D40;
            UInt32Defs[322] = 0x8F6D3F;
            UInt32Defs[323] = 0xB68D56;
            UInt32Defs[324] = 0xE4D5AD;
            UInt32Defs[325] = 0x817D5D;
            UInt32Defs[326] = 0x093DBF;
            UInt32Defs[327] = 0xFD2003;
            UInt32Defs[328] = 0xC8F6FE;
            UInt32Defs[329] = 0xFF20D8;
            UInt32Defs[330] = 0xE2764C;
            UInt32Defs[331] = 0x464D50;
            UInt32Defs[332] = 0xCCB548;
            UInt32Defs[333] = 0xBEBEB2;
            UInt32Defs[334] = 0x78553C;
            UInt32Defs[335] = 0xE3B994;
            UInt32Defs[336] = 0xFE7902;
            UInt32Defs[337] = 0x606460;
            UInt32Defs[338] = 0x19C762;
            UInt32Defs[339] = 0x57ADBD;
            UInt32Defs[340] = 0xB3FC00;
            UInt32Defs[341] = 0x660CD4;
            UInt32Defs[342] = 0x00BAF2;
            UInt32Defs[343] = 0xFECA50;
            UInt32Defs[344] = 0x83FCF5;
            UInt32Defs[345] = 0xFF9C0C;
            UInt32Defs[346] = 0x246133;
            UInt32Defs[347] = 0xA42A49;
            UInt32Defs[348] = 0x2215A4;
            UInt32Defs[349] = 0x37589D;
            UInt32Defs[350] = 0x629AB3;
            UInt32Defs[351] = 0x191919;
            UInt32Defs[352] = 0x883231;
            UInt32Defs[353] = 0x1E9648;
            UInt32Defs[354] = 0x4C2200;
            UInt32Defs[355] = 0x6D4E47;
            UInt32Defs[356] = 0x006887;
            UInt32Defs[357] = 0xA8B2CC;
            UInt32Defs[358] = 0xCBB349;
            UInt32Defs[359] = 0xCBB349;
            UInt32Defs[360] = 0xCBB349;
            UInt32Defs[361] = 0xCBB349;
            UInt32Defs[362] = 0xCBB349;
            UInt32Defs[363] = 0xCBB349;
            UInt32Defs[364] = 0xCBB349;
            UInt32Defs[365] = 0x757FB9;
            UInt32Defs[366] = 0xDFE8E9;
            UInt32Defs[367] = 0xC3CEE3;
            UInt32Defs[368] = 0x322E68;
            UInt32Defs[369] = 0x221F47;
            UInt32Defs[370] = 0x7F74C2;
            UInt32Defs[371] = 0xF97FC8;
            UInt32Defs[372] = 0xFE95D2;
            UInt32Defs[373] = 0xffffff; // todo: fix me...
            UInt32Defs[374] = 0xffffff; // todo: fix me...
            UInt32Defs[375] = 0xffffff; // todo: fix me...
            UInt32Defs[376] = 0x906850;
            UInt32Defs[377] = 0x6C6257;
            UInt32Defs[378] = 0xDDB487;
            UInt32Defs[379] = 0xD3D2FF;
            UInt32Defs[380] = 0x946B50;
            UInt32Defs[381] = 0xFE7902;
            UInt32Defs[382] = 0x1E9648;
            UInt32Defs[383] = 0xDD8890;
            UInt32Defs[384] = 0x476D0B;
            UInt32Defs[385] = 0x0B377F;
            UInt32Defs[386] = 0x4F3A2E;
            UInt32Defs[387] = 0x6B4F3F;
            UInt32Defs[388] = 0x503B30;
            UInt32Defs[389] = 0x2E2119;
            UInt32Defs[390] = 0xFD2003;
            UInt32Defs[391] = 0x57ADBD;
            UInt32Defs[392] = 0x57ADBD;
            UInt32Defs[393] = 0x57ADBD;
            UInt32Defs[394] = 0x57ADBD;
            UInt32Defs[395] = 0x634732;
            UInt32Defs[396] = 0xB36741;
            UInt32Defs[397] = 0xD49458;
            UInt32Defs[398] = 0x604475;
            UInt32Defs[399] = 0x4D4C42;
            UInt32Defs[400] = 0x604475;
            UInt32Defs[401] = 0x573937;
            UInt32Defs[402] = 0xB18ABA;
            UInt32Defs[403] = 0x9E71A4;
            UInt32Defs[404] = 0x8C543C;
            UInt32Defs[405] = 0xFD3E03;
            UInt32Defs[406] = 0x8C8C8C;
            UInt32Defs[407] = 0xFFE384;
            UInt32Defs[408] = 0x5EE5A3;
            UInt32Defs[409] = 0x3A3736;
            UInt32Defs[410] = 0x22DD97;
            UInt32Defs[411] = 0xC90303;
            UInt32Defs[412] = 0x936857;
            UInt32Defs[413] = 0x57ADBD;
            UInt32Defs[414] = 0xCBB349;
            UInt32Defs[415] = 0xFE9E23;
            UInt32Defs[416] = 0x00A0AA;
            UInt32Defs[417] = 0xA057EA;
            UInt32Defs[418] = 0x5057B6;
            UInt32Defs[419] = 0x585F70;
            UInt32Defs[420] = 0x757D97;
            UInt32Defs[421] = 0x494646;
            UInt32Defs[422] = 0x494646;
            UInt32Defs[423] = 0x9F2500;
            UInt32Defs[424] = 0x929BBB;
            UInt32Defs[425] = 0xAEC3D7;
            UInt32Defs[426] = 0x4D0B23;
            UInt32Defs[427] = 0x771634;
            UInt32Defs[428] = 0xFCA259;
            UInt32Defs[429] = 0x3F3F3F;
            UInt32Defs[430] = 0x17774F;
            UInt32Defs[431] = 0x173677;
            UInt32Defs[432] = 0x774417;
            UInt32Defs[433] = 0x4A1777;
            UInt32Defs[434] = 0x4E526D;
            UInt32Defs[435] = 0x27A860;
            UInt32Defs[436] = 0x275EA8;
            UInt32Defs[437] = 0xA87927;
            UInt32Defs[438] = 0x6F27A8;
            UInt32Defs[439] = 0x9694AE;
            UInt32Defs[440] = 0x9B1512;
            UInt32Defs[441] = 0x946B50;
            UInt32Defs[442] = 0x0390C9;
            UInt32Defs[443] = 0x7B7B7B;
            UInt32Defs[444] = 0xBFB07C;
            UInt32Defs[445] = 0x373749;
            UInt32Defs[446] = 0xFF4298;
            UInt32Defs[447] = 0xB384FF;
            UInt32Defs[448] = 0x00CEB4;
            UInt32Defs[449] = 0x5BBAF0;
            UInt32Defs[450] = 0x5CF05B;
            UInt32Defs[451] = 0xF05B93;
            UInt32Defs[452] = 0xFF96B5;
            UInt32Defs[453] = 0xB384FF;
            UInt32Defs[454] = 0xAE10B0;
            UInt32Defs[455] = 0x30FF6E;
            UInt32Defs[456] = 0xB384FF;
            UInt32Defs[457] = 0x96A4CE;
            UInt32Defs[458] = 0xD3C66F;
            UInt32Defs[459] = 0xBEDFE8;
            UInt32Defs[460] = 0x8DA3B5;
            UInt32Defs[461] = 0xFFDE64;
            UInt32Defs[462] = 0xE7B21C;
            UInt32Defs[463] = 0x9BD6F0;
            UInt32Defs[464] = 0xE9B780;
            UInt32Defs[465] = 0x3354C3;
            UInt32Defs[466] = 0xCD9949;
            UInt32Defs[467] = 0x4B137E;
            UInt32Defs[468] = 0x4B137E;
            UInt32Defs[469] = 0x7F5C45;

            // unknown
            for (int i = TILE_END_INDEX + 1; i < WALL_START_INDEX; i++)
            {
                UInt32Defs[i] = 0xFF00FF;
            }

            //walls
            UInt32Defs[1000] = 0x343434;
            UInt32Defs[1001] = 0x583D2E;
            UInt32Defs[1002] = 0x3D3A4E;
            UInt32Defs[1003] = 0x523C2D;
            UInt32Defs[1004] = 0x3C3C3C;
            UInt32Defs[1005] = 0x5B1E1E;
            UInt32Defs[1006] = 0x3D4456;
            UInt32Defs[1007] = 0x384147;
            UInt32Defs[1008] = 0x603E5C;
            UInt32Defs[1009] = 0x64510A;
            UInt32Defs[1010] = 0x616969;
            UInt32Defs[1011] = 0x532E16;
            UInt32Defs[1012] = 0x492929;
            UInt32Defs[1013] = 0x020202;
            UInt32Defs[1014] = 0x31282B;
            UInt32Defs[1015] = 0x583D2E;
            UInt32Defs[1016] = 0x492929;
            UInt32Defs[1017] = 0x384147;
            UInt32Defs[1018] = 0x603E5C;
            UInt32Defs[1019] = 0x0F0F0F;
            UInt32Defs[1020] = 0x12242C;
            UInt32Defs[1021] = 0x827C79;
            UInt32Defs[1022] = 0x56494B;
            UInt32Defs[1023] = 0x403033;
            UInt32Defs[1024] = 0x0B233E;
            UInt32Defs[1025] = 0x3C5B3A;
            UInt32Defs[1026] = 0x3A291D;
            UInt32Defs[1027] = 0x515465;
            UInt32Defs[1028] = 0x581717;
            UInt32Defs[1029] = 0x2C6A26;
            UInt32Defs[1030] = 0x6B7577;
            UInt32Defs[1031] = 0x5a122b;
            UInt32Defs[1032] = 0x46455e;
            UInt32Defs[1033] = 0x6c6748;
            UInt32Defs[1034] = 0x333346;
            UInt32Defs[1035] = 0x704b46;
            UInt32Defs[1036] = 0x574f30;
            UInt32Defs[1037] = 0x5a5d73;
            UInt32Defs[1038] = 0x6d6d6b;
            UInt32Defs[1039] = 0x577173;
            UInt32Defs[1040] = 0x3a3944;
            UInt32Defs[1041] = 0x4e1e20;
            UInt32Defs[1042] = 0x776c51;
            UInt32Defs[1043] = 0x414141;
            UInt32Defs[1044] = 0x3c3b33;
            UInt32Defs[1045] = 0x586758;
            UInt32Defs[1046] = 0x666b75;
            UInt32Defs[1047] = 0x3c2452;
            UInt32Defs[1048] = 0x614b1c;
            UInt32Defs[1049] = 0x32527d;
            UInt32Defs[1050] = 0x1c4924;
            UInt32Defs[1051] = 0x431d22;
            UInt32Defs[1052] = 0x234249;
            UInt32Defs[1053] = 0x304a42;
            UInt32Defs[1054] = 0x4f4f33;
            UInt32Defs[1055] = 0x4e3938;
            UInt32Defs[1056] = 0x334452;
            UInt32Defs[1057] = 0x432f49;
            UInt32Defs[1058] = 0x583d2e;
            UInt32Defs[1059] = 0x013e17;
            UInt32Defs[1060] = 0x362619;
            UInt32Defs[1061] = 0x242424;
            UInt32Defs[1062] = 0x235f39;
            UInt32Defs[1063] = 0x3f5f23;
            UInt32Defs[1064] = 0x1e5030;
            UInt32Defs[1065] = 0x1e5030;
            UInt32Defs[1066] = 0x35501e;
            UInt32Defs[1067] = 0x1e5030;
            UInt32Defs[1068] = 0x333250;
            UInt32Defs[1069] = 0x143736;
            UInt32Defs[1070] = 0x306085;
            UInt32Defs[1071] = 0x34540c;
            UInt32Defs[1072] = 0xe4ecee;
            UInt32Defs[1073] = 0x313b8c;
            UInt32Defs[1074] = 0x63633c;
            UInt32Defs[1075] = 0x213c79;
            UInt32Defs[1076] = 0x3d0d10;
            UInt32Defs[1077] = 0x513426;
            UInt32Defs[1078] = 0x332f60;
            UInt32Defs[1079] = 0x313b8c;
            UInt32Defs[1080] = 0x844545;
            UInt32Defs[1081] = 0x4d4022;
            UInt32Defs[1082] = 0x3f1c21;
            UInt32Defs[1083] = 0x596f72;
            UInt32Defs[1084] = 0x2a363f;
            UInt32Defs[1085] = 0x8c5131;
            UInt32Defs[1086] = 0x320f08;
            UInt32Defs[1087] = 0x6c3c82;
            UInt32Defs[1088] = 0x787a45;
            UInt32Defs[1089] = 0x35498a;
            UInt32Defs[1090] = 0x45794f;
            UInt32Defs[1091] = 0x8a3535;
            UInt32Defs[1092] = 0x99255c;
            UInt32Defs[1093] = 0x303f46;
            UInt32Defs[1094] = 0x393643;
            UInt32Defs[1095] = 0x593e5f;
            UInt32Defs[1096] = 0x4e3245;
            UInt32Defs[1097] = 0x384745;
            UInt32Defs[1098] = 0x383e47;
            UInt32Defs[1099] = 0x303f46;
            UInt32Defs[1100] = 0x393643;
            UInt32Defs[1101] = 0x48324d;
            UInt32Defs[1102] = 0x4e3245;
            UInt32Defs[1103] = 0x445747;
            UInt32Defs[1104] = 0x383e47;
            UInt32Defs[1105] = 0x604438;
            UInt32Defs[1106] = 0x3c3c3c;
            UInt32Defs[1107] = 0x8c5131;
            UInt32Defs[1108] = 0x5e1911;
            UInt32Defs[1109] = 0x994996;
            UInt32Defs[1110] = 0x1f1814;
            UInt32Defs[1111] = 0x320F08;
            UInt32Defs[1112] = 0x803700;
            UInt32Defs[1113] = 0x745E19;
            UInt32Defs[1114] = 0x33243F;
            UInt32Defs[1115] = 0x5A1B1C;
            UInt32Defs[1116] = 0x7D7A71;
            UInt32Defs[1117] = 0x073112;
            UInt32Defs[1118] = 0x69181B;
            UInt32Defs[1119] = 0x111F41;
            UInt32Defs[1120] = 0xC6C3B7;
            UInt32Defs[1121] = 0x6E6E7E;
            UInt32Defs[1122] = 0x85755F;
            UInt32Defs[1123] = 0x100340;
            UInt32Defs[1124] = 0x748352;
            UInt32Defs[1125] = 0x8F919D;
            UInt32Defs[1126] = 0x8CB9E2;
            UInt32Defs[1127] = 0x6F1975;
            UInt32Defs[1128] = 0x311B91;
            UInt32Defs[1129] = 0x6505C1;
            UInt32Defs[1130] = 0x575E7D;
            UInt32Defs[1131] = 0x060606;
            UInt32Defs[1132] = 0x4D3FA9;
            UInt32Defs[1133] = 0xC25C17;
            UInt32Defs[1134] = 0x177FA8;
            UInt32Defs[1135] = 0x285697;
            UInt32Defs[1136] = 0xB7210F;
            UInt32Defs[1137] = 0x9989A5;
            UInt32Defs[1138] = 0xA36368;
            UInt32Defs[1139] = 0x776C51;
            UInt32Defs[1140] = 0x586976;
            UInt32Defs[1141] = 0xE0DACC;
            UInt32Defs[1142] = 0x516863;
            UInt32Defs[1143] = 0x3E1C57;
            UInt32Defs[1144] = 0x919191;
            UInt32Defs[1145] = 0x703712;
            UInt32Defs[1146] = 0x4C4C4C;
            UInt32Defs[1147] = 0xE5DAA1;
            UInt32Defs[1148] = 0x504237;
            UInt32Defs[1149] = 0x6B5647;
            UInt32Defs[1150] = 0x664B22;
            UInt32Defs[1151] = 0xB68D56;
            UInt32Defs[1152] = 0xFF743F;
            UInt32Defs[1153] = 0xBF3FFF;
            UInt32Defs[1154] = 0xDBDBE8;
            UInt32Defs[1155] = 0x3FFF47;
            UInt32Defs[1156] = 0x763F25;
            UInt32Defs[1157] = 0x512576;
            UInt32Defs[1158] = 0x404359;
            UInt32Defs[1159] = 0x257634;
            UInt32Defs[1160] = 0x76253A;
            UInt32Defs[1161] = 0x252576;
            UInt32Defs[1162] = 0x767125;
            UInt32Defs[1163] = 0xFF3F3F;
            UInt32Defs[1164] = 0x3F51FF;
            UInt32Defs[1165] = 0xEFFF3F;
            UInt32Defs[1166] = 0x464433;
            UInt32Defs[1167] = 0x0C6A8A;
            UInt32Defs[1168] = 0x850C77;
            UInt32Defs[1169] = 0x3B2716;
            UInt32Defs[1170] = 0x3B2716;
            UInt32Defs[1171] = 0xA36000;
            UInt32Defs[1172] = 0x082A27;
            UInt32Defs[1173] = 0x451D26;
            UInt32Defs[1174] = 0x0F0969;
            UInt32Defs[1175] = 0x2C2934;
            UInt32Defs[1176] = 0x5D2B2B;
            UInt32Defs[1177] = 0x868DA0;
            UInt32Defs[1178] = 0x9AA2B1;
            UInt32Defs[1179] = 0x0C0A19;
            UInt32Defs[1180] = 0x1A1733;
            UInt32Defs[1181] = 0x4A4781;
            UInt32Defs[1182] = 0x9AA2B1;
            UInt32Defs[1183] = 0x141D49;
            UInt32Defs[1184] = 0x525252;
            UInt32Defs[1185] = 0x260942;
            UInt32Defs[1186] = 0xA8603B;
            UInt32Defs[1187] = 0x523F50;
            UInt32Defs[1188] = 0x41334D;
            UInt32Defs[1189] = 0x3E3A51;
            UInt32Defs[1190] = 0x5E4364;
            UInt32Defs[1191] = 0x904334;
            UInt32Defs[1192] = 0x4F1317;
            UInt32Defs[1193] = 0x2F0A0C;
            UInt32Defs[1194] = 0x792A2F;
            UInt32Defs[1195] = 0x70503E;
            UInt32Defs[1196] = 0x7F5E4C;
            UInt32Defs[1197] = 0x614333;
            UInt32Defs[1198] = 0x70503E;
            UInt32Defs[1199] = 0x553952;
            UInt32Defs[1200] = 0x7B6474;
            UInt32Defs[1201] = 0xA7307E;
            UInt32Defs[1202] = 0x96407E;
            UInt32Defs[1203] = 0x53311E;
            UInt32Defs[1204] = 0x5F7652;
            UInt32Defs[1205] = 0x464541;
            UInt32Defs[1206] = 0x53311E;
            UInt32Defs[1207] = 0x3E3D3C;
            UInt32Defs[1208] = 0x554246;
            UInt32Defs[1209] = 0x393434;
            UInt32Defs[1210] = 0x58433B;
            UInt32Defs[1211] = 0x585750;
            UInt32Defs[1212] = 0x383735;
            UInt32Defs[1213] = 0x4C343C;
            UInt32Defs[1214] = 0x524A4D;
            UInt32Defs[1215] = 0xB97F3B;
            UInt32Defs[1216] = 0x463F5E;
            UInt32Defs[1217] = 0x33332D;
            UInt32Defs[1218] = 0x72618C;
            UInt32Defs[1219] = 0x53356A;
            UInt32Defs[1220] = 0x56372F;
            UInt32Defs[1221] = 0x3E4765;
            UInt32Defs[1222] = 0x572612;
            UInt32Defs[1223] = 0x262424;
            UInt32Defs[1224] = 0x444444;
            UInt32Defs[1225] = 0x948A4A;
            UInt32Defs[1226] = 0x5F89BF;
            UInt32Defs[1227] = 0xA0024B;
            UInt32Defs[1228] = 0x6437A4;
            UInt32Defs[1229] = 0x007565;

            //list for when dimming the world for highlighting
            DimColorDefs = new Dictionary<int, System.Drawing.Color>(FADE_START_INDEX + Main.maxTilesY);
            DimUInt32Defs = new Dictionary<int, UInt32>(FADE_START_INDEX + Main.maxTilesY);
        }
    }
}
