using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LspTypes;
using Newtonsoft.Json.Linq;
using StreamJsonRpc;

namespace Oraide.LanguageServer.LanguageServerProtocolHandlers
{
	public class ShutdownHandler : BaseRpcMessageHandler
	{
		[OraideCustomJsonRpcMethodTag(Methods.ShutdownName)]
		public JToken ShutdownName()
		{
			lock (_object)
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
			lock (_object)
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
