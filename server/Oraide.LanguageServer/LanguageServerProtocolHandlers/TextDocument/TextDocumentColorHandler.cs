﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LspTypes;
using Oraide.LanguageServer.Abstractions.LanguageServerProtocolHandlers;
using Oraide.LanguageServer.Caching;

namespace Oraide.LanguageServer.LanguageServerProtocolHandlers.TextDocument
{
	public class TextDocumentColorHandler : BaseRpcMessageHandler
	{
		public TextDocumentColorHandler(SymbolCache symbolCache, OpenFileCache openFileCache)
			: base(symbolCache, openFileCache) { }

		[OraideCustomJsonRpcMethodTag("textDocument/documentColor")]
		public IEnumerable<ColorInformation> DocumentColor(DocumentColorParams request)
		{
			lock (LockObject)
			{
				try
				{
					if (trace)
						Console.Error.WriteLine("<-- TextDocument-DocumentColor");

					var results = new List<ColorInformation>();
					var (yamlNodes, flattenedYamlNodes, lines) = openFileCache[request.TextDocument.Uri];
					foreach (var node in flattenedYamlNodes)
					{
						if (node.Key != null && node.Key.EndsWith("Color") && node.ChildNodes == null)
						{
							var colorString = node.Value;
							if (TryParseColor(colorString, out var color))
							{
								var line = lines[node.Location.LineNumber - 1];
								var valueStartIndex = line.IndexOf("Color: ", StringComparison.Ordinal);
								if (valueStartIndex > 0)
								{
									valueStartIndex += 7;
									var colorInfo = new ColorInformation
									{
										Color = color,
										Range = new LspTypes.Range
										{
											Start = new Position((uint)(node.Location.LineNumber - 1), (uint)valueStartIndex),
											End = new Position((uint)(node.Location.LineNumber - 1), (uint)(valueStartIndex + node.Value.Length))
										}
									};

									results.Add(colorInfo);
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