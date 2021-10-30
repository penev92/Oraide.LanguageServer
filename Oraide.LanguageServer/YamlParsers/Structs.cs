using System.Collections.Generic;
using Oraide.LanguageServer.CodeParsers;

namespace Oraide.LanguageServer.YamlParsers
{
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

	public struct ActorDefinition
	{
		public string Name { get; set; }

		public MemberLocation Location { get; }

		public List<TraitDefinition> Traits { get; } // This is redundant for the navigatable definitions list.

		public ActorDefinition(string name, MemberLocation location, List<TraitDefinition> traits)
		{
			Name = name;
			Location = location;
			Traits = traits;
		}
	}

	public readonly struct TraitDefinition
	{
		public string Name { get; }

		public MemberLocation Location { get; }

		public TraitDefinition(string name, MemberLocation location)
		{
			Name = name;
			Location = location;
		}
	}
}
