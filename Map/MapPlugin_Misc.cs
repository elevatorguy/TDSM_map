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

		static Player FindPlayer (string name)
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
            return (fr << 16) | (fg << 8) | fb;
        }

        private Color dimC(UInt32 c)
        {
            return ColorTranslator.FromHtml("#" + String.Format("{0:X}", dimI(c)));
        }

        private UInt32 highlightI(UInt32 c)
        {
            return alphaBlend(c, 0xff88ff, 0.9);
        }

        private UInt32 dimI(UInt32 c)
        {
            c = alphaBlend(0, c, 0.3);
            //this fixes the 0 alpha problem
            UInt32 result = 0xff000000 + (c & 0x00ff0000) + (c & 0x0000ff00) + (c & 0x000000ff);

            return result;
        }
	}
}

