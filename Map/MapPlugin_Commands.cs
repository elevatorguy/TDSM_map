using System;
using TShockAPI;
using NDesk.Options;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Terraria;

namespace Map
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

		private TShockAPI.Utils utils = TShockAPI.Utils.Instance;
		
		public void MapCommand(CommandArgs argzz)
		{
            bool autosave = false;
            if(argzz.Message == "automap")
            {
                autosave = true;
            }

            TSPlayer player = argzz.Player;
			if(player == null)
				return;
            
			List<string> argz = argzz.Parameters;
			try {
                if (isMapping)
                {
					player.SendErrorMessage("Still currently mapping.");
                    return;
                }
				p = mapoutputpath;
				filename = "world-now.png";
                bool timestamp = false;
				bool reload = false;
                bool highlight = false;
				highlightID = 0;
                hlchests = false;
                string nameOrID = "";
				bool savefromcommand = false;
				string cs = colorscheme;
                bool autosaveedit = false;
                string x1 = "";
                string x2 = "";
                string y1 = "";
                string y2 = "";
                crop = false;
                var options = new OptionSet()
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

                if(crop)
                {
                    if (x1.Equals("") || x2.Equals("") || y1.Equals("") || y2.Equals(""))
                    {
                        //we need both coordinates
                        player.SendErrorMessage("If cropping, please specify x1, y1, x2, and y2.");
                        return;
                    }
                    else
                    {
                        //we need x1,y1 to be the top left corner
                        //and x2,y2 to the bottom right corner

                        try
                        {
                            int x1num = Convert.ToInt32(x1);
                            int x2num = Convert.ToInt32(x2);
                            int y1num = Convert.ToInt32(y1);
                            int y2num = Convert.ToInt32(y2);

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
                                player.SendErrorMessage("("+x1num+","+y1num+")("+x2num+","+y2num+")  (x1,y1) must be the top left corner.");
                                return;
                            }

                            //update numbers, for use with the mapping threads
                            this.x1 = x1num;
                            this.x2 = x2num;
                            this.y1 = y1num;
                            this.y2 = y2num;
                        }
                        catch
                        {
                            player.SendErrorMessage("x1, y1, x2, and y2 must be integers.");
                            return;
                        }
                    }
                }

                if (autosaveedit)
                {
                    if (autosaveenabled)
                    {
                        properties.setValue("autosave-enabled", "False");
                        player.SendInfoMessage("autosave disabled.");
                    }
                    else
                    {
                        properties.setValue("autosave-enabled", "True");
                        player.SendInfoMessage("autosave enabled.");
                    }
                    if (highlight)
                    {
                        if (highlightsearch(player, nameOrID))
                        {
                            properties.setValue("autosave-highlight", "True");
                            properties.setValue("autosave-highlightID", nameOrID);
                            player.SendInfoMessage("autosave highlight settings updated.");
                        }
                    }

                    if (timestamp)
                    {
                        if (autosavetimestamp)
                        {
                            properties.setValue("autosave-timestamp", "False");
                            player.SendInfoMessage("autosave now using regular name.");
                        }
                        else
                        {
                            properties.setValue("autosave-timestamp", "True");
                            player.SendInfoMessage("autosave now using timestamp.");
                        }
                    }

                    if (filename != "world-now.png")
                    {
                        properties.setValue("autosave-filename", filename);
                    }

                    properties.Save();
                    return;
                }

                if (reload || autosave)
                {
                    if(reload)
                        player.SendInfoMessage("map: Reloaded settings database, entries: " + properties.Count);
                    if(autosave)
                        TShock.Log.Info("<map> Reloaded settings database, entries: " + properties.Count);

                    properties.Load();
                    var msg = string.Concat(
                    "Settings: mapoutputpath=", p, ", ",
                    "colorscheme=", cs);
                    if (!(Directory.Exists(p)))
                    {
                        msg = string.Concat(msg, "  (DOESNT EXIST)");
                        TShock.Log.Error("<map> ERROR: Loaded Directory does not exist.");
                    }
                    if (!autosave)
                    {
                        TShock.Log.Info("<map> " + msg);
                    }
                    //sender.sendMessage ("map: " + msg);

                    if (!(cs == "MoreTerra" || cs == "Terrafirma"))
                    {
                        TShock.Log.Error("<map> ERROR: please change colorscheme");
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

				if (timestamp) {
					DateTime value = DateTime.Now;
					string time = value.ToString ("yyyy-MM-dd_HH-mm-ss");
					filename = string.Concat ("terraria-", time, ".png");
				}		
			
				if(savefromcommand){
					properties.setValue ("color-scheme", cs);
					properties.setValue ("mapoutput-path", p);
					properties.Save();
				}
                // chests are not an item so i draw them from the chest array
                if (highlight && nameOrID.ToLower() != "chest")
                {
                    highlightsearch(player, nameOrID);
                }
                if (highlight && nameOrID.ToLower() == "chest")
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
                            TShock.Log.Error("Save ERROR: check colorscheme");
						}
						if( !(Directory.Exists(p)) ){
						player.SendErrorMessage ("map: "+p+" does not exist.");
                        TShock.Log.Error("<map> ERROR: Loaded Directory does not exist.");
				}
					}
				} else {
                    return;
				}
			} catch (OptionException) {
                return;
			}
		
		}

        public bool highlightsearch(TSPlayer player, string nameOrID)
        {
            List<Terraria.Item> itemlist = utils.GetItemByIdOrName(nameOrID);

            if (itemlist != null && itemlist.Count > 0)
            {
                if (itemlist.Count > 1)
                {
                    player.SendInfoMessage("There were " + itemlist.Count + " Items found regarding the specified name");
                    return false;
                }

                foreach (Terraria.Item item in itemlist)
                    highlightID = item.type;
            }
            else
            {
                player.SendErrorMessage("There were no Items found regarding the specified Item Id/Name");
                return false;
            }

            return true;
        }
	}
}

