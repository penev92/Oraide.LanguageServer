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
		public static ILookup<string, TraitInfo> Parse(in string oraFolderPath)
		{
			var traitInfos = new List<TraitInfo>();

			var filePaths = Directory.EnumerateFiles(oraFolderPath, "*.cs", SearchOption.AllDirectories);
			foreach (var filePath in filePaths)
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
									var traitInfoName = classElement.Identifier.ValueText;

									// Skip classes that are not TraitInfos.
									if (!traitInfoName.EndsWith("Info"))
										continue;

									// Get trait's DescAttribute.
									var traitDesc = "";
									foreach (var attributeList in classElement.AttributeLists)
										foreach (var attribute in attributeList.Attributes)
											if (attribute.Name.GetText().ToString() == "Desc")
											{
												var strings = attribute.ArgumentList.Arguments
													.Select(x => x.GetText().ToString())
													.Select(x => x.Substring(x.IndexOf('"') + 1))
													.Select(x => x.Substring(0, x.Length - 1));

												traitDesc = string.Join(" ", strings);
											}

									// Resolve `nameof(...)`.
									traitDesc = Regex.Replace(traitDesc, "(\"\\s*\\+\\s*nameof\\(([A-Za-z0-9.\\S]*)\\)\\s*\\+\\s*\")", "$2");

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
											{
												var fieldDesc = "";
												var loadUsing = "";
												foreach (var attributeList in fieldMember.AttributeLists)
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
												var propertyType = HumanReadablePropertyType(fieldMember.Declaration.Type);
												var defaultValue = HumanReadablePropertyDefaultValue(variableDeclaratorSyntax);
												var location = FindPropertyLocationInText(filePath, text, variableDeclaratorSyntax.GetLocation().SourceSpan.Start);
												traitProperties.Add(new TraitPropertyInfo(propertyName, propertyType, defaultValue, location, fieldDesc, loadUsing));
											}

									// Some manual string nonsense to determine trait name location inside the file.
									var classStart = classElement.GetLocation().SourceSpan.Start;
									var classLocation = FindClassLocationInText(filePath, text, traitInfoName, classStart);

									// Finally, add the TraitInfo to the list of loaded TraitInfos.
									var traitInfo = new TraitInfo(traitInfoName.Substring(0, traitInfoName.Length - 4), traitInfoName, traitDesc, classLocation, baseTypes.ToArray(), traitProperties.ToArray());
									traitInfos.Add(traitInfo);
								}
				}
			}

			return traitInfos.ToLookup(x => x.TraitInfoName, y => y);
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
				Console.WriteLine(e);
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
				Console.WriteLine(e);
			}

			return defaultValue;
		}
	}
}

// RoslynCodeParserSymbolProvider
// DecompileBinarySymbolProvider
// StaticSourceSymbolProvider
