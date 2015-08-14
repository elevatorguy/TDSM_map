using TDSM.API.Plugin;
using TDSM.API.Misc;
using TDSM.API;
using System.IO;
using TDSM.API.Logging;
using System;
using TDSM.API.Command;
using System.Threading;
using System.Collections.Generic;

namespace Map
{
	public partial class MapPlugin : BasePlugin
	{
		PropertiesFile properties;
		volatile bool isEnabled = false;
        public static MapPlugin instance;
        public static bool initialized = false;

		string mapoutputpath
		{
			get { return properties.GetValue<String>("mapoutput-path", Globals.SavePath); }
		}
		
		string colorscheme
		{
			get { return properties.GetValue<String>("color-scheme", "Terrafirma"); }		
		}

        string autosavepath
        {
            get { return properties.GetValue<String>("autosave-path", Environment.CurrentDirectory); }
        }

        string autosavename
        {
            get { return properties.GetValue<String>("autosave-filename", "autosave.png"); }
        }

        bool autosaveenabled
        {
            get { return properties.GetValue<Boolean>("autosave-enabled", false); }
        }

        int autosaveinterval
        {
            get { return properties.GetValue<Int32>("autosave-interval", 30); } // in minutes
        }

        bool autosavetimestamp
        {
            get { return properties.GetValue<Boolean>("autosave-timestamp", false); }
        }

        bool autosavehighlight
        {
            get { return properties.GetValue<Boolean>("autosave-highlight", false); }
        }

        string autosavehightlightID
        {
            get { return properties.GetValue<String>("autosave-highlightID", "chest"); }
        }

		public MapPlugin ()
		{
			Name = "Map";
			Description = "Gives TDSM a World Mapper.";
			Author = "elevatorguy";
			Version = "0.39.1";
			TDSMBuild = 5;
		}
		
		protected override void Initialized (object state)
		{
			string pluginFolder = Globals.DataPath + Path.DirectorySeparatorChar + "map";
			CreateDirectory (pluginFolder);
			
			properties = new PropertiesFile (pluginFolder + Path.DirectorySeparatorChar + "map.properties");
			properties.Load ();
			var dummy = mapoutputpath;
			var dummy2 = colorscheme;
            var dummy3 = autosavepath;
            var dummy4 = autosaveinterval;
            var dummy5 = autosavetimestamp;
            var dummy6 = autosavehighlight;
            var dummy7 = autosavehightlightID;
            var dummy8 = autosaveenabled;
            var dummy9 = autosavename;
			properties.Save ();
			
			if(colorscheme=="MoreTerra" || colorscheme=="Terrafirma"){
				isEnabled = true;
			}
			else{
				ProgramLog.Error.Log ("<map> ERROR: colorscheme must be either 'MoreTerra' or 'Terrafirma'");
				ProgramLog.Error.Log ("<map> ERROR: map command will not work until you change it");
				isEnabled = false;
			}			
			
			AddCommand ("map")
				.WithDescription ("map options")
                .WithAccessLevel(AccessLevel.OP)
				.WithHelpText ("map help")
				.WithHelpText ("map -t")
				.WithHelpText ("map -n outputname.png")
				.WithHelpText ("map -L")
				.WithHelpText ("map [-s] -p /path/to/output")
				.WithHelpText ("map [-s] -p \"C:\\path\\to\\output\"")	
				.WithHelpText ("map [-s] -c MoreTerra")
				.WithHelpText ("map [-s] -c Terrafirma")
                .WithHelpText ("map -h \"name or ID of item to highlight\"")
				.Calls (this.MapCommand);
		}
		
		protected override void Enabled()
		{
			isEnabled = true;
			ProgramLog.Plugin.Log (base.Name + " " + base.Version + " enabled.");
		}

		protected override void Disabled ()
		{
			isEnabled = false;
			ProgramLog.Plugin.Log (base.Name + " " + base.Version + " disabled.");
		}
		
		protected override void Disposed (object state)
		{
            isEnabled = false;
            ProgramLog.Plugin.Log(base.Name + " " + base.Version + " disposed.");
		}

        protected override void WorldLoaded()
        {
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

            instance = this;
            initialized = true;
        }

        public void autoSave()
        {
            bool firstrun = true;
            DateTime lastsave = new DateTime();
            ISender console = new ConsoleSender();
            List<string> empty = new List<string>();
            ArgumentList arguments = new ArgumentList();
            while (isEnabled)
            {
                if (autosaveenabled)
                {
                    if (!firstrun && (DateTime.UtcNow > lastsave.AddMinutes(autosaveinterval)))
                    {
                        if (!arguments.Contains("automap"))
                        {
                            arguments.Add("automap");
                        }
                        MapCommand(console, arguments);
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

namespace Map.API
{
    public class Mapper
    {
        public static System.Drawing.Bitmap map(int x1, int y1, int x2, int y2)
        {
            if(MapPlugin.initialized && !MapPlugin.instance.isMapping)
            {
                ISender console = new ConsoleSender();
                ArgumentList coords = new ArgumentList();
                coords.Add("api-call");
                coords.Add("-x1=" + x1);
                coords.Add("-x2=" + x2);
                coords.Add("-y1=" + y1);
                coords.Add("-y2=" + y2);

                MapPlugin.instance.MapCommand(console, coords);
                while (MapPlugin.instance.isMapping) ;
            }
            return MapPlugin.bmp;
        }
    }
}