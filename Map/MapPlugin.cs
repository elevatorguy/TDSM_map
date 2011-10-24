using Terraria_Server.Plugins;
using Terraria_Server.Misc;
using Terraria_Server;
using System.IO;
using Terraria_Server.Logging;
using System;
using Terraria_Server.Commands;

namespace MapPlugin
{
	public partial class MapPlugin : BasePlugin
	{
		PropertiesFile properties;
		bool isEnabled = false;
		
		string mapoutputpath
		{
			get { return properties.getValue ("mapoutput-path", Statics.SavePath); }
		}
		
		string colorscheme
		{
			get { return properties.getValue ("color-scheme", "Terrafirma"); }		
		}
		
		public MapPlugin ()
		{
			Name = "Map";
			Description = "Gives TDSM a World Mapper.";
			Author = "elevatorguy";
			Version = "0.36.0";
			TDSMBuild = 36;
		}
		
		protected override void Initialized (object state)
		{
			string pluginFolder = Statics.PluginPath + Path.DirectorySeparatorChar + "map";
			CreateDirectory (pluginFolder);
			
			properties = new PropertiesFile (pluginFolder + Path.DirectorySeparatorChar + "map.properties");
			properties.Load ();
			var dummy = mapoutputpath;
			var dummy2 = colorscheme;
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
				.WithHelpText ("map help")
				.WithHelpText ("map -t")
				.WithHelpText ("map -n outputname.png")
				.WithHelpText ("map -L")
				.WithHelpText ("map [-s] -p /path/to/output")
				.WithHelpText ("map [-s] -p \"C:\\path\\to\\output\"")	
				.WithHelpText ("map [-s] -c MoreTerra")
				.WithHelpText ("map [-s] -c Terrafirma")
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

        [Hook(HookOrder.TERMINAL)]
        void OnWorldLoad(ref HookContext ctx, ref HookArgs.WorldLoaded args)
        {
            //UInt32Defs and ColorDefs for colors, and background fade in Terrafirma Color Scheme
            InitializeMapperDefs();
            InitializeMapperDefs2();

            //this pre blends colors for Terrafirma Color Scheme
            initBList();
        }

	}
}

