using System.Collections.Generic;
using System.IO;
using System.Linq;
using Oraide.Core.Entities.Csharp;
using Oraide.Csharp.Abstraction.CodeParsers;

namespace Oraide.Csharp.CodeParsers
{
	/// <summary>
	/// This is very loosely named. Its purpose is to cater to whatever old engine version the Cameo mod uses.
	/// The distinguishing changes here came from https://github.com/OpenRA/OpenRA/pull/18123, were merged mid-2020 and were likely released with release 20210331.
	/// </summary>
	public class Pre20210321RoslynCodeParser : BaseRoslynCodeParser
	{
		public override bool CanParse(in string oraFolderPath)
		{
			try
			{
				// The distinguishing change was removing ITraitInfo, so using a random (the first possible) file to check if it is there.
				var file = Directory.EnumerateFiles(oraFolderPath, "DebugPauseState.cs", SearchOption.AllDirectories).Single();
				return File.ReadAllText(file).Contains("ITraitInfo");
			}
			catch
			{
				return false;
			}
		}

		protected override IEnumerable<ClassInfo> FinalizeTraitInfos(IList<ClassInfo> traitInfos)
		{
			foreach (var ti in traitInfos)
			{
				// Skip the base TraitInfo class(es).
				if (ti.InfoName == "TraitInfo")
					continue;

				var baseTypes = GetTraitBaseTypes(ti.InfoName, traitInfos).ToArray();

				// HACK: Checking for ITraitInfo and IRulesetLoaded here as a hack for Cameo because it's using a X-year-old engine!
				if (baseTypes.Any(x => x.TypeName == "TraitInfo" || x.TypeName == "ITraitInfo" || x.TypeName == "IRulesetLoaded"))
				{
					var fieldInfos = new List<ClassFieldInfo>();
					foreach (var (className, classFieldNames) in baseTypes)
					{
						foreach (var typeFieldName in classFieldNames)
						{
							var fi = ti.PropertyInfos.FirstOrDefault(z => z.Name == typeFieldName);
							if (fi.Name != null)
								fieldInfos.Add(new ClassFieldInfo(fi.Name, fi.InternalType, fi.UserFriendlyType, fi.DefaultValue,
									className, fi.Location, fi.Description, fi.OtherAttributes));
							else
							{
								var otherFieldInfo = traitInfos.First(x => x.InfoName == className).PropertyInfos.First(x => x.Name == typeFieldName);
								fieldInfos.Add(new ClassFieldInfo(otherFieldInfo.Name, otherFieldInfo.InternalType, otherFieldInfo.UserFriendlyType,
									otherFieldInfo.DefaultValue, className, otherFieldInfo.Location, otherFieldInfo.Description, otherFieldInfo.OtherAttributes));
							}
						}
					}

					var traitInfo = new ClassInfo(
						ti.Name,
						ti.InfoName,
						ti.Description,
						ti.Location,
						baseTypes.Where(x => x.TypeName != ti.InfoName).Select(x => x.TypeName).ToArray(),
						fieldInfos.ToArray(),
						ti.IsAbstract);

					yield return traitInfo;
				}
			}
		}

		protected override IEnumerable<(string TypeName, IEnumerable<string> ClassFields)> GetTraitBaseTypes(string traitInfoName, IList<ClassInfo> knownTraitInfos)
		{
			// TODO: It would be useful to know what the `Requires` requires.
			if (traitInfoName == "TraitInfo" || traitInfoName == "Requires" || (traitInfoName.StartsWith("I") && !traitInfoName.EndsWith("Info")))
				return new[] { (traitInfoName, Enumerable.Empty<string>()) };

			// HACK: These are only here as a hack for Cameo because it's using a X-year-old engine!
			if (traitInfoName == "ITraitInfo" || traitInfoName == "IRulesetLoaded")
				return new[] { (traitInfoName, Enumerable.Empty<string>()) };

			var traitInfo = knownTraitInfos.FirstOrDefault(x => x.InfoName == traitInfoName);
			if (traitInfo.InfoName == null)
				return Enumerable.Empty<(string, IEnumerable<string>)>();

			var result = new List<(string TypeName, IEnumerable<string> ClassFieldInfos)>
			{
				(traitInfoName, traitInfo.PropertyInfos.Select(x => x.Name))
			};

			foreach (var baseType in traitInfo.BaseTypes)
				result.AddRange(GetTraitBaseTypes(baseType, knownTraitInfos));

			return result;
		}
	}
}
