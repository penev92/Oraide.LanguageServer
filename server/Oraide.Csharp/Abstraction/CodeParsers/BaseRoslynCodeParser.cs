using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Oraide.Core.Entities;
using Oraide.Core.Entities.Csharp;

namespace Oraide.Csharp.Abstraction.CodeParsers
{
	public abstract class BaseRoslynCodeParser : ICodeParser
	{
		public abstract bool CanParse(in string oraFolderPath);

		public CodeInformation Parse(in string oraFolderPath)
		{
			var traitInfos = new List<ClassInfo>();
			var weaponInfoFields = Array.Empty<ClassFieldInfo>();
			var warheadInfos = new List<ClassInfo>();
			var projectileInfos = new List<ClassInfo>();
			var spriteSequenceInfos = new List<ClassInfo>();
			var enumInfos = new List<EnumInfo>();
			var packageLoaders = new List<ClassInfo>();
			var soundLoaders = new List<ClassInfo>();
			var spriteLoaders = new List<ClassInfo>();
			var videoLoaders = new List<ClassInfo>();
			var widgets = new List<ClassInfo>();

			var filePaths = Directory.EnumerateFiles(oraFolderPath, "*.cs", SearchOption.AllDirectories)
				.Where(x => !x.Contains("OpenRA.Test"));

			// Split .cs files into traits/weapons/etc. and parse them.
			foreach (var filePath in filePaths)
			{
				var fileText = File.ReadAllText(filePath);
				var syntaxTree = CSharpSyntaxTree.ParseText(fileText);
				var root = syntaxTree.GetCompilationUnitRoot();

				foreach (var element in root.Members)
				{
					if (element is NamespaceDeclarationSyntax namespaceDeclaration)
					{
						foreach (var namespaceMember in namespaceDeclaration.Members)
						{
							if (namespaceMember is ClassDeclarationSyntax classDeclaration)
							{
								var (type, classInfo) = ParseClass(filePath, fileText, classDeclaration);
								switch (type)
								{
									case ClassType.None:
										// Nothing to do here - invalid or unsupported class type.
										break;
									case ClassType.Trait:
										traitInfos.Add(classInfo);
										break;
									case ClassType.WeaponInfo:
										weaponInfoFields = classInfo.PropertyInfos;
										break;
									case ClassType.Warhead:
										warheadInfos.Add(classInfo);
										break;
									case ClassType.Projectile:
										projectileInfos.Add(classInfo);
										break;
									case ClassType.SpriteSequence:
										spriteSequenceInfos.Add(classInfo);
										break;
									case ClassType.PackageLoader:
										packageLoaders.Add(classInfo);
										break;
									case ClassType.SoundLoader:
										soundLoaders.Add(classInfo);
										break;
									case ClassType.SpriteLoader:
										spriteLoaders.Add(classInfo);
										break;
									case ClassType.VideoLoader:
										videoLoaders.Add(classInfo);
										break;
									case ClassType.Widget:
										widgets.Add(classInfo);
										break;
									default:
										throw new ArgumentOutOfRangeException();
								}
							}
							else if (namespaceMember is EnumDeclarationSyntax enumDeclaration)
								enumInfos.Add(ParseEnum(filePath, fileText, namespaceDeclaration, enumDeclaration));
						}
					}
				}
			}

			var finalTraitInfos = FinalizeTraitInfos(traitInfos).ToArray();
			var finalPaletteInfos = FinalizePaletteInfos(finalTraitInfos);
			var weaponInfo = FinalizeWeaponInfo(weaponInfoFields, projectileInfos, warheadInfos);
			var finalSpriteSequenceInfos = FinalizeSpriteSequenceInfos(spriteSequenceInfos);
			var finalWidgets = FinalizeWidgets(widgets);

			return new CodeInformation(finalTraitInfos, finalPaletteInfos, weaponInfo, finalSpriteSequenceInfos, enumInfos,
				packageLoaders, soundLoaders, spriteLoaders, videoLoaders, finalWidgets);
		}

