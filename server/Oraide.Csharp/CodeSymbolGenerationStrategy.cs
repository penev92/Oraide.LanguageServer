using System;
using System.Collections.Generic;
using Oraide.Core.Entities.Csharp;
using Oraide.Csharp.CodeParsers;

namespace Oraide.Csharp
{
	abstract class CodeSymbolGenerationStrategy
	{
		public abstract IReadOnlyDictionary<string, TraitInfo> GetTraitInfos(string openRaFolder);
	}

	class CodeParsingSymbolGenerationStrategy : CodeSymbolGenerationStrategy
	{
		public override IReadOnlyDictionary<string, TraitInfo> GetTraitInfos(string openRaFolder)
		{
			return RoslynCodeParser.Parse(openRaFolder);
		}
	}

	class ReflectionSymbolGenerationStrategy : CodeSymbolGenerationStrategy
	{
		public override IReadOnlyDictionary<string, TraitInfo> GetTraitInfos(string openRaFolder)
		{
			throw new NotImplementedException();
		}
	}

	class FromStaticFileSymbolGenerationStrategy : CodeSymbolGenerationStrategy
	{
		public override IReadOnlyDictionary<string, TraitInfo> GetTraitInfos(string openRaFolder)
		{
			throw new NotImplementedException();
		}
	}
}
