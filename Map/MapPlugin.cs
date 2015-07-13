using tdsm.api.Plugin;
using tdsm.api.Misc;
using tdsm.api;
using System.IO;
using tdsm.api.Logging;
using System;
using tdsm.api.Command;
using System.Threading;
using System.Collections.Generic;

namespace MapPlugin
{
	public partial class MapPlugin : BasePlugin
	{
		PropertiesFile properties;
		bool isEnabled = false;
		
		string mapoutputpath
		{
			get { return properties.GetValue<string>("mapoutput-path", Globals.SavePath); }
		}
		
		string colorscheme
		{
			get { return properties.GetValue<string>("color-scheme", "Terrafirma"); }		
		}

        string autosavepath
        {
            get { return properties.GetValue<string>("autosave-path", Environment.CurrentDirectory); }
        }

        string autosavename
        {
            get { return properties.GetValue<string>("autosave-filename", "autosave.png"); }
        }

        bool autosaveenabled
        {
            get { return properties.GetValue<bool>("autosave-enabled", false); }
        }

        int autosaveinterval
        {
            get { return properties.GetValue<int>("autosave-interval", 30); } // in minutes
        }

        bool autosavetimestamp
        {
            get { return properties.GetValue<bool>("autosave-timestamp", false); }
        }

        bool autosavehighlight
        {
            get { return properties.GetValue<bool>("autosave-highlight", false); }
        }

        string autosavehightlightID
        {
            get { return properties.GetValue<string>("autosave-highlightID", "chest"); }
        }

		public MapPlugin ()
		{
			Name = "Map";
			Description = "Gives TDSM a World Mapper.";
			Author = "elevatorguy";
			Version = "Rebind.2.1";
			TDSMBuild = 2;
		}
		
		protected override void Initialized (object state)
		{
			string pluginFolder = Globals.PluginPath + Path.DirectorySeparatorChar + "map";
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