		protected virtual (ClassType Type, ClassInfo Info) ParseClass(string filePath, string fileText, ClassDeclarationSyntax classDeclaration)
		{
			var baseTypes = ParseBaseTypeNames(classDeclaration).ToArray();

			// Parsing files the "if it quacks like a duck" way because the alternative would be
			// to create a whole type hierarchy to know what inherits what and that is just overkill.
			// TODO: Be smarter about how we decide what type of file this is.
			if ((filePath.Contains("Trait")
			     && (classDeclaration.Identifier.ValueText.EndsWith("Info") || classDeclaration.Identifier.ValueText.EndsWith("InfoBase")))
			    || baseTypes.Any(x => x == "TraitInfo" || x.StartsWith("TraitInfo<")))
			{
				var classInfo = ParseTraitInfo(filePath, fileText, classDeclaration, baseTypes);
				return classInfo == null ? (ClassType.None, default) : (ClassType.Trait, classInfo.Value);
			}

			if (filePath.Replace("\\", "/").EndsWith("GameRules/WeaponInfo.cs") && classDeclaration.Identifier.ValueText == "WeaponInfo")
			{
				var weaponInfoFields = ParseWeaponInfo(filePath, fileText, classDeclaration).ToArray();
				return (ClassType.WeaponInfo, new ClassInfo("", "", "", default, null, weaponInfoFields, false));
			}

			if (baseTypes.Any(x => x == "IProjectileInfo"))
			{
				var classInfo = ParseProjectileInfo(filePath, fileText, classDeclaration);
				return (ClassType.Projectile, classInfo);
			}

			// Could be done smarter, but several levels of inheritance and not wanting to construct a full tree got us here.
			if (baseTypes.Any(x => x.EndsWith("Warhead")))
			{
				var classInfo = ParseWarheadInfo(filePath, fileText, classDeclaration);
				return (ClassType.Warhead, classInfo);
			}

			if (classDeclaration.Identifier.ValueText.EndsWith("SpriteSequence"))
			{
				var classInfo = ParseSpriteSequenceInfo(filePath, fileText, classDeclaration);
				return (ClassType.SpriteSequence, classInfo);
			}

			// Asset loaders:
			if (baseTypes.Contains("IPackageLoader"))
			{
				var classInfo = ParseAssetLoader(filePath, fileText, classDeclaration);
				return (ClassType.PackageLoader, classInfo);
			}

			if (baseTypes.Contains("ISoundLoader"))
			{
				var classInfo = ParseAssetLoader(filePath, fileText, classDeclaration);
				return (ClassType.SoundLoader, classInfo);
			}

			if (baseTypes.Contains("ISpriteLoader"))
			{
				var classInfo = ParseAssetLoader(filePath, fileText, classDeclaration);
				return (ClassType.SpriteLoader, classInfo);
			}

			if (baseTypes.Contains("IVideoLoader"))
			{
				var classInfo = ParseAssetLoader(filePath, fileText, classDeclaration);
				return (ClassType.VideoLoader, classInfo);
			}

			// Widget-related types:
			if (classDeclaration.Identifier.ValueText == "Widget" || baseTypes.Any(x => (!x.StartsWith("I") || char.IsLower(x[1])) && x.EndsWith("Widget")))
			{
				var classInfo = ParseWidget(filePath, fileText, classDeclaration);
				return (ClassType.Widget, classInfo);
			}

			return default;
		}

		protected virtual EnumInfo ParseEnum(string filePath, string fileText, NamespaceDeclarationSyntax namespaceDeclaration, EnumDeclarationSyntax enumDeclaration)
		{
			var name = enumDeclaration.Identifier.ValueText;
			var namespaceName = namespaceDeclaration.Name.GetText().ToString().Trim();
			var fullName = $"{namespaceName}.{name}";
			var values = enumDeclaration.Members.Select(x => x.Identifier.ValueText).ToArray();
			var isFlagsEnum = enumDeclaration.AttributeLists.Any(x => x.Attributes.Any(y => y.Name.GetText().ToString().Trim() == "Flags"));

			// Some manual string nonsense to determine the class name location inside the file.
			var enumDeclarationStart = enumDeclaration.GetLocation().SourceSpan.Start;
			var enumDeclarationLocation = FindTypeLocationInText(filePath, fileText, name, enumDeclarationStart, "enum");

			return new EnumInfo(name, fullName, "", values, isFlagsEnum, enumDeclarationLocation);
		}

		#region Specific type parsers

		protected virtual ClassInfo? ParseTraitInfo(string filePath, string fileText, ClassDeclarationSyntax classDeclaration, string[] baseTypes)
		{
			var traitProperties = new List<ClassFieldInfo>();

			// Previously "traitInfoName", now "fullName" for consistency with other parsing methods.
			var fullName = classDeclaration.Identifier.ValueText;
			var typeSuffix = "Info";
			var name = GetTypeNameWithoutSuffix(fullName, ref typeSuffix);

			var isAbstract = classDeclaration.Modifiers.Any(x => x.ValueText == "abstract");

			// Skip classes that are not TraitInfos. Make a special case exception for TooltipInfoBase.
			if (!fullName.EndsWith("Info") && !fullName.EndsWith("InfoBase"))
				return null;

			// Get trait's DescAttribute.
			var traitDesc = ParseDescAttribute(classDeclaration);

			// Get TraitInfo property (actually field) list.
			foreach (var member in classDeclaration.Members)
				if (member is FieldDeclarationSyntax fieldMember
				    && fieldMember.Modifiers.Any(x => x.ValueText == "public")
				    && fieldMember.Modifiers.Any(x => x.ValueText == "readonly")
				    && fieldMember.Modifiers.All(x => x.ValueText != "static"))
					traitProperties.AddRange(ParseClassField(filePath, fileText, fieldMember));

			// Some manual string nonsense to determine trait name location inside the file.
			var classStart = classDeclaration.GetLocation().SourceSpan.Start;
			var classLocation = FindTypeLocationInText(filePath, fileText, fullName, classStart);

			return new ClassInfo(name, typeSuffix, traitDesc, classLocation, baseTypes, traitProperties.ToArray(), isAbstract);
		}

		protected virtual IEnumerable<ClassFieldInfo> ParseWeaponInfo(string filePath, string fileText, ClassDeclarationSyntax classDeclaration)
		{
			return ParseClassFields(filePath, fileText, classDeclaration);
		}

