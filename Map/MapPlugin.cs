using System;
using System.Collections.Generic;
using Terraria;
using TShockAPI;
using System.IO;
using System.Threading;
using TerrariaApi.Server;

namespace Map
{
    [ApiVersion(2, 1)]
    public partial class MapPlugin : TerrariaPlugin
    {
        PropertiesFile properties;
        Timer autosavetimer;
        bool isEnabled = false;

        public static MapPlugin instance;

        public static bool initialized = false;

        string mapoutputpath
        {
            get { return properties.getValue("mapoutput-path", Environment.CurrentDirectory); }
        }

        string autosavepath
        {
            get { return properties.getValue("autosave-path", Environment.CurrentDirectory); }
        }

        string colorscheme
        {
            get { return properties.getValue("color-scheme", "Terrafirma"); }
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
            get { return properties.getValue("autosave-interval", 30); } // in minutes
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

        public MapPlugin(Main game) : base(game)
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
            get { return new Version(4, 3, 24, 0516); } //Version number reflects tshock version, and date map plugin was updated.
        }

        private void PostInitialize(EventArgs args)
        {
            // these below require that the world be loaded

            //UInt32Defs and ColorDefs for colors, and background fade in Terrafirma Color Scheme
            InitializeMapperDefs();
            InitializeMapperDefs2();

            //this pre blends colors for Terrafirma Color Scheme
            initBList();

            //start autosave thread
            if (autosaveenabled)
                autosavetimer = new Timer(s => { autoSave(); }, null, autosaveinterval * 60000, Timeout.Infinite);

            instance = this;
            initialized = true;
        }

        public override void Initialize()
        {
            ServerApi.Hooks.GamePostInitialize.Register(this, PostInitialize);
            Commands.ChatCommands.Add(new Command("map.create", MapCommand, "map"));

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
                TShock.Log.Error("<map> ERROR: colorscheme must be either 'MoreTerra' or 'Terrafirma'");
                TShock.Log.Error("<map> ERROR: map command will not work until you change it");
                isEnabled = false;
            }
        }

        protected override void Dispose(bool disposing)
        {
            isEnabled = false;
            base.Dispose(disposing);
        }


        public void autoSave()
        {
            if (!isEnabled)
                return;
            TSPlayer console = new TSPlayer(-1);
            List<string> empty = new List<string>();
            CommandArgs arguments = new CommandArgs("automap", console, empty); // the command method interprets this, along with the data in the properties file
            MapCommand(arguments);
            autosavetimer.Change(autosaveinterval * 60000, Timeout.Infinite);
        }
    }
}

namespace Map.API
{
    public class Mapper
    {
        public static System.Drawing.Bitmap map(int x1, int y1, int x2, int y2)
        {
            if (MapPlugin.initialized && !MapPlugin.instance.isMapping)
            {
                TSPlayer console = new TSPlayer(-1);
                List<string> coords = new List<string>();
                coords.Add("-x1=" + x1);
                coords.Add("-x2=" + x2);
                coords.Add("-y1=" + y1);
                coords.Add("-y2=" + y2);
                CommandArgs arguments = new CommandArgs("api-call", console, coords); // the command method interprets this, along with the data in the properties file

                MapPlugin.instance.MapCommand(arguments);
                while (MapPlugin.instance.isMapping) ;
            }
            return MapPlugin.bmp;
        }
    }
}
