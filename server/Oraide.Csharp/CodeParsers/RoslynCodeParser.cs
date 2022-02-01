using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Oraide.Core.Entities;
using Oraide.Core.Entities.Csharp;

namespace Oraide.Csharp.CodeParsers
{
	public static class RoslynCodeParser
	{
		public static (ILookup<string, TraitInfo>, WeaponInfo) Parse(in string oraFolderPath)
		{
			var traitInfos = new List<TraitInfo>();
			var weaponInfoFields = Array.Empty<ClassFieldInfo>();
			var warheadInfos = new List<SimpleClassInfo>();
			var projectileInfos = new List<SimpleClassInfo>();

			var filePaths = Directory.EnumerateFiles(oraFolderPath, "*.cs", SearchOption.AllDirectories)
				.Where(x => !x.Contains("OpenRA.Test"));

			foreach (var filePath in filePaths)
			{
				var fileText = File.ReadAllText(filePath);
				var syntaxTree = CSharpSyntaxTree.ParseText(fileText);
				var root = syntaxTree.GetCompilationUnitRoot();

				foreach (var element in root.Members)
				{
					if (element is NamespaceDeclarationSyntax namespaceElement)
					{
						foreach (var namespaceMember in namespaceElement.Members)
						{
							if (namespaceMember is ClassDeclarationSyntax classDeclaration)
							{
								var baseTypes = ParseBaseTypes(classDeclaration).ToArray();

								// Parsing files the "if it quacks like a duck" way because the alternative would be
								// to create a whole type hierarchy to know what inherits what and that is just overkill.
								// TODO: Be smarter about how we decide what type of file this is.
								if ((filePath.Contains("Trait")
									&& (classDeclaration.Identifier.ValueText.EndsWith("Info") || classDeclaration.Identifier.ValueText.EndsWith("InfoBase")))
									|| baseTypes.Any(x => x == "TraitInfo" || x.StartsWith("TraitInfo<")))
								{
									traitInfos.AddRange(ParseTraitInfo(filePath, fileText, classDeclaration));
								}
								else if (filePath.Replace("\\", "/").EndsWith("GameRules/WeaponInfo.cs") && classDeclaration.Identifier.ValueText == "WeaponInfo")
								{
									weaponInfoFields = ParseWeaponInfo(filePath, fileText, classDeclaration).ToArray();
								}
								else if (baseTypes.Any(x => x == "IProjectileInfo"))
								{
									var projectileInfo = ParseProjectileInfo(filePath, fileText, classDeclaration);
									projectileInfos.Add(projectileInfo);
								}

								// Could be done smarter, but several levels of inheritance and not wanting to construct a full tree got us here.
								else if (baseTypes.Any(x => x.EndsWith("Warhead")))
								{
									var warheadInfo = ParseWarheadInfo(filePath, fileText, classDeclaration);
									warheadInfos.Add(warheadInfo);
								}
							}
						}
					}
				}
			}

			var weaponInfo = new WeaponInfo(weaponInfoFields, projectileInfos.ToArray(), warheadInfos.ToArray());
			return (traitInfos.ToLookup(x => x.TraitInfoName, y => y), weaponInfo);
		}

		// Files can potentially contain multiple TraitInfos.
		static IEnumerable<TraitInfo> ParseTraitInfo(string filePath, string fileText, ClassDeclarationSyntax classDeclaration)
		{
			var traitProperties = new List<ClassFieldInfo>();
			var traitInfoName = classDeclaration.Identifier.ValueText;

			// Skip classes that are not TraitInfos.
			if (!traitInfoName.EndsWith("Info"))
				yield break;

			// Get trait's DescAttribute.
			var traitDesc = ParseClassDescAttribute(classDeclaration);

			// Get TraitInfo property (actually field) list.
			foreach (var member in classDeclaration.Members)
				if (member is FieldDeclarationSyntax fieldMember)
					traitProperties.AddRange(ParseClassField(filePath, fileText, fieldMember));

			// Some manual string nonsense to determine trait name location inside the file.
			var classStart = classDeclaration.GetLocation().SourceSpan.Start;
			var classLocation = FindClassLocationInText(filePath, fileText, traitInfoName, classStart);

			// Get inherited/implemented types.
			var baseTypes = ParseBaseTypes(classDeclaration).ToArray();

			yield return new TraitInfo(traitInfoName.Substring(0, traitInfoName.Length - 4), traitInfoName, traitDesc, classLocation, baseTypes, traitProperties.ToArray());
		}