		protected virtual ClassInfo ParseProjectileInfo(string filePath, string fileText, ClassDeclarationSyntax classDeclaration)
		{
			var typeSuffix = "Info";
			return ParseSimpleClass(filePath, fileText, classDeclaration, ref typeSuffix);
		}

		protected virtual ClassInfo ParseWarheadInfo(string filePath, string fileText, ClassDeclarationSyntax classDeclaration)
		{
			var typeSuffix = "Warhead";
			return ParseSimpleClass(filePath, fileText, classDeclaration, ref typeSuffix);
		}

		protected virtual ClassInfo ParseSpriteSequenceInfo(string filePath, string fileText, ClassDeclarationSyntax classDeclaration)
		{
			var typeSuffix = string.Empty;
			var classInfo = ParseSimpleClass(filePath, fileText, classDeclaration, ref typeSuffix);

			// Sequences need special member field parsing...
			var fields = new List<ClassFieldInfo>();
			foreach (var member in classDeclaration.Members)
			{
				if (member is FieldDeclarationSyntax fieldMember
				    && fieldMember.Modifiers.Any(x => x.ValueText == "static")
				    && fieldMember.Modifiers.Any(x => x.ValueText == "readonly")
				    && (fieldMember.Declaration.Type as GenericNameSyntax)?.Identifier.ValueText == "SpriteSequenceField")
				{
					foreach (var fieldInfo in ParseClassField(filePath, fileText, fieldMember))
					{
						var type = string.Join(", ", (fieldMember.Declaration.Type as GenericNameSyntax).TypeArgumentList.Arguments);
						var defaultValue = fieldInfo.DefaultValue.Split(',').Last().Trim();
						var newFieldInfo = new ClassFieldInfo(fieldInfo.Name, type, UserFriendlyTypeName(type),
							defaultValue, fieldInfo.ClassName, fieldInfo.Location, fieldInfo.Description, fieldInfo.OtherAttributes);

						fields.Add(newFieldInfo);
					}
				}
			}

			return new ClassInfo(classInfo.Name, classInfo.TypeSuffix, classInfo.Description, classInfo.Location,
				classInfo.BaseTypes, fields.ToArray(), classInfo.IsAbstract);
		}

		protected virtual ClassInfo ParseAssetLoader(string filePath, string fileText, ClassDeclarationSyntax classDeclaration)
		{
			var typeSuffix = "Loader";
			return ParseSimpleClass(filePath, fileText, classDeclaration, ref typeSuffix);
		}

		protected virtual ClassInfo ParseWidget(string filePath, string fileText, ClassDeclarationSyntax classDeclaration)
		{
			// Can't just use ParseSimpleClass() because Widget fields aren't readonly and need different parsing.
			var typeSuffix = "Widget";
			var fullName = classDeclaration.Identifier.ValueText;
			var name = GetTypeNameWithoutSuffix(fullName, ref typeSuffix);
			var description = ParseDescAttribute(classDeclaration);
			var isAbstract = classDeclaration.Modifiers.Any(x => x.ValueText == "abstract");

			// Some manual string nonsense to determine the class name location inside the file.
			var classStart = classDeclaration.GetLocation().SourceSpan.Start;
			var classLocation = FindTypeLocationInText(filePath, fileText, fullName, classStart);
			var baseTypes = ParseBaseTypeNames(classDeclaration).ToArray();
			var fields = ParseWidgetClassFields(filePath, fileText, classDeclaration).ToArray();

			return new ClassInfo(name, typeSuffix, description, classLocation, baseTypes, fields, isAbstract);
		}

		#endregion

		#region Finalizing methods

		/// <summary>
		/// Resolves trait inheritance - load base types and a full list in fields - inherited or not.
		/// Excludes abstract types from the result.
		/// </summary>
		/// <param name="traitInfos">A list of all loaded trait infos.</param>
		/// <returns>A finalized list of trait infos, with loaded fields and resolved base types.</returns>
		protected virtual IEnumerable<ClassInfo> FinalizeTraitInfos(IList<ClassInfo> traitInfos)
		{
			foreach (var ti in traitInfos)
			{
				// Skip the base TraitInfo class(es).
				if (ti.NameWithTypeSuffix == "TraitInfo")
					continue;

				var baseTypes = GetTraitBaseTypes(ti.NameWithTypeSuffix, traitInfos).ToArray();
				if (baseTypes.Any(x => x.TypeName == "TraitInfo"))
				{
					var fieldInfos = new List<ClassFieldInfo>();
					foreach (var (classFullName, classFieldNames) in baseTypes)
					{
						foreach (var typeFieldName in classFieldNames)
						{
							var fi = ti.PropertyInfos.FirstOrDefault(z => z.Name == typeFieldName);
							if (fi.Name != null)
								fieldInfos.Add(new ClassFieldInfo(fi.Name, fi.InternalType, fi.UserFriendlyType, fi.DefaultValue,
									classFullName, fi.Location, fi.Description, fi.OtherAttributes));
							else
							{
								var otherFieldInfo = traitInfos.First(x => x.NameWithTypeSuffix == classFullName).PropertyInfos.First(x => x.Name == typeFieldName);
								fieldInfos.Add(new ClassFieldInfo(otherFieldInfo.Name, otherFieldInfo.InternalType, otherFieldInfo.UserFriendlyType,
									otherFieldInfo.DefaultValue, classFullName, otherFieldInfo.Location, otherFieldInfo.Description, otherFieldInfo.OtherAttributes));
							}
						}
					}

					if (!ti.IsAbstract)
					{
						var traitInfo = new ClassInfo(
							ti.Name,
							ti.TypeSuffix,
							ti.Description,
							ti.Location,
							baseTypes.Where(x => x.TypeName != ti.NameWithTypeSuffix).Select(x => x.TypeName).ToArray(),
							fieldInfos.ToArray(),
							ti.IsAbstract);

						yield return traitInfo;
					}
				}
			}
		}

