using Terraria_Server;
using Terraria_Server.Commands;
using NDesk.Options;
using System;
using Terraria_Server.Logging;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace MapPlugin
{
	public partial class MapPlugin
	{
		void MapCommand (Server server, ISender sender, ArgumentList argz)
		{
			try {
				var p = mapoutputpath;
				var timestamp = false;
				var reload = false;
				string filename = "world-now.png";
				var options = new OptionSet ()
				{
					{ "t|timestamp", v => timestamp = true },
					{ "n|name=", v => filename = v },
					{ "L|reload", v => reload = true },
				};
				var args = options.Parse (argz);
				
				if (timestamp) {
					DateTime value = DateTime.Now;
					string time = value.ToString ("yyyy-MM-dd_HH-mm-ss");
					filename = string.Concat ("terraria-", time, ".png");
				}
				
				if (reload) {
					sender.sendMessage ("map: Reloaded settings database, entries: " + properties.Count);
					properties.Load ();
					var msg = string.Concat (
					"Settings: mapoutputpath=", p);
					if ( !(Directory.Exists(mapoutputpath)) ){
						msg = string.Concat ( msg , "  (DOESNT EXIST)" );
						ProgramLog.Admin.Log ("<map> Loaded Directory does not exist.");
					}
					ProgramLog.Admin.Log ("<map> " + msg);
					//sender.sendMessage ("map: " + msg);
				}
				
				if (args.Count == 0) {
					if(!reload && Directory.Exists(mapoutputpath)){
						Program.server.notifyOps("Saving Image...", true);
						Stopwatch stopwatch = new Stopwatch ();
						stopwatch.Start ();
						Bitmap blank = new Bitmap (Main.maxTilesX, Main.maxTilesY, PixelFormat.Format32bppArgb);
						Graphics graphicsHandle = Graphics.FromImage ((Image)blank);
						graphicsHandle.FillRectangle (new SolidBrush (Constants.Colors.SKY), 0, 0, blank.Width, blank.Height);
						Bitmap world = mapWorld (blank);
						Program.server.notifyOps("Saving Data...", true);
						world.Save (string.Concat (mapoutputpath, Path.DirectorySeparatorChar, filename));
						stopwatch.Stop ();
						ProgramLog.Log ("Save duration: " + stopwatch.Elapsed.Seconds + " Second(s)");
						Program.server.notifyOps("Saving Complete.", true);
					}
					if( !(Directory.Exists(mapoutputpath)) ){
						sender.sendMessage ("map: "+mapoutputpath+" does not exist.");
					}
				} else {
					throw new CommandError ("");
				}
			} catch (OptionException) {
				throw new CommandError ("");
			}
		}
	}
}

