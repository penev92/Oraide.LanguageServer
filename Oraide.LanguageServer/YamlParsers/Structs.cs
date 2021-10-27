using System;
using System.Collections.Generic;
using Oraide.LanguageServer.CodeParsers;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Oraide.LanguageServer.YamlParsers
{
	public class LoadScreenConverter : IYamlTypeConverter
	{
		public bool Accepts(Type type)
		{
			return true;
		}

		public object ReadYaml(IParser parser, Type type)
		{
			if (parser.TryConsume<Scalar>(out var scalar))
			{
				return new List<string> { scalar.Value };
			}

			if (parser.TryConsume<SequenceStart>(out var _))
			{
				var items = new List<string>();
				while (parser.TryConsume<Scalar>(out var scalarItem))
				{
					items.Add(scalarItem.Value);
				}

				parser.Consume<SequenceEnd>();
				return items;
			}

			return new List<string> { "kor" };
		}

		public void WriteYaml(IEmitter emitter, object value, Type type)
		{
			throw new NotImplementedException();
		}
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

	public struct ActorDefinition
	{
		public string Name { get; set; }

		public MemberLocation Location { get; }

		public List<TraitDefinition> Traits { get; }

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
