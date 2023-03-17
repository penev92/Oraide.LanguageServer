using System;

namespace Oraide.LanguageServer.Abstractions.LanguageServerProtocolHandlers.Configuration
{
	interface IFileHandlerConfiguration
	{
		Type ServiceType { get; }

		IFileTypeHandlerConfigurationBuilder Use<TService>() where TService : class;
	}
}
