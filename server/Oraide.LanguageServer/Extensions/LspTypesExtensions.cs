using System;
using System.Collections.Generic;
using LspTypes;
using Oraide.Core.Entities;
using Oraide.Core.Entities.Csharp;
using Oraide.Core.Entities.MiniYaml;
using Range = LspTypes.Range;

namespace Oraide.LanguageServer.Extensions
{
	static class LspTypesExtensions
	{
		public static Range ToRange(this CursorTarget cursorTarget)
		{
			return new Range
			{
				Start = new Position((uint)cursorTarget.TargetStart.LineNumber, (uint)cursorTarget.TargetStart.CharacterPosition),
				End = new Position((uint)cursorTarget.TargetEnd.LineNumber, (uint)cursorTarget.TargetEnd.CharacterPosition)
			};
		}

		public static Range ToRange(this MemberLocation memberLocation, int length)
		{
			return new Range
			{
				Start = new Position((uint)(memberLocation.LineNumber - 1), (uint)memberLocation.CharacterPosition),
				End = new Position((uint)(memberLocation.LineNumber - 1), (uint)(memberLocation.CharacterPosition + length))
			};
		}

		public static Location ToLspLocation(this MemberLocation memberLocation, int length)
		{
			return new Location
			{
				Uri = memberLocation.FileUri.AbsoluteUri,
				Range = memberLocation.ToRange(length)
			};
		}

		public static CompletionItem ToCompletionItem(this TraitInfo traitInfo)
		{
			return new CompletionItem
			{
				Label = traitInfo.TraitName,
				Kind = CompletionItemKind.Class,
				Detail = "Trait name. Expand for details >",
				Documentation = traitInfo.TraitDescription,
				CommitCharacters = new[] { ":" }
			};
		}

		public static CompletionItem ToCompletionItem(this ClassFieldInfo classFieldInfo)
		{
			return new CompletionItem
			{
				Label = classFieldInfo.Name,
				Kind = CompletionItemKind.Field,
				Detail = "Class property. Expand for details >",
				Documentation = classFieldInfo.Description,
				CommitCharacters = new[] {":"}
			};
		}

		public static CompletionItem ToCompletionItem(this SimpleClassInfo classInfo, string detail = null)
		{
			return new CompletionItem
			{
				Label = classInfo.Name,
				Kind = CompletionItemKind.Class,
				Detail = detail ?? "Unknown C# class",
				Documentation = classInfo.Description,
				CommitCharacters = new[] { ":" }
			};
		}

		public static CompletionItem ToCompletionItem(this ActorDefinition actorDefinition)
		{
			return new CompletionItem
			{
				Label = actorDefinition.Name,
				Kind = CompletionItemKind.Unit,
				Detail = "Actor name",
				CommitCharacters = new[] { ":" }
			};
		}

		public static CompletionItem ToCompletionItem(this WeaponDefinition weaponDefinition)
		{
			return new CompletionItem
			{
				Label = weaponDefinition.Name,
				Kind = CompletionItemKind.Unit,
				Detail = "Weapon name",
				CommitCharacters = new[] { ":" }
			};
		}

		public static CompletionItem ToCompletionItem(this ConditionDefinition conditionDefinition)
		{
			return new CompletionItem
			{
				Label = conditionDefinition.Name,
				Kind = CompletionItemKind.Value,
				Detail = "Condition",
				CommitCharacters = new[] { ":" }
			};
		}

		public static CompletionItem ToCompletionItem(this CursorDefinition cursorDefinition)
		{
			return new CompletionItem
			{
				Label = cursorDefinition.Name,
				Kind = CompletionItemKind.Value,
				Detail = "Cursor name",
				CommitCharacters = new[] { ":" }
			};
		}

