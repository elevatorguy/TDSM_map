using Terraria_Server.Plugin;
using Terraria_Server.Misc;
using Terraria_Server;
using System.IO;

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
		
		public override void Load ()
		{
			Name = "Map";
			Description = "Gives TDSM a World Mapper.";
			Author = "elevatorguy";
			Version = "0.34.0";
			TDSMBuild = 34;
			
			string pluginFolder = Statics.PluginPath + Path.DirectorySeparatorChar + "map";
			CreateDirectory (pluginFolder);
			
			properties = new PropertiesFile (pluginFolder + Path.DirectorySeparatorChar + "map.properties");
			properties.Load ();
			var dummy = mapoutputpath;
			properties.Save ();
			
			InitializeMapperDefs();
			
			isEnabled = true;
			
			AddCommand ("map")
				.WithDescription ("map options")
				.WithHelpText ("map ")
				.WithHelpText ("map -t")
				.WithHelpText ("map -n outputname.png")
				.WithHelpText ("map -L")
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

