using System;
using Oraide.Core.Entities.MiniYaml;

namespace Oraide.LanguageServer.Abstractions.LanguageServerProtocolHandlers.Configuration
{
	interface IFileTypeHandlerConfigurationBuilder
	{
		IFileHandlerConfiguration For(FileType fileType);

		IFileTypeHandlerConfiguration Build(IServiceProvider serviceProvider);
	}
}
