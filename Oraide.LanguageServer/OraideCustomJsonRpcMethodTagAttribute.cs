using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oraide.LanguageServer
{
	[AttributeUsage(AttributeTargets.Method)]
	public class OraideCustomJsonRpcMethodTagAttribute : Attribute
	{
		private readonly string methodName;

		public OraideCustomJsonRpcMethodTagAttribute(string methodName)
		{
			this.methodName = methodName;
		}
	}
}
