
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Drawing;
using Terraria;
using TShockAPI;
using Hooks;
using System.ComponentModel;
using TShock_Map;
using System.IO;
using System.Threading;


namespace Map
{
    [APIVersion(1, 12)]
    public partial class MapPlugin : TerrariaPlugin
    {
		PropertiesFile properties;
		bool isEnabled = false;

		string mapoutputpath
		{
			get { return properties.getValue ("mapoutput-path", Environment.CurrentDirectory); }
		}

        string autosavepath
        {
            get { return properties.getValue("autosave-path", Environment.CurrentDirectory); }
        }
		
		string colorscheme
		{
			get { return properties.getValue ("color-scheme", "Terrafirma"); }		
		}

        string autosavename
        {
            get { return properties.getValue("autosave-filename", "autosave.png"); }
        }

        bool autosaveenabled
        {
            get { return properties.getValue("autosave-enabled", false); }
        }

        int autosaveinterval
        {
            get { return properties.getValue("autosave-interval", 30);  } // in minutes
        }

        bool autosavetimestamp
        {
            get { return properties.getValue("autosave-timestamp", false); }
        }

        bool autosavehighlight
        {
            get { return properties.getValue("autosave-highlight", false); }
        }

        string autosavehightlightID
        {
            get { return properties.getValue("autosave-highlightID", "chest"); }
        }

        public MapPlugin(Main game)  : base(game)
        {
            // constructor
        }
		
        public override string Name
        {
            get { return "Map"; }
        }
        public override string Author
        {
            get { return "elevatorguy"; }
        }
        public override string Description
        {
            get { return "Terraria World Mapper"; }
        }
        public override Version Version
        {
            get { return new Version("4.0.5.0"); }
        }

        public override void Initialize()
        {
            GameHooks.Initialize += OnInitialize;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                GameHooks.Initialize -= OnInitialize;
            }
            isEnabled = false;
            base.Dispose(disposing);
        }

        public void OnInitialize()
        {
			Commands.ChatCommands.Add( new Command("map", MapCommand, "map"));

            string pluginFolder = Environment.CurrentDirectory + Path.DirectorySeparatorChar + "map";
            CreateDirectory(pluginFolder);

            properties = new PropertiesFile(pluginFolder + Path.DirectorySeparatorChar + "map.properties");
            properties.Load();
            var dummy = mapoutputpath;
            var dummy2 = colorscheme;
            var dummy3 = autosavepath;
            var dummy4 = autosaveinterval;
            var dummy5 = autosavetimestamp;
            var dummy6 = autosavehighlight;
            var dummy7 = autosavehightlightID;
            var dummy8 = autosaveenabled;
            var dummy9 = autosavename;
            properties.Save();

            if (colorscheme == "MoreTerra" || colorscheme == "Terrafirma")
            {
                isEnabled = true;
            }
            else
            {
                TShockAPI.Log.Error("<map> ERROR: colorscheme must be either 'MoreTerra' or 'Terrafirma'");
                TShockAPI.Log.Error("<map> ERROR: map command will not work until you change it");
                isEnabled = false;
            }

            // these below require that the world be loaded

            //UInt32Defs and ColorDefs for colors, and background fade in Terrafirma Color Scheme
            InitializeMapperDefs();
            InitializeMapperDefs2();

            //this pre blends colors for Terrafirma Color Scheme
            initBList();

            //start autosave thread
            Thread autosavethread;
            autosavethread = new Thread(autoSave);
            autosavethread.Name = "Auto-Mapper";
            autosavethread.Start();
            while (!autosavethread.IsAlive) ;
        }

        public void autoSave()
        {
            bool firstrun = true;
            DateTime lastsave = new DateTime();
            TSPlayer console = new TSPlayer(-1);
            List<string> empty = new List<string>();
            CommandArgs arguments = new CommandArgs("automap", console, empty); // the command method interprets this, along with the data in the properties file
            while(isEnabled)
            {
                if (autosaveenabled)
                {
                    if (!firstrun && (DateTime.UtcNow > lastsave.AddMinutes(autosaveinterval)))
                    {
                        MapCommand(arguments);
                        lastsave = DateTime.UtcNow;
                    }
                    if (firstrun)
                    {
                        firstrun = false;
                        lastsave = DateTime.UtcNow;
                    }
                }
                Thread.Sleep(1000);
            }
        }

    }
}