		protected virtual IEnumerable<ClassInfo> FinalizePaletteInfos(IList<ClassInfo> traitInfos)
		{
			// Palettes are just TraitInfos that have a name field with a PaletteDefinitionAttribute.
			return traitInfos
				.Where(x => x.PropertyInfos
					.Any(y => y.OtherAttributes
						.Any(z => z.Name == "PaletteDefinition")));
		}

		protected virtual WeaponInfo FinalizeWeaponInfo(IList<ClassFieldInfo> fieldInfos, IList<ClassInfo> projectileInfos, IList<ClassInfo> warheadInfos)
		{
			var finalizedProjectileInfos = FinalizeProjectileInfos(projectileInfos);
			var finalizedWarheadInfos = FinalizeWarheadInfos(warheadInfos);
			return new WeaponInfo(fieldInfos.ToArray(), finalizedProjectileInfos.ToArray(), finalizedWarheadInfos.ToArray());
		}

		/// <summary>
		/// Resolves warhead inheritance - load base types and a full list in fields - inherited or not.
		/// Excludes abstract types from the result.
		/// </summary>
		/// <param name="warheadInfos">A list of all loaded warhead infos.</param>
		/// <returns>A finalized list of warhead infos, with loaded fields and resolved base types.</returns>
		protected virtual IEnumerable<ClassInfo> FinalizeWarheadInfos(IList<ClassInfo> warheadInfos)
		{
			foreach (var wi in warheadInfos)
			{
				// Skip the base Warhead class (its Name is empty because we trim down "Warhead" off of names).
				if (wi.Name == string.Empty)
					continue;

				var baseTypes = GetClassBaseTypes(wi.NameWithTypeSuffix, warheadInfos).ToArray();
				var fieldInfos = new List<ClassFieldInfo>();
				foreach (var (className, classFieldNames) in baseTypes)
				{
					// TODO: This seems like it could be simplified a lot like SpriteSequences below.
					foreach (var typeFieldName in classFieldNames)
					{
						var fi = wi.PropertyInfos.FirstOrDefault(z => z.Name == typeFieldName);
						if (fi.Name != null)
							fieldInfos.Add(new ClassFieldInfo(fi.Name, fi.InternalType, fi.UserFriendlyType, fi.DefaultValue,
								className, fi.Location, fi.Description, fi.OtherAttributes));
						else
						{
							var otherFieldInfo = warheadInfos.First(x => x.NameWithTypeSuffix == className).PropertyInfos.First(x => x.Name == typeFieldName);
							fieldInfos.Add(new ClassFieldInfo(otherFieldInfo.Name, otherFieldInfo.InternalType, otherFieldInfo.UserFriendlyType,
								otherFieldInfo.DefaultValue, className, otherFieldInfo.Location, otherFieldInfo.Description, otherFieldInfo.OtherAttributes));
						}
					}
				}

				if (!wi.IsAbstract)
				{
					var warheadInfo = new ClassInfo(
						wi.Name,
						wi.TypeSuffix,
						wi.Description,
						wi.Location,
						baseTypes.Where(x => x.TypeName != wi.NameWithTypeSuffix).Select(x => x.TypeName).ToArray(),
						fieldInfos.ToArray(),
						wi.IsAbstract);

					yield return warheadInfo;
				}
			}
		}

		/// <summary>
		/// Exclude abstract types from the result.
		/// </summary>
		/// <param name="projectileInfos">A list of all loaded projectile infos.</param>
		/// <returns>A finalized list of projectile infos.</returns>
		protected virtual IEnumerable<ClassInfo> FinalizeProjectileInfos(IList<ClassInfo> projectileInfos)
		{
			return projectileInfos.Where(x => !x.IsAbstract);
		}

