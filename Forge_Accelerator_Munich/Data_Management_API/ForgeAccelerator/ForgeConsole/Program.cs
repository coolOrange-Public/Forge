using System;
using System.Diagnostics;
using System.Linq;
using Forge.Autocad_IO;
using Forge.Common;


namespace ForgeConsole
{
	class Program
	{
		static void Main(string[] args)
		{
			const string clientId = "Fmn1lCKJC96ZTqZIN84GZYcLkfVcjCu8";
			const string clientSecret = "tGBZepNdnvCaxH05";


			var dataMng = new AcadIoCommunication();

			var authenticationUrl = dataMng.GetAuthenticationUrl(clientId);
			Process.Start(authenticationUrl);

			var code = GetAuthorizationCode();
			dataMng.Connect(clientId, clientSecret,code);

			var hub = dataMng.GetHubs().FirstOrDefault();
			if (hub == null)
			{
				Console.Write("No Hub found!");
				return;
			}

			var project = dataMng.GetProjects(hub.Id).FirstOrDefault();
			if (project == null)
			{
				Console.Write("No Project found!");
				return;
			}

			var rootFolder = project.Relationships.RootFolder;
			if (rootFolder == null)
			{
				Console.Write("No RootFolder found!");
				return;
			}

			var rootFolderId = rootFolder.Data.Id;
			Console.ReadLine();
		}

		static string GetAuthorizationCode()
		{
			Console.Write("Please enter the Authorization Code: ");
			var code = Console.ReadLine();
			return string.IsNullOrEmpty(code) ? GetAuthorizationCode() : code;
		}
	}
}
