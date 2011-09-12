using Terraria_Server;
using System.IO;
using System;

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
		
		private string int32ToHexString(long i)
		{
			byte[] int32Bytes;
			int32Bytes = BitConverter.GetBytes(i);
			return String.Format("{0}{1}{2}{3}",
				padString(int32Bytes[0].ToString("X")),
				padString(int32Bytes[1].ToString("X")),
				padString(int32Bytes[2].ToString("X")),
				padString(int32Bytes[3].ToString("X")));
		}
		
		private string padString(string s)
		{
			while (s.Length < 2) s = "0" + s;
			while (s.Length < 3) s = " " + s;
			return s;
		}
	}
}

