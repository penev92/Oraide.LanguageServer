using System;

namespace OpenRA.MiniYamlParser
{
	[Serializable]
	public class YamlException : Exception
	{
		public YamlException(string s)
			: base(s) { }
	}
}
