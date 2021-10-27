using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Oraide.LanguageServer.CodeParsers
{
	public static class RoslynCodeParser
	{
		public static IDictionary<string, TraitInfo> Parse(in string oraFolderPath)
		{
			var traitDictionary = new Dictionary<string, TraitInfo>();

			var filePaths = Directory.EnumerateFiles(oraFolderPath, "*.cs", SearchOption.AllDirectories);
			foreach (var filePath in filePaths/*.Where(x => x.Contains("Disguise"))*/)
			{
				var text = File.ReadAllText(filePath);
				var fileName = Path.GetFileName(filePath).Split('.')[0];
				var subTextStart = text.IndexOf($"class {fileName}Info", StringComparison.Ordinal);

				if (filePath.Contains("Trait") && subTextStart > 0)
				{
					var syntaxTree = CSharpSyntaxTree.ParseText(text);
					var root = syntaxTree.GetCompilationUnitRoot();

					foreach (var element in root.Members)
						if (element is NamespaceDeclarationSyntax namespaceElement)
							foreach (var namespaceMember in namespaceElement.Members)
								if (namespaceMember is ClassDeclarationSyntax classElement)
								{
									var baseTypes = new List<string>();
									var traitProperties = new List<TraitPropertyInfo>();
									var traitName = classElement.Identifier.ValueText;

									// Skip classes that are not TraitInfos.
									if (!traitName.EndsWith("Info"))
										continue;

									// Get trait's DescAttribute.
									var traitDesc = "";
									foreach (var attributeList in classElement.AttributeLists)
										foreach (var attribute in attributeList.Attributes)
											if (attribute.Name.GetText().ToString() == "Desc")
												traitDesc = attribute.ArgumentList.Arguments.ToString();

									// Get inherited/implemented types.
									if (classElement.BaseList != null)
									{
										foreach (var baseTypeSyntax in classElement.BaseList.Types)
										{
											if (baseTypeSyntax.Type is IdentifierNameSyntax identifierNameSyntax)
												baseTypes.Add(identifierNameSyntax.Identifier.ValueText);

											if (baseTypeSyntax.Type is GenericNameSyntax genericNameSyntax)
												baseTypes.Add(genericNameSyntax.Identifier.ValueText);
										}
									}

									// Get TraitInfo property list.
									foreach (var member in classElement.Members)
										if (member is FieldDeclarationSyntax fieldMember)
											foreach (var variableDeclaratorSyntax in fieldMember.Declaration.Variables)
												traitProperties.Add(new TraitPropertyInfo(variableDeclaratorSyntax.Identifier.ValueText, new MemberLocation(filePath, 0, 0))); // TODO:

									// Finally, add the TraitInfo to the list of loaded TraitInfos.
									var location = new MemberLocation(filePath, 0, 0); // TODO:
									var traitInfo = new TraitInfo(traitName, traitDesc, location, baseTypes.ToArray(), traitProperties.ToArray());
									traitDictionary.Add(traitName, traitInfo);
								}
				}
			}

			return traitDictionary;
		}
	}
}

// RoslynCodeParserSymbolProvider
// DecompileBinarySymbolProvider
// StaticSourceSymbolProvider
