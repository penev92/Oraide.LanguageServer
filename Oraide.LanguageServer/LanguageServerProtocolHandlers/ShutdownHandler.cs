﻿using System;
using LspTypes;
using Newtonsoft.Json.Linq;
using Oraide.LanguageServer.Abstractions.LanguageServerProtocolHandlers;

namespace Oraide.LanguageServer.LanguageServerProtocolHandlers
{
	public class ShutdownHandler : BaseRpcMessageHandler
	{
		public ShutdownHandler(SymbolCache symbolCache)
			: base(symbolCache) { }

		[OraideCustomJsonRpcMethodTag(Methods.ShutdownName)]
		public JToken ShutdownName()
		{
			lock (LockObject)
			{
				try
				{
					if (trace)
					{
						System.Console.Error.WriteLine("<-- Shutdown");
					}
				}
				catch (Exception)
				{
				}

				return null;
			}
		}

		[OraideCustomJsonRpcMethodTag(Methods.ExitName)]
		public void ExitName()
		{
			lock (LockObject)
			{
				try
				{
					if (trace)
					{
						System.Console.Error.WriteLine("<-- Exit");
					}

					// TODO:
					// Exit();
				}
				catch (Exception)
				{
				}
			}
		}
	}
}
