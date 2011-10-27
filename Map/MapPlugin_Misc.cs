using Terraria_Server;
using System.IO;
using System;
using System.Drawing;

namespace MapPlugin
{
    public partial class MapPlugin
    {
        private static void CreateDirectory(string dirPath)
        {
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
        }

        static Player FindPlayer(string name)
        {
            name = name.ToLower();

            foreach (var p in Main.players)
            {
                if (p != null && p.Name != null && p.Name.ToLower() == name)
                    return p;
            }

            return null;
        }

        //this comes from terrafirma
        private UInt32 alphaBlend(UInt32 from, UInt32 to, double alpha)
        {
            uint fr = (from >> 16) & 0xff;
            uint fg = (from >> 8) & 0xff;
            uint fb = from & 0xff;
            uint tr = (to >> 16) & 0xff;
            uint tg = (to >> 8) & 0xff;
            uint tb = to & 0xff;
            fr = (uint)(tr * alpha + fr * (1 - alpha));
            fg = (uint)(tg * alpha + fg * (1 - alpha));
            fb = (uint)(tb * alpha + fb * (1 - alpha));

            //this fixes the zero alpha problem
            UInt32 result = (fr << 16) | (fg << 8) | fb;
            return (0xff000000 + (result & 0x00ff0000) + (result & 0x0000ff00) + (result & 0x000000ff));
        }

        private Color dimC(UInt32 c)
        {
            return toColor(dimI(c));
        }

        private UInt32 highlightI(UInt32 c)
        {
            return alphaBlend(c, 0xff88ff, 0.9);
        }

        private UInt32 dimI(UInt32 c)
        {
            return alphaBlend(0, c, 0.3);
        }

        private Color toColor(UInt32 c)
        {
            return ColorTranslator.FromHtml("#" + String.Format("{0:X}", c));
        }

        //turns out that I need a translator from the tile and wall numbers
        //to the give ID since thats the itemlist returned by the highlight command
        // ***WORK IN PROGRESS***
        private void getGiveID(int tile, int wall)
        {
            //quick copy and paste from the worldmodify.cs file in TDSM source
            //to get started on this ID to ID function mapping
            int dropItem = 0;
            if (tile == 0 || tile == 2)
            {
                dropItem = 2;
            }
            else if (tile == 1)
            {
                dropItem = 3;
            }
            else if (tile == 3 || tile == 73)
            {
                dropItem = 283;
            }
            else if (tile == 4)
            {
                dropItem = 8;
            }
            else if (tile == 5)
            {
                dropItem = 27;
                dropItem = 9;
            }
            else if (tile == 6)
            {
                dropItem = 11;
            }
            else if (tile == 7)
            {
                dropItem = 12;
            }
            else if (tile == 8)
            {
                dropItem = 13;
            }
            else if (tile == 9)
            {
                dropItem = 14;
            }
            else if (tile == 13)
            {
                dropItem = 28;
                dropItem = 110;
                dropItem = 350;
                dropItem = 351;
                dropItem = 31;
            }
            else if (tile == 19)
            {
                dropItem = 94;
            }
            else if (tile == 22)
            {
                dropItem = 56;
            }
            else if (tile == 23)
            {
                dropItem = 2;
            }
            else if (tile == 25)
            {
                dropItem = 61;
            }
            else if (tile == 30)
            {
                dropItem = 9;
            }
            else if (tile == 33)
            {
                dropItem = 105;
            }
            else if (tile == 37)
            {
                dropItem = 116;
            }
            else if (tile == 38)
            {
                dropItem = 129;
            }
            else if (tile == 39)
            {
                dropItem = 131;
            }
            else if (tile == 40)
            {
                dropItem = 133;
            }
            else if (tile == 41)
            {
                dropItem = 134;
            }
            else if (tile == 43)
            {
                dropItem = 137;
            }
            else if (tile == 44)
            {
                dropItem = 139;
            }
            else if (tile == 45)
            {
                dropItem = 141;
            }
            else if (tile == 46)
            {
                dropItem = 143;
            }
            else if (tile == 47)
            {
                dropItem = 145;
            }
            else if (tile == 48)
            {
                dropItem = 147;
            }
            else if (tile == 49)
            {
                dropItem = 148;
            }
            else if (tile == 51)
            {
                dropItem = 150;
            }
            else if (tile == 53)
            {
                dropItem = 169;
            }
            else if (tile != 54)
            {
                if (tile == 56)
                {
                    dropItem = 173;
                }
                else if (tile == 57)
                {
                    dropItem = 172;
                }
                else if (tile == 58)
                {
                    dropItem = 174;
                }
                else if (tile == 60)
                {
                    dropItem = 176;
                }
                else if (tile == 70)
                {
                    dropItem = 176;
                }
                else if (tile == 75)
                {
                    dropItem = 192;
                }
                else if (tile == 76)
                {
                    dropItem = 214;
                }
                else if (tile == 78)
                {
                    dropItem = 222;
                }
                else if (tile == 81)
                {
                    dropItem = 275;
                }
                else if (tile == 80)
                {
                    dropItem = 276;
                }
                else if (tile == 61 || tile == 74)
                {
                    dropItem = 223;
                    dropItem = 208;
                    dropItem = 195;
                }
                else if (tile == 59 || tile == 60)
                {
                    dropItem = 176;
                }
                else if (tile == 71 || tile == 72)
                {
                    dropItem = 194;
                    dropItem = 183;
                }
                else if (tile >= 63 && tile <= 68)
                {
                    dropItem = (int)(tile - 63 + 177);
                }
                else if (tile == 50)
                {
                    dropItem = 165;
                    dropItem = 149;
                }

                int num2 = 0;
                if (wall == 1)
                {
                    num2 = 26;
                }
                if (wall == 4)
                {
                    num2 = 93;
                }
                if (wall == 5)
                {
                    num2 = 130;
                }
                if (wall == 6)
                {
                    num2 = 132;
                }
                if (wall == 7)
                {
                    num2 = 135;
                }
                if (wall == 8)
                {
                    num2 = 138;
                }
                if (wall == 9)
                {
                    num2 = 140;
                }
                if (wall == 10)
                {
                    num2 = 142;
                }
                if (wall == 11)
                {
                    num2 = 144;
                }
                if (wall == 12)
                {
                    num2 = 146;
                }
                if (wall == 14)
                {
                    num2 = 330;
                }
                if (wall == 16)
                {
                    num2 = 30;
                }
                if (wall == 17)
                {
                    num2 = 135;
                }
                if (wall == 18)
                {
                    num2 = 138;
                }
                if (wall == 19)
                {
                    num2 = 140;
                }
                if (wall == 20)
                {
                    num2 = 330;
                }
            }

        }

    }
}

