using System;
using LspTypes;

namespace Oraide.LanguageServer.LanguageServerProtocolHandlers.TextDocument
{
	public class TextDocumentHoverHandler : BaseRpcMessageHandler
	{
		[OraideCustomJsonRpcMethodTag(Methods.TextDocumentHoverName)]
		public Hover Hover(TextDocumentPositionParams positionParams)
		{
			lock (_object)
			{
				try
				{
					if (trace)
					{
						Console.Error.WriteLine("<-- TextDocument-Hover");
						// Console.Error.WriteLine(arg.ToString());
					}

					// if (TryGetTargetNode(positionParams, out var targetNode, out var targetType, out var targetString))
					// {
					// 	if (TryGetTargetCodeHoverInfo(targetNode, out var codeHoverInfo))
					// 		return HoverFromHoverInfo(codeHoverInfo.Content, codeHoverInfo.Range);
					//
					// 	if (TryGetTargetYamlHoverInfo(targetNode, out var yamlHoverInfo))
					// 		return HoverFromHoverInfo(yamlHoverInfo.Content, yamlHoverInfo.Range);
					// }
				}
				catch (Exception)
				{
				}

				return null;
			}
		}
	}
}
