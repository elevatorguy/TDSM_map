using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace Map
{
    [ApiVersion(2, 1)]
    public partial class MapPlugin : TerrariaPlugin
    {
        PropertiesFile properties;
        Timer autosavetimer;
        bool isEnabled;

        public static MapPlugin instance;

        public static bool initialized;

        string OutputPath
        {
            get { return properties.getValue("mapoutput-path", Environment.CurrentDirectory); }
        }

        string AutosavePath
        {
            get { return properties.getValue("autosave-path", Environment.CurrentDirectory); }
        }

        string Colorscheme
        {
            get { return properties.getValue("color-scheme", "Terrafirma"); }
        }

        string AutosaveName
        {
            get { return properties.getValue("autosave-filename", "autosave.png"); }
        }

        bool AutosaveEnabled
        {
            get { return properties.getValue("autosave-enabled", false); }
        }

        int AutosaveInterval
        {
            get { return properties.getValue("autosave-interval", 30); } // in minutes
        }

        bool AutosaveTimestamp
        {
            get { return properties.getValue("autosave-timestamp", false); }
        }

        bool AutosaveHighlight
        {
            get { return properties.getValue("autosave-highlight", false); }
        }

        string AutosaveHighlightID
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
            if (AutosaveEnabled)
                autosavetimer = new Timer(s => { Autosave(); }, null, AutosaveInterval * 60000, Timeout.Infinite);

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
            var dummy = OutputPath;
            var dummy2 = Colorscheme;
            var dummy3 = AutosavePath;
            var dummy4 = AutosaveInterval;
            var dummy5 = AutosaveTimestamp;
            var dummy6 = AutosaveHighlight;
            var dummy7 = AutosaveHighlightID;
            var dummy8 = AutosaveEnabled;
            var dummy9 = AutosaveName;
            properties.Save();

            if (Colorscheme == "MoreTerra" || Colorscheme == "Terrafirma")
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


        public void Autosave()
        {
            if (!isEnabled)
                return;
            TSPlayer console = new TSPlayer(-1);
            List<string> empty = new List<string>();
            CommandArgs arguments = new CommandArgs("automap", console, empty); // the command method interprets this, along with the data in the properties file
            MapCommand(arguments);
            autosavetimer.Change(AutosaveInterval * 60000, Timeout.Infinite);
        }
    }
}

namespace Map.API
{
    public static class Mapper
    {
        public static System.Drawing.Bitmap Map(int x1, int y1, int x2, int y2)
        {
            if (MapPlugin.initialized && !MapPlugin.instance.isMapping)
            {
                TSPlayer console = new TSPlayer(-1);
                List<string> coords = new List<string>
                {
                    "-x1=" + x1,
                    "-x2=" + x2,
                    "-y1=" + y1,
                    "-y2=" + y2
                };
                CommandArgs arguments = new CommandArgs("api-call", console, coords); // the command method interprets this, along with the data in the properties file

                MapPlugin.instance.MapCommand(arguments);
                while (MapPlugin.instance.isMapping)
                {
                }
            }
            return MapPlugin.bmp;
        }
    }
}