		/// <summary>
		/// Resolves sprite sequence inheritance - load base types and a full list in fields - inherited or not.
		/// Excludes abstract types from the result.
		/// </summary>
		/// <param name="spriteSequenceInfos">A list of all loaded sprite sequence infos.</param>
		/// <returns>A finalized list of sprite sequence infos, with loaded fields and resolved base types.</returns>
		protected virtual IEnumerable<ClassInfo> FinalizeSpriteSequenceInfos(IList<ClassInfo> spriteSequenceInfos)
		{
			foreach (var ssi in spriteSequenceInfos)
			{
				var baseTypes = GetClassBaseTypes(ssi.Name, spriteSequenceInfos).ToArray();
				var fieldInfos = new List<ClassFieldInfo>();
				foreach (var (baseClassName, _) in baseTypes)
				{
					var baseClassInfo = spriteSequenceInfos.FirstOrDefault(x => x.Name == baseClassName);
					fieldInfos.AddRange(baseClassInfo.PropertyInfos);
				}

				if (!ssi.IsAbstract)
				{
					var spriteSequenceInfo = new ClassInfo(
						ssi.Name,
						ssi.TypeSuffix,
						ssi.Description,
						ssi.Location,
						baseTypes.Where(x => x.TypeName != ssi.NameWithTypeSuffix).Select(x => x.TypeName).ToArray(),
						fieldInfos.ToArray(),
						ssi.IsAbstract);

					yield return spriteSequenceInfo;
				}
			}
		}

		/// <summary>
		/// Resolves widget inheritance - load base types and a full list in fields - inherited or not.
		/// Excludes abstract types from the result.
		/// </summary>
		/// <param name="widgets">A list of all loaded widget types.</param>
		/// <returns>A finalized list of widget types, with loaded fields and resolved base types.</returns>
		protected virtual IEnumerable<ClassInfo> FinalizeWidgets(IList<ClassInfo> widgets)
		{
			foreach (var w in widgets)
			{
				var baseTypes = GetClassBaseTypes(w.NameWithTypeSuffix, widgets).ToArray();
				var fieldInfos = new List<ClassFieldInfo>();
				foreach (var (baseClassName, _) in baseTypes)
				{
					var baseClassInfo = widgets.FirstOrDefault(x => x.NameWithTypeSuffix == baseClassName);
					fieldInfos.AddRange(baseClassInfo.PropertyInfos);
				}

				if (w.IsAbstract)
					continue;

				var widget = new ClassInfo(
					w.Name,
					w.TypeSuffix,
					w.Description,
					w.Location,
					baseTypes.Where(x => x.TypeName != w.NameWithTypeSuffix).Select(x => x.TypeName).ToArray(),
					fieldInfos.ToArray(),
					w.IsAbstract);

				yield return widget;
			}
		}

		#endregion

		#region Helper methods

		static MemberLocation FindTypeLocationInText(string filePath, string text, string typeName, int definitionStartIndex, string memberType = "class")
		{
			var subtext = text.Substring(0, definitionStartIndex);
			subtext += text.Substring(definitionStartIndex, text.IndexOf($"{memberType} {typeName}", definitionStartIndex, StringComparison.InvariantCulture) - definitionStartIndex);
			var lines = subtext.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
			var lineNumber = lines.Length;
			var characterNumber = lines.Last().Length + memberType.Length + 1; // Add 1 for the space because reasons.
			return new MemberLocation(filePath, lineNumber, characterNumber);
		}

		static MemberLocation FindPropertyLocationInText(string filePath, string text, int definitionStartIndex)
		{
			var subtext = text.Substring(0, definitionStartIndex);
			var lines = subtext.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
			var lineNumber = lines.Length;
			var characterNumber = lines.Last().Replace('\t', ' ').Length;
			return new MemberLocation(filePath, lineNumber, characterNumber);
		}

		static IEnumerable<string> ParseBaseTypeNames(ClassDeclarationSyntax classDeclaration)
		{
			// Get inherited/implemented types.
			if (classDeclaration.BaseList != null)
			{
				// TODO: It would be useful to know what the `Requires` requires.
				foreach (var baseTypeSyntax in classDeclaration.BaseList.Types)
				{
					if (baseTypeSyntax.Type is IdentifierNameSyntax identifierNameSyntax)
						yield return identifierNameSyntax.Identifier.ValueText;

					if (baseTypeSyntax.Type is GenericNameSyntax genericNameSyntax)
						yield return genericNameSyntax.Identifier.ValueText;
				}
			}
		}

		static string ParseDescAttribute(MemberDeclarationSyntax memberDeclarationSyntax)
		{
			// NOTE: Intentionally leaving the two cycles finish iterating and keeping the final result
			// because I can't be bothered to check what AttributeLists are at present.
			var description = "";
			foreach (var attributeList in memberDeclarationSyntax.AttributeLists)
			{
				foreach (var attribute in attributeList.Attributes)
				{
					if (attribute.Name.GetText().ToString() == "Desc")
					{
						var strings = attribute.ArgumentList?.Arguments
							.Select(x => x.GetText().ToString())
							.Select(x => x.Substring(x.IndexOf('"') + 1))
							.Select(x => x.Substring(0, x.Length - 1));

						description = string.Join(" ", strings ?? Array.Empty<string>());
					}
				}
			}

			// Resolve `nameof(...)`.
			description = Regex.Replace(description, "(\"\\s*\\+\\s*nameof\\(([A-Za-z0-9.\\S]*)\\)\\s*\\+\\s*\")", "$2");

			// Resolve (multi-line) string concatenation.
			description = Regex.Replace(description, "(\"\\s*\\+\\s*\")", "");

			return description;
		}

