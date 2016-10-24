using System.Runtime.InteropServices;

namespace Forge.Common
{
	public interface IEnvironment
	{
		bool IsConnectedToInternet();
	}

	public sealed class Environment : IEnvironment
	{
		public bool IsConnectedToInternet()
		{
			int description;
			return InternetGetConnectedState(out description, 0);
		}

		[DllImport("wininet.dll")]
		extern static bool InternetGetConnectedState(out int description, int reservedValue);

	}
}