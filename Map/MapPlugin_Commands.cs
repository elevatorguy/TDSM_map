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
            bool autosave = false;
            if (argz.Contains("automap"))
            {
                autosave = true;
                argz.Remove("automap");
            }
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
                var autosaveedit = false;
				var options = new OptionSet ()
				{
					{ "t|timestamp", v => timestamp = true },
					{ "n|name=", v => filename = v },
					{ "L|reload", v => reload = true },
					{ "s|save", v => savefromcommand = true },
					{ "p|path=", v => p = v },
                    { "h|highlight=", v => { nameOrID = v; highlight = true; } },
					{ "c|colorscheme=", v => cs = v },
                    { "a|autosave", v => autosaveedit = true },
				};
				var args = options.Parse (argz);

                if (autosaveedit)
                {
                    if (autosaveenabled)
                    {
                        properties.setValue("autosave-enabled", "False");
                        sender.sendMessage("autosave disabled.");
                    }
                    else
                    {
                        properties.setValue("autosave-enabled", "True");
                        sender.sendMessage("autosave enabled.");
                    }
                    if (highlight)
                    {
                        if (highlightsearch(sender as Player, nameOrID))
                        {
                            properties.setValue("autosave-highlight", "True");
                            properties.setValue("autosave-highlightID", nameOrID);
                            sender.sendMessage("autosave highlight settings updated.");
                        }
                    }

                    if (timestamp)
                    {
                        if (autosavetimestamp)
                        {
                            properties.setValue("autosave-timestamp", "False");
                            sender.sendMessage("autosave now using regular name.");
                        }
                        else
                        {
                            properties.setValue("autosave-timestamp", "True");
                            sender.sendMessage("autosave now using timestamp.");
                        }
                    }

                    if (filename != "world-now.png")
                    {
                        properties.setValue("autosave-filename", filename);
                    }

                    properties.Save();
                    return;
                }
				
				if (reload || autosave) {
					sender.sendMessage ("map: Reloaded settings database, entries: " + properties.Count);
					properties.Load ();
					var msg = string.Concat (
					"Settings: mapoutputpath=", p, ", ",
					"colorscheme=", cs);
					if ( !(Directory.Exists(p)) ){
						msg = string.Concat ( msg , "  (DOESNT EXIST)" );
						ProgramLog.Error.Log ("<map> ERROR: Loaded Directory does not exist.");
					}
                    if (!autosave)
                    {
                        ProgramLog.Admin.Log("<map> " + msg);
                    }
					//sender.sendMessage ("map: " + msg);
					
					if ( !(cs=="MoreTerra" || cs=="Terrafirma") ){
						ProgramLog.Error.Log ("<map> ERROR: please change colorscheme");
					}
				}

                if (autosave)
                {
                    p = autosavepath;
                    filename = autosavename;
                    timestamp = autosavetimestamp;
                    if (autosavehighlight)
                    {
                        nameOrID = autosavehightlightID;
                    }
                }

                if (timestamp)
                {
                    DateTime value = DateTime.Now;
                    string time = value.ToString("yyyy-MM-dd_HH-mm-ss");
                    filename = string.Concat("terraria-", time, ".png");
                }
				if(savefromcommand){
					properties.setValue ("color-scheme", cs);
					properties.setValue ("mapoutput-path", p);
					properties.Save();
				}
                // chests are not an item so i draw them from the chest array
                if (highlight && nameOrID.ToLower() != "chest")  
                {
                    highlightsearch(sender as Player, nameOrID);
                    hlchests = false;
                }
                else
                {
                    if(nameOrID.ToLower() == "chest") //double checking
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
                            imagethread.Name = "Mapper";
							imagethread.Start();
							while (!imagethread.IsAlive);
								// the thread terminates itself since there is no while loop in mapWorld2
						}
						else if(cs=="MoreTerra"){
                            isMapping = true;
							Thread imagethread;
							imagethread = new Thread(mapWorld);
                            imagethread.Name = "Mapper";
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

        //the following is taken and modified from Commands.cs from TDSM source. Thanks guys!!! ;)
        public bool highlightsearch(Player player, string nameOrID)
        {
            List<ItemInfo> itemlist;

            if (Server.TryFindItemByName(nameOrID, out itemlist) && itemlist.Count > 0)
            {
                if (itemlist.Count > 1)
                {
                    player.sendMessage("There were " + itemlist.Count + " Items found regarding the specified name");
                    return false;
                }

                foreach (ItemInfo id in itemlist)
                    highlightID = id.Type;
            }
            else
            {
                player.sendMessage("There were no Items found regarding the specified Item Id/Name");
                return false;
            }

            return true;
        }

	}
}

