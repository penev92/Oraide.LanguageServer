using LspTypes;

namespace Oraide.LanguageServer.Extensions
{
	public static class CompletionItems
	{
		public static CompletionItem Inherits = new()
		{
			Label = "Inherits",
			Kind = CompletionItemKind.Constructor,
			Detail = "Allows rule inheritance.",
			CommitCharacters = new[] { ":" }
		};

		public static CompletionItem Defaults = new()
		{
			Label = "Defaults",
			Kind = CompletionItemKind.Constructor,
			Detail = "Sets default values for all sequences of this image.",
			CommitCharacters = new[] { ":" }
		};

		public static CompletionItem Warhead = new()
		{
			Label = "Warhead",
			Kind = CompletionItemKind.Constructor,
			Detail = "A warhead to be used by this weapon. You can list several of these.",
			CommitCharacters = new[] { ":" }
		};

		public static CompletionItem True = new()
		{
			Label = "true",
			Kind = CompletionItemKind.Value,
			Detail = "A boolean value."
		};

		public static CompletionItem False = new()
		{
			Label = "false",
			Kind = CompletionItemKind.Value,
			Detail = "A boolean value."
		};
	}
}
