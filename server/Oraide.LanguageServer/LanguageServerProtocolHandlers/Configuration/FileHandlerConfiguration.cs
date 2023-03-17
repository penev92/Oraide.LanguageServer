using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Abstractions.LanguageServerProtocolHandlers.Configuration;

namespace Oraide.LanguageServer.LanguageServerProtocolHandlers.Configuration
{
	class FileTypeHandlerConfiguration : IFileTypeHandlerConfiguration
	{
		public IReadOnlyDictionary<FileType, Type> FileHandlers { get; private init; }

		IServiceProvider ServiceProvider { get; init; }

		FileTypeHandlerConfiguration() { }

		public static IFileTypeHandlerConfigurationBuilder CreateBuilder()
		{
			return new FileTypeHandlerConfigurationBuilder();
		}

		public TOperationService GetService<TOperationService>(CursorTarget cursorTarget) where TOperationService : class
		{
			if (!FileHandlers.TryGetValue(cursorTarget.FileType, out var serviceType))
				return null;

			return ServiceProvider.GetRequiredService(serviceType) as TOperationService;
		}

		class FileHandlerConfiguration : IFileHandlerConfiguration
		{
			public Type ServiceType { get; private set; }

			readonly IFileTypeHandlerConfigurationBuilder builder;

			public FileHandlerConfiguration(IFileTypeHandlerConfigurationBuilder builder)
			{
				this.builder = builder;
			}

			public IFileTypeHandlerConfigurationBuilder Use<TService>() where TService : class
			{
				ServiceType = typeof(TService);
				return builder;
			}
		}

		class FileTypeHandlerConfigurationBuilder : IFileTypeHandlerConfigurationBuilder
		{
			readonly List<(FileType FileType, FileHandlerConfiguration HandlerConfiguration)> handlers = new();

			public IFileHandlerConfiguration For(FileType fileType)
			{
				var handlerConfiguration = new FileHandlerConfiguration(this);
				handlers.Add((fileType, handlerConfiguration));
				return handlerConfiguration;
			}

			public IFileTypeHandlerConfiguration Build(IServiceProvider serviceProvider)
			{
				var dict = new Dictionary<FileType, Type>();
				foreach (var (fileType, handlerConfiguration) in handlers)
					dict.Add(fileType, handlerConfiguration.ServiceType);

				return new FileTypeHandlerConfiguration
				{
					FileHandlers = new ReadOnlyDictionary<FileType, Type>(dict),
					ServiceProvider = serviceProvider
				};
			}
		}
	}
}
