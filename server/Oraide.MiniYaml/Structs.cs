using System.Collections.Generic;

namespace Oraide.MiniYaml
{
	public class Mod
	{
		// public string Name { get; set; }
		//
		// public string FullName { get; set; }

		public string RootDirectory { get; set; }
	}

	public class MyModData
	{
		public MyLoadScreen LoadScreen { get; set; }

		public List<string> Packages { get; set; }
	}

	public class MyLoadScreen
	{
		public string Image { get; set; }

		public string Image2x { get; set; }

		public string Image3x { get; set; }

		public string Text { get; set; }

		public string Type { get; set; }
	}
}