		static ClassInfo ParseSimpleClass(string filePath, string fileText, ClassDeclarationSyntax classDeclaration, ref string typeSuffix)
		{
			var fullName = classDeclaration.Identifier.ValueText;
			var name = GetTypeNameWithoutSuffix(fullName, ref typeSuffix);
			var description = ParseDescAttribute(classDeclaration);
			var isAbstract = classDeclaration.Modifiers.Any(x => x.ValueText == "abstract");

			// Some manual string nonsense to determine the class name location inside the file.
			var classStart = classDeclaration.GetLocation().SourceSpan.Start;
			var classLocation = FindTypeLocationInText(filePath, fileText, fullName, classStart);
			var baseTypes = ParseBaseTypeNames(classDeclaration).ToArray();
			var fields = ParseClassFields(filePath, fileText, classDeclaration).ToArray();

			return new ClassInfo(name, typeSuffix, description, classLocation, baseTypes, fields, isAbstract);
		}

		static IEnumerable<ClassFieldInfo> ParseClassFields(string filePath, string fileText, ClassDeclarationSyntax classDeclaration)
		{
			// Get property (actually field) list.
			foreach (var member in classDeclaration.Members)
				if (member is FieldDeclarationSyntax fieldMember
				    && fieldMember.Modifiers.Any(x => x.ValueText == "public")
				    && fieldMember.Modifiers.All(x => x.ValueText != "static")
				    && fieldMember.Modifiers.Any(x => x.ValueText == "readonly"))
					foreach (var fieldInfo in ParseClassField(filePath, fileText, fieldMember))
						yield return fieldInfo;
		}

		static IEnumerable<ClassFieldInfo> ParseWidgetClassFields(string filePath, string fileText, ClassDeclarationSyntax classDeclaration)
		{
			// Get property (actually field) list.
			foreach (var member in classDeclaration.Members)
			{
				if (member is FieldDeclarationSyntax fieldMember
				    && fieldMember.Modifiers.Any(x => x.ValueText == "public")
				    && fieldMember.Modifiers.All(x => x.ValueText != "static"))
				{
					// Ignore any fields that are of type Func<> or Action<>.
					if (fieldMember.Declaration.Type is GenericNameSyntax genericSyntax
					    && (genericSyntax.Identifier.ValueText == "Func" || genericSyntax.Identifier.ValueText == "Action"))
						continue;

					// Ignore any fields that are of type Action.
					if (fieldMember.Declaration.Type is IdentifierNameSyntax { Identifier: { ValueText: "Action" } })
						continue;

					foreach (var fieldInfo in ParseClassField(filePath, fileText, fieldMember))
						yield return fieldInfo;
				}
			}
		}

		static IEnumerable<ClassFieldInfo> ParseClassField(string filePath, string fileText, FieldDeclarationSyntax fieldDeclarationSyntax)
		{
			foreach (var variableDeclaratorSyntax in fieldDeclarationSyntax.Declaration.Variables)
			{
				var otherAttributes = new List<(string Name, string Value)>();
				foreach (var attributeList in fieldDeclarationSyntax.AttributeLists)
				{
					foreach (var attribute in attributeList.Attributes)
					{
						var attributeName = attribute.Name.GetText().ToString();
						var attributeValue = attribute.ArgumentList?.Arguments.ToString();

						// Break because we shouldn't be parsing this field.
						if (attributeName == "FieldLoader.Ignore")
							yield break;

						// Continue so we don't run into the "unknown field attribute" case.
						if (attributeName == "Desc"
							|| attributeName == "FieldLoader.LoadUsing")
							continue;

						// Full set of attributes on trait properties for future reference.
						// The TranslateAttribute was present in `release-20210321` and was removed some time later.
						if (attributeName == "FieldLoader.Require"
								 || attributeName == "Translate"
								 || attributeName == "ActorReference"
								 || attributeName == "VoiceReference"
								 || attributeName == "VoiceSetReference"
								 || attributeName == "CursorReference"
								 || attributeName == "WeaponReference"
								 || attributeName == "PaletteReference"
								 || attributeName == "PaletteDefinition"
								 || attributeName == "SequenceReference"
								 || attributeName == "NotificationReference"
								 || attributeName == "GrantedConditionReference"
								 || attributeName == "ConsumedConditionReference"
								 || attributeName == "TranslationReference"
								 || attributeName == "LocomotorReference")
						{
							// Try to resolve `nameof(...)`.
							if (attributeValue != null)
								attributeValue = Regex.Replace(attributeValue, "(nameof\\(([A-Za-z0-9.\\S]*)\\))", "$2");

							otherAttributes.Add((attributeName, attributeValue));
						}
						else
						{
							Console.Error.WriteLine($"Unknown field attribute {attributeName} in {filePath}");
						}
					}
				}

				var propertyName = variableDeclaratorSyntax.Identifier.ValueText;
				var propertyType = HumanReadablePropertyType(fieldDeclarationSyntax.Declaration.Type);
				var userFriendlyType = UserFriendlyTypeName(propertyType);
				var defaultValue = HumanReadablePropertyDefaultValue(variableDeclaratorSyntax);
				var location = FindPropertyLocationInText(filePath, fileText, variableDeclaratorSyntax.GetLocation().SourceSpan.Start);
				var description = ParseDescAttribute(fieldDeclarationSyntax);

				// Using "???" as class name here as a temporary placeholder. That should be replaced later when resolving inheritance and inherited fields.
				yield return new ClassFieldInfo(propertyName, propertyType, userFriendlyType, defaultValue, "???", location, description, otherAttributes.ToArray());
			}
		}

