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
using System.Collections.Generic;

namespace MapPlugin
{
	public partial class MapPlugin
	{
		public static string p;
		public static string filename;
        public static bool highlight;
        public static bool hlchests;
        private static int highlightID;
        public bool isMapping = false;

		void MapCommand ( ISender sender, ArgumentList argz)
		{
			try {
                if (isMapping)
                {
                    throw new CommandError("Still currently mapping.");
                }
				p = mapoutputpath;
				filename = "world-now.png";
				var timestamp = false;
				var reload = false;
                var highlight = false;
                string nameOrID = "";
				var savefromcommand = false;
				string cs = colorscheme;
				var options = new OptionSet ()
				{
					{ "t|timestamp", v => timestamp = true },
					{ "n|name=", v => filename = v },
					{ "L|reload", v => reload = true },
					{ "s|save", v => savefromcommand = true },
					{ "p|path=", v => p = v },
                    { "h|highlight=", v => { nameOrID = v; highlight = true; } },
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
                // chests are not an item so i draw them from the chest array
                if (highlight && nameOrID.ToLower() != "chest")  //the following is taken from Commands.cs from TDSM source. Thanks guys!!! ;)
                {
                    List<Int32> itemlist;
                    if (Server.TryFindItemByName(nameOrID, out itemlist) && itemlist.Count > 0)
                    {
                        if (itemlist.Count > 1)
                            throw new CommandError("There were {0} Items found regarding the specified name", itemlist.Count);

                        foreach (int id in itemlist)
                            highlightID = id;
                    }
                    else
                    {
                        int Id = -1;
                        try
                        {
                            Id = Int32.Parse(nameOrID);
                        }
                        catch
                        {
                            throw new CommandError("There were {0} Items found regarding the specified name", itemlist.Count);
                        }

                        if (Server.TryFindItemByType(Id, out itemlist) && itemlist.Count > 0)
                        {
                            if (itemlist.Count > 1)
                                throw new CommandError("There were {0} Items found regarding the specified name", itemlist.Count);

                            foreach (int id in itemlist)
                                highlightID = id;
                        }
                        else
                        {
                            throw new CommandError("There were no Items found regarding the specified Item Id/Name");
                        }
                    }
                    //end copy
                    hlchests = false;
                }
                else
                {
                    hlchests = true;
                }

				if (args.Count == 0 && isEnabled) {
					if(!reload && Directory.Exists(p)){
						if(cs=="Terrafirma"){
                            isMapping = true;
                            //for now highlighting is only in terrafirma color scheme
                            MapPlugin.highlight = highlight;

							Thread imagethread;
							imagethread = new Thread(mapWorld2);
							imagethread.Start();
							while (!imagethread.IsAlive);
								// the thread terminates itself since there is no while loop in mapWorld2
						}
						else if(cs=="MoreTerra"){
                            isMapping = true;
							Thread imagethread;
							imagethread = new Thread(mapWorld);
							imagethread.Start();
							while (!imagethread.IsAlive);
								// the thread terminates itself since there is no while loop in mapWorld
						}
						else{
							ProgramLog.Error.Log ("Save ERROR: check colorscheme");
						}
						if( !(Directory.Exists(p)) ){
						sender.sendMessage ("map: "+p+" does not exist.");
						ProgramLog.Error.Log ("<map> ERROR: Loaded Directory does not exist.");
				}
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

