using System;
using System.Collections.Generic;
using Oraide.Core.Entities.MiniYaml;

namespace Oraide.LanguageServer.Abstractions.LanguageServerProtocolHandlers.Configuration
{
	public interface IFileTypeHandlerConfiguration
	{
		IReadOnlyDictionary<FileType, Type> FileHandlers { get; }

		TOperationService GetService<TOperationService>(CursorTarget cursorTarget) where TOperationService : class;
	}
}
