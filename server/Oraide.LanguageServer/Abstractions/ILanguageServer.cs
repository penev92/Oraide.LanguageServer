using System.Threading.Tasks;

namespace Oraide.LanguageServer.Abstractions
{
	public interface ILanguageServer
	{
		Task RunAsync();
	}
}