		static IEnumerable<ClassFieldInfo> ParseWeaponInfo(string filePath, string fileText, ClassDeclarationSyntax classDeclaration)
		{
			return ParseClassFields(filePath, fileText, classDeclaration);
		}

		static SimpleClassInfo ParseProjectileInfo(string filePath, string fileText, ClassDeclarationSyntax classDeclaration)
		{
			return ParseSimpleClass(filePath, fileText, classDeclaration);
		}

		static SimpleClassInfo ParseWarheadInfo(string filePath, string fileText, ClassDeclarationSyntax classDeclaration)
		{
			return ParseSimpleClass(filePath, fileText, classDeclaration);
		}

		static SimpleClassInfo ParseSimpleClass(string filePath, string fileText, ClassDeclarationSyntax classDeclaration)
		{
			var projectileInfoName = classDeclaration.Identifier.ValueText;
			var projectileName = projectileInfoName.EndsWith("Info") ? projectileInfoName.Substring(0, projectileInfoName.Length - 4) : projectileInfoName;
			var description = ParseClassDescAttribute(classDeclaration);

			// Some manual string nonsense to determine the class name location inside the file.
			var classStart = classDeclaration.GetLocation().SourceSpan.Start;
			var classLocation = FindClassLocationInText(filePath, fileText, projectileName, classStart);
			var baseTypes = ParseBaseTypes(classDeclaration).ToArray();
			var fields = ParseClassFields(filePath, fileText, classDeclaration).ToArray();

			return new SimpleClassInfo(projectileName, projectileInfoName, description, classLocation, baseTypes, fields);
		}

		static IEnumerable<ClassFieldInfo> ParseClassFields(string filePath, string fileText, ClassDeclarationSyntax classDeclaration)
		{
			// Get property (actually field) list.
			foreach (var member in classDeclaration.Members)
				if (member is FieldDeclarationSyntax fieldMember)
					foreach (var fieldInfo in ParseClassField(filePath, fileText, fieldMember))
						yield return fieldInfo;
		}

		static IEnumerable<ClassFieldInfo> ParseClassField(string filePath, string fileText, FieldDeclarationSyntax fieldDeclarationSyntax)
		{
			foreach (var variableDeclaratorSyntax in fieldDeclarationSyntax.Declaration.Variables)
			{
				var fieldDesc = "";
				var loadUsing = "";
				foreach (var attributeList in fieldDeclarationSyntax.AttributeLists)
				{
					foreach (var attribute in attributeList.Attributes)
					{
						if (attribute.Name.GetText().ToString() == "Desc")
						{
							var strings = attribute.ArgumentList.Arguments
								.Select(x => x.GetText().ToString())
								.Select(x => x.Substring(x.IndexOf('"') + 1))
								.Select(x => x.Substring(0, x.Length - 1));

							fieldDesc = string.Join(" ", strings);
						}
						else if (attribute.Name.GetText().ToString() == "FieldLoader.LoadUsing")
						{
							loadUsing = attribute.ArgumentList.Arguments.ToString();
						}
						else
						{
							// Full set of attributes on trait properties for future reference.
							if (attribute.Name.GetText().ToString() != "FieldLoader.Require"
								&& attribute.Name.GetText().ToString() != "FieldLoader.Ignore"
								&& attribute.Name.GetText().ToString() != "ActorReference"
								&& attribute.Name.GetText().ToString() != "VoiceReference"
								&& attribute.Name.GetText().ToString() != "VoiceSetReference"
								&& attribute.Name.GetText().ToString() != "CursorReference"
								&& attribute.Name.GetText().ToString() != "WeaponReference"
								&& attribute.Name.GetText().ToString() != "PaletteReference"
								&& attribute.Name.GetText().ToString() != "PaletteDefinition"
								&& attribute.Name.GetText().ToString() != "SequenceReference"
								&& attribute.Name.GetText().ToString() != "NotificationReference"
								&& attribute.Name.GetText().ToString() != "GrantedConditionReference"
								&& attribute.Name.GetText().ToString() != "ConsumedConditionReference"
								&& attribute.Name.GetText().ToString() != "LocomotorReference")
								fieldDesc = fieldDesc;
						}
					}
				}

				// Resolve `nameof(...)`.
				fieldDesc = Regex.Replace(fieldDesc, "(\"\\s*\\+\\s*nameof\\(([A-Za-z0-9.\\S]*)\\)\\s*\\+\\s*\")", "$2");

				var propertyName = variableDeclaratorSyntax.Identifier.ValueText;
				var propertyType = HumanReadablePropertyType(fieldDeclarationSyntax.Declaration.Type);
				var defaultValue = HumanReadablePropertyDefaultValue(variableDeclaratorSyntax);
				var location = FindPropertyLocationInText(filePath, fileText, variableDeclaratorSyntax.GetLocation().SourceSpan.Start);
				yield return new ClassFieldInfo(propertyName, propertyType, defaultValue, location, fieldDesc, loadUsing);
			}
		}