		static string HumanReadablePropertyType(TypeSyntax typeSyntax)
		{
			var propertyType = string.Empty;
			try
			{
				if (typeSyntax is PredefinedTypeSyntax predefinedTypeSyntax)
					propertyType = predefinedTypeSyntax.Keyword.Text;
				else if (typeSyntax is GenericNameSyntax genericNameSyntax)
					propertyType = $"{genericNameSyntax.Identifier.Value}<{string.Join(", ", genericNameSyntax.TypeArgumentList.Arguments)}>";
				else if (typeSyntax is IdentifierNameSyntax identifierNameSyntax)
					propertyType = identifierNameSyntax.Identifier.Text;
				else if (typeSyntax is ArrayTypeSyntax arrayTypeSyntax)
					propertyType = $"{arrayTypeSyntax.ElementType.GetText()}[]";
				else if (typeSyntax is NullableTypeSyntax nullableTypeSyntax)
					propertyType = $"Nullable<{nullableTypeSyntax.ElementType.GetText()}>";
				else if (typeSyntax is QualifiedNameSyntax qualifiedNameSyntax)
					propertyType = qualifiedNameSyntax.Right.Identifier.Text;
				else
					Console.Error.WriteLine($"Unknown TypeSyntax {typeSyntax.GetType()}!");
			}
			catch (Exception e)
			{
				Console.Error.WriteLine(e);
			}

			return propertyType;
		}

		static string UserFriendlyTypeName(string typeName)
		{
			if (typeName.EndsWith("[]"))
				return $"Collection of {UserFriendlyTypeName(typeName.Substring(0, typeName.Length - 2))}";

			if (typeName.StartsWith("BitSet<"))
				return $"Collection of {UserFriendlyTypeName(typeName.Substring(7, typeName.Length - 8))}";

			if (typeName.StartsWith("HashSet<"))
				return $"Set of {UserFriendlyTypeName(typeName.Substring(8, typeName.Length - 9))}";

			if (typeName.StartsWith("Dictionary<"))
			{
				var types = typeName.Substring(11, typeName.Length - 12).Split(", ");
				return $"Dictionary with Key: {UserFriendlyTypeName(types[0])}, Value: {UserFriendlyTypeName(types[1])}";
			}

			if (typeName.StartsWith("Nullable<"))
				return $"{UserFriendlyTypeName(typeName.Substring(9, typeName.Length - 10))} (optional)";

			if (typeName == "int" || typeName == "uint")
				return "Integer";

			if (typeName == "int2")
				return "2D Integer";

			if (typeName == "float" || typeName == "decimal")
				return "Real Number";

			if (typeName == "float2")
				return "2D Real Number";

			if (typeName == "CPos")
				return "2D Cell Position";

			if (typeName == "CVec")
				return "2D Cell Vector";

			if (typeName == "WAngle")
				return "1D World Angle";

			if (typeName == "WRot")
				return "3D World Rotation";

			if (typeName == "WPos")
				return "3D World Position";

			if (typeName == "WDist")
				return "1D World Distance";

			if (typeName == "WVec")
				return "3D World Vector";

			if (typeName == "Color")
				return "Color (RRGGBB[AA] notation)";

			if (typeName == "IProjectileInfo")
				return "Projectile";

			return typeName;
		}

