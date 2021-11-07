using System;
using System.Collections.Generic;
using System.Linq;
using LspTypes;
using Oraide.LanguageServer.Abstractions.LanguageServerProtocolHandlers;
using Oraide.LanguageServer.Caching;

namespace Oraide.LanguageServer.LanguageServerProtocolHandlers.TextDocument
{
	public class TextDocumentCompletionHandler : BaseRpcMessageHandler
	{
		public TextDocumentCompletionHandler(SymbolCache symbolCache, OpenFileCache openFileCache)
			: base(symbolCache, openFileCache) { }

		[OraideCustomJsonRpcMethodTag(Methods.TextDocumentCompletionName)]
		public CompletionList CompletionTextDocument(CompletionParams completionParams)
		{
			lock (LockObject)
			{
				try
				{
					if (trace)
					{
						Console.Error.WriteLine("<-- TextDocument-Completion");
						Console.Error.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(completionParams));

						return new CompletionList
						{
							IsIncomplete = false,
							Items = GetCompletionItems().ToArray()
						};
					}

				}
				catch (Exception e)
				{
				}

				return null;
			}
		}

		IEnumerable<CompletionItem> GetCompletionItems()
		{
			var traitNames = symbolCache.TraitInfos.Select(x => new CompletionItem
			{
				Label = x.Value.TraitName,
				Kind = CompletionItemKind.Class,
				Detail = "Trait name. Expand for details >",
				Documentation = x.Value.TraitDescription,
				CommitCharacters = new[] { ":" }
			});

			var actorNames = symbolCache.ActorDefinitions.Select(x => new CompletionItem
			{
				Label = x.Key,
				Kind = CompletionItemKind.Unit,
				Detail = "Actor name",
				CommitCharacters = new[] { ":" }
			});

			var conditionNames = symbolCache.ConditionDefinitions.Select(x => new CompletionItem
			{
				Label = x.Key,
				Kind = CompletionItemKind.Value,
				Detail = "Condition",
				Documentation = "Conditions are arbitrary user-defined strings that are used across multiple actors/weapons/traits."
			});

			return traitNames.Union(actorNames).Union(conditionNames);
		}
	}
}
