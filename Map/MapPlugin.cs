using Terraria_Server.Plugin;
using Terraria_Server.Misc;
using Terraria_Server;
using System.IO;
using Terraria_Server.Logging;

namespace MapPlugin
{
	public partial class MapPlugin : Plugin
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
		
		public override void Load ()
		{
			Name = "Map";
			Description = "Gives TDSM a World Mapper.";
			Author = "elevatorguy";
			Version = "0.35.0";
			TDSMBuild = 35;
			
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
		
		public override void Enable ()
		{
			isEnabled = true;
			Program.tConsole.WriteLine (base.Name + " enabled");
		}
		
		public override void Disable ()
		{
			isEnabled = false;
			Program.tConsole.WriteLine (base.Name + " disabled.");
		}			
	}
}

