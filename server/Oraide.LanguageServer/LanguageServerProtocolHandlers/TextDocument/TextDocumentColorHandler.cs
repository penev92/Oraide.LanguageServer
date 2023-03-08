using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using LspTypes;
using Oraide.Core;
using Oraide.LanguageServer.Abstractions.LanguageServerProtocolHandlers;
using Oraide.LanguageServer.Caching;

namespace Oraide.LanguageServer.LanguageServerProtocolHandlers.TextDocument
{
	public class TextDocumentColorHandler : BaseRpcMessageHandler
	{
		const string RegexMatchPattern = "(?:\t|[a-zA-Z0-9])(Color[s]*)(?:[:A-Z])";

		public TextDocumentColorHandler(SymbolCache symbolCache, OpenFileCache openFileCache)
			: base(symbolCache, openFileCache, null) { }

		[OraideCustomJsonRpcMethodTag("textDocument/documentColor")]
		public IEnumerable<ColorInformation> DocumentColor(DocumentColorParams request)
		{
			lock (LockObject)
			{
				try
				{
					if (trace)
						Console.Error.WriteLine("<-- TextDocument-DocumentColor");

					// HACK HACK HACK!!!
					// For whatever reason we receive the file URI borked - looks to be encoded for JSON, but the deserialization doesn't fix it.
					// No idea if this is an issue with VSCode or the LSP library used as there are currently no clients for other text editors.
					var incomingFileUriString = OpenRaFolderUtils.NormalizeFileUriString(request.TextDocument.Uri);

					var results = new List<ColorInformation>();
					var (yamlNodes, flattenedYamlNodes, lines) = openFileCache[incomingFileUriString];
					foreach (var node in flattenedYamlNodes)
					{
						if (node.Key == null || node.ChildNodes != null)
							continue;

						var line = lines[node.Location.LineNumber - 1];
						var matches = Regex.Match(line, RegexMatchPattern, RegexOptions.Compiled);
						if (matches.Groups.Count > 1)
						{
							var valueStartIndex = line.IndexOf(": ", StringComparison.Ordinal);
							if (valueStartIndex < 0)
								continue;

							valueStartIndex += 2;
							var valueString = node.Value;
							var previousWasEmpty = true;
							for (var i = 0; i < valueString.Length; i++)
							{
								if (valueString[i] == '\t' || valueString[i] == ' ' || valueString[i] == ',')
								{
									previousWasEmpty = true;
									continue;
								}

								if (previousWasEmpty && char.IsLetterOrDigit(valueString[i]))
								{
									previousWasEmpty = false;
									var startIndex = i;
									for (; i < valueString.Length; i++)
										if (!char.IsLetterOrDigit(valueString[i]))
											break;

									var endIndex = i;

									var colorString = line.Substring(valueStartIndex + startIndex, endIndex - startIndex);
									if (TryParseColor(colorString, out var color))
									{
										var colorInfo = new ColorInformation
										{
											Color = color,
											Range = new LspTypes.Range
											{
												Start = new Position((uint)(node.Location.LineNumber - 1), (uint)(valueStartIndex + startIndex)),
												End = new Position((uint)(node.Location.LineNumber - 1), (uint)(valueStartIndex + startIndex + colorString.Length))
											}
										};

										results.Add(colorInfo);
									}
								}
							}
						}
					}

					return results;
				}
				catch (Exception e)
				{
					Console.Error.WriteLine("EXCEPTION!!!");
					Console.Error.WriteLine(e.ToString());
				}

				return Enumerable.Empty<ColorInformation>();
			}
		}

		[OraideCustomJsonRpcMethodTag("textDocument/colorPresentation")]
		public IEnumerable<ColorPresentation> ColorPresentation(ColorPresentationParams request)
		{
			lock (LockObject)
			{
				try
				{
					if (trace)
						Console.Error.WriteLine("<-- TextDocument-ColorPresentation");

					var results = new List<ColorPresentation>();

					var colorPresentation = new ColorPresentation
					{
						Label = ColorToString(request.Color)
					};

					results.Add(colorPresentation);

					return results;
				}
				catch (Exception e)
				{
					Console.Error.WriteLine("EXCEPTION!!!");
					Console.Error.WriteLine(e.ToString());
				}

				return Enumerable.Empty<ColorPresentation>();
			}
		}

		static bool TryParseColor(string value, out Color color)
		{
			color = default;
			value = value.Trim();
			if (value.Length != 6 && value.Length != 8)
				return false;

			byte alpha = 255;
			if (!byte.TryParse(value.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var red)
			    || !byte.TryParse(value.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var green)
			    || !byte.TryParse(value.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var blue))
				return false;

			if (value.Length == 8 && !byte.TryParse(value.Substring(6, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out alpha))
				return false;

			color = new Color
			{
				Alpha = (float)alpha / 255,
				Red = (float)red / 255,
				Green = (float)green / 255,
				Blue = (float)blue / 255
			};

			return true;
		}

		static string ColorToString(Color color)
		{
			if (Math.Abs(color.Alpha - 1) < 0.000000001)
				return ((int)(color.Red * 255)).ToString("X2")
				       + ((int)(color.Green * 255)).ToString("X2")
				       + ((int)(color.Blue * 255)).ToString("X2");

			return ((int)(color.Red * 255)).ToString("X2")
			       + ((int)(color.Green * 255)).ToString("X2")
			       + ((int)(color.Blue * 255)).ToString("X2")
			       + ((int)(color.Alpha * 255)).ToString("X2");
		}
	}
}
