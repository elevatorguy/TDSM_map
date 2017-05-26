using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Terraria;

namespace Map
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

            foreach (var p in Main.player)
            {
                if (p != null && p.name != null && p.name.ToLower() == name)
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

        private System.Drawing.Color dimC(UInt32 c)
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

        private System.Drawing.Color toColor(UInt32 c)
        {
            return ColorTranslator.FromHtml("#" + String.Format("{0:X}", c));
        }

        //turns out that I need a translator from the tile and wall numbers
        //to the give ID since thats the itemlist returned by the highlight command
        private List<int> getGiveID(int tile, int wall)
        {
            //there were tiles with multiple give ID's
            //so I use a list of int(s) as the output
            List<int> list = new List<int>();
            if (tile == 0 || tile == 2)
            {
                list.Add(2);
            }
            else if (tile == 1)
            {
                list.Add(3);
            }
            else if (tile == 3 || tile == 73)
            {
                list.Add(283);
            }
            else if (tile == 4)
            {
                list.Add(8);
            }
            else if (tile == 5)
            {
                list.Add(27);
                list.Add(9);
            }
            else if (tile == 6)
            {
                list.Add(11);
            }
            else if (tile == 7)
            {
                list.Add(12);
            }
            else if (tile == 8)
            {
                list.Add(13);
            }
            else if (tile == 9)
            {
                list.Add(14);
            }
            else if (tile == 13)
            {
                list.Add(28);
                list.Add(110);
                list.Add(350);
                list.Add(351);
                list.Add(31);
            }
            else if (tile == 19)
            {
                list.Add(94);
            }
            else if (tile == 22)
            {
                list.Add(56);
            }
            else if (tile == 23)
            {
                list.Add(2);
            }
            else if (tile == 25)
            {
                list.Add(61);
            }
            else if (tile == 30)
            {
                list.Add(9);
            }
            else if (tile == 33)
            {
                list.Add(105);
            }
            else if (tile == 37)
            {
                list.Add(116);
            }
            else if (tile == 38)
            {
                list.Add(129);
            }
            else if (tile == 39)
            {
                list.Add(131);
            }
            else if (tile == 40)
            {
                list.Add(133);
            }
            else if (tile == 41)
            {
                list.Add(134);
            }
            else if (tile == 43)
            {
                list.Add(137);
            }
            else if (tile == 44)
            {
                list.Add(139);
            }
            else if (tile == 45)
            {
                list.Add(141);
            }
            else if (tile == 46)
            {
                list.Add(143);
            }
            else if (tile == 47)
            {
                list.Add(145);
            }
            else if (tile == 48)
            {
                list.Add(147);
            }
            else if (tile == 49)
            {
                list.Add(148);
            }
            else if (tile == 51)
            {
                list.Add(150);
            }
            else if (tile == 53)
            {
                list.Add(169);
            }
            else if (tile != 54)
            {
                if (tile == 56)
                {
                    list.Add(173);
                }
                else if (tile == 57)
                {
                    list.Add(172);
                }
                else if (tile == 58)
                {
                    list.Add(174);
                }
                else if (tile == 60)
                {
                    list.Add(176);
                }
                else if (tile == 70)
                {
                    list.Add(176);
                }
                else if (tile == 75)
                {
                    list.Add(192);
                }
                else if (tile == 76)
                {
                    list.Add(214);
                }
                else if (tile == 78)
                {
                    list.Add(222);
                }
                else if (tile == 81)
                {
                    list.Add(275);
                }
                else if (tile == 80)
                {
                    list.Add(276);
                }
                else if (tile == 61 || tile == 74)
                {
                    list.Add(223);
                    list.Add(208);
                    list.Add(195);
                }
                else if (tile == 59 || tile == 60)
                {
                    list.Add(176);
                }
                else if (tile == 71 || tile == 72)
                {
                    list.Add(194);
                    list.Add(183);
                }
                else if (tile >= 63 && tile <= 68)
                {
                    list.Add((int)(tile - 63 + 177));
                }
                else if (tile == 50)
                {
                    list.Add(165);
                    list.Add(149);
                }

            }
            if (wall == 1)
            {
                list.Add(26);
            }
            if (wall == 4)
            {
                list.Add(93);
            }
            if (wall == 5)
            {
                list.Add(130);
            }
            if (wall == 6)
            {
                list.Add(132);
            }
            if (wall == 7)
            {
                list.Add(135);
            }
            if (wall == 8)
            {
                list.Add(138);
            }
            if (wall == 9)
            {
                list.Add(140);
            }
            if (wall == 10)
            {
                list.Add(142);
            }
            if (wall == 11)
            {
                list.Add(144);
            }
            if (wall == 12)
            {
                list.Add(146);
            }
            if (wall == 14)
            {
                list.Add(330);
            }
            if (wall == 16)
            {
                list.Add(30);
            }
            if (wall == 17)
            {
                list.Add(135);
            }
            if (wall == 18)
            {
                list.Add(138);
            }
            if (wall == 19)
            {
                list.Add(140);
            }
            if (wall == 20)
            {
                list.Add(330);
            }
            return list;
        }
    }
}