		static string HumanReadablePropertyDefaultValue(VariableDeclaratorSyntax declaratorSyntax)
		{
			var defaultValue = string.Empty;
			try
			{
				if (declaratorSyntax.Initializer == null)
					return defaultValue;

				defaultValue = declaratorSyntax.Initializer.Value.ToString();
				var valueKind = declaratorSyntax.Initializer.Value.Kind();
				if (valueKind == SyntaxKind.BitwiseOrExpression)
				{
					var values = new List<string>();
					var binaryExpression = (BinaryExpressionSyntax)declaratorSyntax.Initializer.Value;
					while (true)
					{
						var leftExpression = binaryExpression.Left;
						if (leftExpression is MemberAccessExpressionSyntax leftAccessExpression)
						{
							values.Add(((MemberAccessExpressionSyntax)binaryExpression.Right).Name.Identifier.Text);
							values.Add(leftAccessExpression.Name.Identifier.Text);
							break;
						}

						if (leftExpression is BinaryExpressionSyntax leftBinaryExpression)
						{
							values.Add(((MemberAccessExpressionSyntax)binaryExpression.Right).Name.Identifier.Text);
							binaryExpression = leftBinaryExpression;
						}
					}

					values.Reverse();
					defaultValue = string.Join(", ", values);
				}
				else if (valueKind == SyntaxKind.SimpleMemberAccessExpression)
					defaultValue = ((MemberAccessExpressionSyntax)declaratorSyntax.Initializer.Value).Name.Identifier.Text;
				else if (valueKind == SyntaxKind.ObjectCreationExpression)
				{
					var objectCreationExpression = (ObjectCreationExpressionSyntax)declaratorSyntax.Initializer.Value;
					if (objectCreationExpression.Initializer != null)
						defaultValue = string.Join(", ", objectCreationExpression.Initializer.Expressions.Select(x => x.ToString()));
					else if (objectCreationExpression.ArgumentList != null)
						defaultValue = objectCreationExpression.ArgumentList.Arguments.ToString();

					if (string.IsNullOrWhiteSpace(defaultValue))
						defaultValue = "(empty)";
				}
				else if (valueKind == SyntaxKind.TrueLiteralExpression || valueKind == SyntaxKind.FalseLiteralExpression)
					defaultValue = bool.FalseString;
				else if (valueKind == SyntaxKind.ArrayInitializerExpression)
					defaultValue = ((InitializerExpressionSyntax)declaratorSyntax.Initializer.Value).Expressions.ToString();
				else if (valueKind == SyntaxKind.DefaultExpression)
				{
					if (defaultValue.Contains('<') && defaultValue.Contains('>'))
						defaultValue = "(empty)";
				}
				else if (valueKind == SyntaxKind.DefaultLiteralExpression)
					defaultValue = "(empty)";
				else if (valueKind == SyntaxKind.ArrayCreationExpression)
				{
					var arrayCreationExpression = (ArrayCreationExpressionSyntax)declaratorSyntax.Initializer.Value;
					if (arrayCreationExpression.Initializer != null)
						defaultValue = string.Join(", ", arrayCreationExpression.Initializer.Expressions.Select(x => x.ToString()));

					if (string.IsNullOrWhiteSpace(defaultValue))
						defaultValue = "(empty)";
				}
				else if (valueKind == SyntaxKind.ImplicitArrayCreationExpression)
				{
					var arrayCreationExpression = (ImplicitArrayCreationExpressionSyntax)declaratorSyntax.Initializer.Value;
					defaultValue = string.Join(", ", arrayCreationExpression.Initializer.Expressions.Select(x => x.ToString()));

					if (string.IsNullOrWhiteSpace(defaultValue))
						defaultValue = "(empty)";
				}
				else if (valueKind != SyntaxKind.StringLiteralExpression
						 && valueKind != SyntaxKind.NumericLiteralExpression
						 && valueKind != SyntaxKind.NullLiteralExpression
						 && valueKind != SyntaxKind.InvocationExpression
						 && valueKind != SyntaxKind.UnaryMinusExpression
						 && valueKind != SyntaxKind.MultiplyExpression)
				{
					throw new NotImplementedException($"unsupported type {valueKind}!");
				}
			}
			catch (Exception e)
			{
				Console.Error.WriteLine(e);
			}

			return defaultValue;
		}

		protected virtual IEnumerable<(string TypeName, IEnumerable<string> ClassFields)> GetTraitBaseTypes(string fullClassName, IList<ClassInfo> knownTraitInfos)
		{
			// TODO: It would be useful to know what the `Requires` requires.
			if (fullClassName == "TraitInfo" || fullClassName == "Requires" || (fullClassName.StartsWith("I") && !fullClassName.EndsWith("Info")))
				return new[] { (fullClassName, Enumerable.Empty<string>()) };

			var traitInfo = knownTraitInfos.FirstOrDefault(x => x.NameWithTypeSuffix == fullClassName);
			if (traitInfo.NameWithTypeSuffix == null)
				return Enumerable.Empty<(string, IEnumerable<string>)>();

			var result = new List<(string TypeName, IEnumerable<string> ClassFieldInfos)>
			{
				(fullClassName, traitInfo.PropertyInfos.Select(x => x.Name))
			};

			foreach (var baseType in traitInfo.BaseTypes)
				result.AddRange(GetTraitBaseTypes(baseType, knownTraitInfos));

			return result;
		}

		protected virtual IEnumerable<(string TypeName, IEnumerable<string> ClassFields)> GetClassBaseTypes(string fullClassName, IList<ClassInfo> knownClassInfos)
		{
			var classInfo = knownClassInfos.FirstOrDefault(x => x.NameWithTypeSuffix == fullClassName);
			if (classInfo.NameWithTypeSuffix == null)
				return Enumerable.Empty<(string, IEnumerable<string>)>();

			var result = new List<(string TypeName, IEnumerable<string> ClassFieldInfos)>
			{
				(fullClassName, classInfo.PropertyInfos.Select(x => x.Name))
			};

			foreach (var inheritedType in classInfo.BaseTypes)
				result.AddRange(GetClassBaseTypes(inheritedType, knownClassInfos));

			return result;
		}

		protected static string GetTypeNameWithoutSuffix(string fullName, ref string suffix)
		{
			// Ignore the case of "Warhead", "Warhead".
			if (fullName.Length > suffix.Length && fullName.EndsWith(suffix))
				return fullName.Substring(0, fullName.Length - suffix.Length);

			suffix = string.Empty;
			return fullName;
		}
		#endregion
	}
}
