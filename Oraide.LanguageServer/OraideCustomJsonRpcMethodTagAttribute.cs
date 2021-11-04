using System;

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
