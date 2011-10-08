using Terraria_Server;
using System.IO;
using System;
using System.Threading;

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
		
		//this fixes the problem introduced in the new b36 API
		//I need the world size but plugins are now loaded before the world
		//so I make a new thread that waits until the variables are assigned
		//and then it pre-blend the colors
		private void startBlendThread()
		{
			Thread blendthread;
			blendthread = new Thread(preBlend);
			blendthread.Start();
			while (!blendthread.IsAlive); //wait for it to start
		}
		
		private void preBlend()
		{
			while(Main.maxTilesX==-1); //wait for maxTilesX to be assigned
			//UInt32Defs and ColorDefs for colors, and background fade in Terrafirma Color Scheme
			InitializeMapperDefs();
			InitializeMapperDefs2();
			
			//this pre blends colors for Terrafirma Color Scheme
			initBList();
		}
	}
}

