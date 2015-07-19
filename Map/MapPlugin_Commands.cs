using Terraria;
using TDSM.API.Command;
using NDesk.Options;
using System;
using TDSM.API.Logging;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using TDSM.Core.Definitions;

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
        public bool crop = false;
        public int x1 = 0;
        public int y1 = 0;
        public int x2 = 0;
        public int y2 = 0;

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
                highlightID = 0;
                hlchests = false;
                string nameOrID = "";
				var savefromcommand = false;
				string cs = colorscheme;
                var autosaveedit = false;
                string x1 = "";
                string x2 = "";
                string y1 = "";
                string y2 = "";
                crop = false;
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
                    { "x1|xA=", v => { x1 = v; crop = true; } },
                    { "x2|xB=", v => { x2 = v; crop = true; } },
                    { "y1|yA=", v => { y1 = v; crop = true; } },
                    { "y2|yB=", v => { y2 = v; crop = true; } },
				};
				var args = options.Parse (argz);

                Player player = sender as Player;

                if (crop)
                {
                    if (x1.Equals("") || x2.Equals("") || y1.Equals("") || y2.Equals(""))
                    {
                        //we need both coordinates
                        throw new CommandError("If cropping, please specify x1, y1, x2, and y2.");
                    }
                    else
                    {
                        //we need x1,y1 to be the top left corner
                        //and x2,y2 to the bottom right corner
                        bool cornererror = false;
                        int x1num;
                        int x2num;
                        int y1num;
                        int y2num;
                        try
                        {
                            x1num = Convert.ToInt32(x1);
                            x2num = Convert.ToInt32(x2);
                            y1num = Convert.ToInt32(y1);
                            y2num = Convert.ToInt32(y2);
                            //enforce bitmap boundaries
                            if (x1num < 0)
                                x1num = 0;
                            if (y1num < 0)
                                y1num = 0;
                            if (x2num > Main.maxTilesX)
                                x2num = Main.maxTilesX;
                            if (y2num > Main.maxTilesY)
                                y2num = Main.maxTilesY;

                            if ((x1num >= x2num) || (y1num >= y2num))
                            {
                                cornererror = true;
                            }
                            if (!cornererror)
                            {
                                //update numbers, for use with the mapping threads
                                this.x1 = x1num;
                                this.x2 = x2num;
                                this.y1 = y1num;
                                this.y2 = y2num;
                            }
                        }
                        catch
                        {
                            throw new CommandError("x1, y1, x2, and y2 must be integers.");
                        }
                        if (cornererror)
                        {
                            throw new CommandError("(" + x1num + "," + y1num + ")(" + x2num + "," + y2num + ")  (x1,y1) must be the top left corner.");
                        }
                    }
                }

                if (autosaveedit)
                {
                    if (autosaveenabled)
                    {
                        properties.SetValue("autosave-enabled", "False");
                        sender.SendMessage("autosave disabled.");
                    }
                    else
                    {
                        properties.SetValue("autosave-enabled", "True");
                        sender.SendMessage("autosave enabled.");
                    }
                    if (highlight)
                    {
                        if (highlightsearch(sender as Player, nameOrID))
                        {
                            properties.SetValue("autosave-highlight", "True");
                            properties.SetValue("autosave-highlightID", nameOrID);
                            sender.SendMessage("autosave highlight settings updated.");
                        }
                    }

                    if (timestamp)
                    {
                        if (autosavetimestamp)
                        {
                            properties.SetValue("autosave-timestamp", "False");
                            sender.SendMessage("autosave now using regular name.");
                        }
                        else
                        {
                            properties.SetValue("autosave-timestamp", "True");
                            sender.SendMessage("autosave now using timestamp.");
                        }
                    }

                    if (filename != "world-now.png")
                    {
                        properties.SetValue("autosave-filename", filename);
                    }

                    properties.Save();
                    return;
                }
				
				if (reload || autosave) {
                    if (reload)
                    {
                        sender.SendMessage("map: Reloaded settings database, entries: " + properties.Count);
                    }
                    if (autosave)
                    {
                        ProgramLog.BareLog(ProgramLog.Plugin, "<map> Reloaded settings database, entries: " + properties.Count);
                    }

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
                    highlight = autosavehighlight;
                }

                if (timestamp)
                {
                    DateTime value = DateTime.Now;
                    string time = value.ToString("yyyy-MM-dd_HH-mm-ss");
                    filename = string.Concat("terraria-", time, ".png");
                }
				if(savefromcommand){
					properties.SetValue ("color-scheme", cs);
					properties.SetValue ("mapoutput-path", p);
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
						sender.SendMessage ("map: "+p+" does not exist.");
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

        //the following is taken and modified from Commands.cs from tdsm.core source. Thanks guys!!! ;)
        public bool highlightsearch(Player player, string nameOrID)
        {
            ItemInfo[] itemlist = DefinitionManager.FindItem(nameOrID);

            if (itemlist != null && itemlist.Length > 0)
            {
                if (itemlist.Length > 1)
                {
                    player.SendMessage("There were " + itemlist.Length + " Items found regarding the specified name");
                    return false;
                }

                ItemInfo item = itemlist[0];
                highlightID = item.NetId; //todo: might be item.Id......
            }
            else
            {
                player.SendMessage("There were no Items found regarding the specified Item Id/Name");
                return false;
            }

            return true;
        }

	}
}