		static string ParseClassDescAttribute(ClassDeclarationSyntax classDeclaration)
		{
			var description = "";
			foreach (var attributeList in classDeclaration.AttributeLists)
			{
				foreach (var attribute in attributeList.Attributes)
				{
					if (attribute.Name.GetText().ToString() == "Desc")
					{
						var strings = attribute.ArgumentList.Arguments
							.Select(x => x.GetText().ToString())
							.Select(x => x.Substring(x.IndexOf('"') + 1))
							.Select(x => x.Substring(0, x.Length - 1));

						description = string.Join(" ", strings);
					}
				}
			}

			// Resolve `nameof(...)`.
			description = Regex.Replace(description, "(\"\\s*\\+\\s*nameof\\(([A-Za-z0-9.\\S]*)\\)\\s*\\+\\s*\")", "$2");

			return description;
		}

		static IEnumerable<string> ParseBaseTypes(ClassDeclarationSyntax classDeclaration)
		{
			// Get inherited/implemented types.
			if (classDeclaration.BaseList != null)
			{
				foreach (var baseTypeSyntax in classDeclaration.BaseList.Types)
				{
					if (baseTypeSyntax.Type is IdentifierNameSyntax identifierNameSyntax)
						yield return identifierNameSyntax.Identifier.ValueText;

					if (baseTypeSyntax.Type is GenericNameSyntax genericNameSyntax)
						yield return genericNameSyntax.Identifier.ValueText;
				}
			}
		}

		static MemberLocation FindClassLocationInText(string filePath, string text, string traitInfoName, int definitionStartIndex)
		{
			var subtext = text.Substring(0, definitionStartIndex);
			subtext += text.Substring(definitionStartIndex, text.IndexOf($"class {traitInfoName}", definitionStartIndex, StringComparison.InvariantCulture) - definitionStartIndex);
			var lines = subtext.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
			var lineNumber = lines.Length;
			var characterNumber = lines.Last().Length + 6;
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

		static string HumanReadablePropertyType(TypeSyntax typeSyntax)
		{
			var propertyType = string.Empty;
			try
			{
				if (typeSyntax is PredefinedTypeSyntax predefinedTypeSyntax)
					propertyType = predefinedTypeSyntax.Keyword.Text;
				else if (typeSyntax is GenericNameSyntax genericNameSyntax)
					propertyType = $"{genericNameSyntax.Identifier.Value} of {genericNameSyntax.TypeArgumentList.Arguments}";
				else if (typeSyntax is IdentifierNameSyntax identifierNameSyntax)
					propertyType = identifierNameSyntax.Identifier.Text;
			}
			catch (Exception e)
			{
				Console.Error.WriteLine(e);
			}

			return propertyType;
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
				         && valueKind != SyntaxKind.UnaryMinusExpression)
					throw new NotImplementedException($"unsupported type {valueKind}!");
			}
			catch (Exception e)
			{
				Console.Error.WriteLine(e);
			}

			return defaultValue;
		}
	}
}

// RoslynCodeParserSymbolProvider
// DecompileBinarySymbolProvider
// StaticSourceSymbolProvider
