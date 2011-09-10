using Terraria_Server;
using Terraria_Server.Commands;
using NDesk.Options;
using System;
using Terraria_Server.Logging;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;

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
				var savefromcommand = false;
				string filename = "world-now.png";
				string cs = colorscheme;
				
				var options = new OptionSet ()
				{
					{ "t|timestamp", v => timestamp = true },
					{ "n|name=", v => filename = v },
					{ "L|reload", v => reload = true },
					{ "s|save", v => savefromcommand = true },
					{ "p|path=", v => p = v },
					{ "c|colorscheme=", v => cs = v },
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
					"Settings: mapoutputpath=", p, ", ",
					"colorscheme=", cs);
					if ( !(Directory.Exists(p)) ){
						msg = string.Concat ( msg , "  (DOESNT EXIST)" );
						ProgramLog.Error.Log ("<map> ERROR: Loaded Directory does not exist.");
					}
					ProgramLog.Admin.Log ("<map> " + msg);
					//sender.sendMessage ("map: " + msg);
					
					if ( !(cs=="MoreTerra" || cs=="Terrafirma") ){
						ProgramLog.Error.Log ("<map> ERROR: please change colorscheme");
					}
				}
				
				if(savefromcommand){
					properties.setValue ("color-scheme", cs);
					properties.setValue ("mapoutput-path", p);
					properties.Save();
				}
				
				if (args.Count == 0 && isEnabled) {
					if(!reload && Directory.Exists(p)){
						Stopwatch stopwatch = new Stopwatch ();
						Bitmap blank = null;
						if(cs=="MoreTerra" || cs=="Terrafirma"){
							Program.server.notifyOps("Saving Image...", true);
							stopwatch.Start ();
							blank = new Bitmap (Main.maxTilesX, Main.maxTilesY, PixelFormat.Format32bppArgb);
						}
						Bitmap world = blank;
						if(cs=="Terrafirma"){
							Graphics graphicsHandle = Graphics.FromImage ((Image)blank);
							graphicsHandle.FillRectangle (new SolidBrush (Constants.Terrafirma_Color.SKY), 0, 0, blank.Width, blank.Height);
							InitializeMapperDefs2();
							Thread imagethread;
							imagethread = new Thread(mapWorld2);
							MapPlugin.bmp = blank;
							mapWorld2 ();
							world = MapPlugin.bmp;
							imagethread.Abort();
						}
						else if(cs=="MoreTerra"){
							Graphics graphicsHandle = Graphics.FromImage ((Image)blank);
							graphicsHandle.FillRectangle (new SolidBrush (Constants.MoreTerra_Color.SKY), 0, 0, blank.Width, blank.Height);
							InitializeMapperDefs();
							Thread imagethread;
							imagethread = new Thread(mapWorld);
							MapPlugin.bmp = blank;
							mapWorld();
							world = MapPlugin.bmp;
							imagethread.Abort();
						}
						else{
							ProgramLog.Error.Log ("Save ERROR: check colorscheme");
						}
						if(cs=="MoreTerra" || cs=="Terrafirma"){
							Program.server.notifyOps("Saving Data...", true);
							world.Save (string.Concat (p, Path.DirectorySeparatorChar, filename));
							stopwatch.Stop ();
							ProgramLog.Log ("Save duration: " + stopwatch.Elapsed.Seconds + " Second(s)");
							Program.server.notifyOps("Saving Complete.", true);
							MapPlugin.bmp = null;
						}
					}
					if( !(Directory.Exists(p)) ){
						sender.sendMessage ("map: "+p+" does not exist.");
						ProgramLog.Error.Log ("<map> ERROR: Loaded Directory does not exist.");
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

