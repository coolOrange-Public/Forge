using System;
using System.Diagnostics;
using System.IO;
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


			var comunication = new AcadIoCommunication();

			var authenticationUrl = comunication.GetAuthenticationUrl(clientId);
			Process.Start(authenticationUrl);

			var code = GetAuthorizationCode();
			comunication.Connect(clientId, clientSecret,code);

			var hub = comunication.GetHubs().FirstOrDefault();
			if (hub == null)
			{
				Console.Write("No Hub found!");
				return;
			}

			var project = comunication.GetProjects(hub.Id).FirstOrDefault();
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

			var file = GetFileName();
			dynamic storageLocation = comunication.CreateStorageLocation(project.Id, rootFolderId, file.Name);
			var uploadedFile = comunication.UploadFileToBucket("wip.dm.prod", (storageLocation.id.Value as string).Split('/').Last(), file);
			var objectId = uploadedFile["objectId"] as string;
			var item = comunication.CreateItem(project.Id, rootFolderId, objectId, file.Name);

			Console.ReadLine();
		}

		static string GetAuthorizationCode()
		{
			Console.Write("Please enter the Authorization Code: ");
			var code = Console.ReadLine();
			return string.IsNullOrEmpty(code) ? GetAuthorizationCode() : code;
		}

		static FileInfo GetFileName()
		{
			Console.Write("Please enter your full file path:  ");
			var code = Console.ReadLine();
			return string.IsNullOrEmpty(code) ? GetFileName() : new FileInfo(code);
		}
	}
}