		public static CompletionItem ToCompletionItem(this PaletteDefinition paletteDefinition)
		{
			return new CompletionItem
			{
				Label = paletteDefinition.Name,
				Kind = CompletionItemKind.Value,
				Detail = "Palette definition",
				CommitCharacters = new[] { ":" }
			};
		}

		public static CompletionItem ToCompletionItem(this SpriteSequenceImageDefinition spriteSequenceImageDefinition)
		{
			return new CompletionItem
			{
				Label = spriteSequenceImageDefinition.Name,
				Kind = CompletionItemKind.Unit,
				Detail = "Sprite sequence image definition",
				CommitCharacters = new[] { ":" }
			};
		}

		public static CompletionItem ToCompletionItem(this SpriteSequenceDefinition spriteSequenceDefinition)
		{
			return new CompletionItem
			{
				Label = spriteSequenceDefinition.Name,
				Kind = CompletionItemKind.Value,
				Detail = "Sprite sequence definition",
				CommitCharacters = new[] { ":" }
			};
		}

		public static IEnumerable<SymbolInformation> ToSymbolInformation(this ActorDefinition actorDefinition)
		{
			yield return new SymbolInformation
			{
				Name = actorDefinition.Name,
				Kind = SymbolKind.Struct,
				Tags = Array.Empty<SymbolTag>(),
				Location = actorDefinition.Location.ToLspLocation(actorDefinition.Name.Length)
			};

			if (!string.IsNullOrEmpty(actorDefinition.TooltipName) && actorDefinition.Name.ToLowerInvariant() != actorDefinition.TooltipName.ToLowerInvariant())
			{
				yield return new SymbolInformation
				{
					Name = actorDefinition.TooltipName,
					Kind = SymbolKind.Struct,
					Tags = Array.Empty<SymbolTag>(),
					Location = actorDefinition.Location.ToLspLocation(actorDefinition.Name.Length),
					ContainerName = actorDefinition.Name
				};
			}
		}

		public static SymbolInformation ToSymbolInformation(this WeaponDefinition weaponDefinition)
		{
			return new SymbolInformation
			{
				Name = weaponDefinition.Name,
				Kind = SymbolKind.Struct,
				Tags = Array.Empty<SymbolTag>(),
				Location = weaponDefinition.Location.ToLspLocation(weaponDefinition.Name.Length)
			};
		}

		public static SymbolInformation ToSymbolInformation(this ConditionDefinition conditionDefinition)
		{
			return new SymbolInformation
			{
				Name = conditionDefinition.Name,
				Kind = SymbolKind.String,
				Tags = Array.Empty<SymbolTag>(),
				Location = conditionDefinition.Location.ToLspLocation(conditionDefinition.Name.Length)
			};
		}

		public static SymbolInformation ToSymbolInformation(this CursorDefinition cursorDefinition)
		{
			return new SymbolInformation
			{
				Name = cursorDefinition.Name,
				Kind = SymbolKind.String,
				Tags = Array.Empty<SymbolTag>(),
				Location = cursorDefinition.Location.ToLspLocation(cursorDefinition.Name.Length)
			};
		}

		public static SymbolInformation ToSymbolInformation(this PaletteDefinition paletteDefinition)
		{
			var name = paletteDefinition.Name ?? paletteDefinition.Type;
			return new SymbolInformation
			{
				Name = name,
				Kind = SymbolKind.String,
				Tags = Array.Empty<SymbolTag>(),
				Location = paletteDefinition.Location.ToLspLocation(paletteDefinition.Type.Length)
			};
		}

		public static SymbolInformation ToSymbolInformation(this SpriteSequenceImageDefinition spriteSequenceImageDefinition)
		{
			return new SymbolInformation
			{
				Name = spriteSequenceImageDefinition.Name,
				Kind = SymbolKind.Struct,
				Tags = Array.Empty<SymbolTag>(),
				Location = spriteSequenceImageDefinition.Location.ToLspLocation(spriteSequenceImageDefinition.Name.Length)
			};
		}
	}
}
