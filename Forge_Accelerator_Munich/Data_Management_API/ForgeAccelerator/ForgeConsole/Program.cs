using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Forge.Autocad_IO;
using System.IO.Compression;


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
			var compressedFile = CompresseFile(file);

			dynamic storageLocation = comunication.CreateStorageLocation(project.Id, rootFolderId, file.Name);
			var uploadedFile = comunication.UploadFileToBucket("wip.dm.prod", (storageLocation.id.Value as string).Split('/').Last(), compressedFile);
			var objectId = uploadedFile["objectId"] as string;
			var item = comunication.CreateItem(project.Id, rootFolderId, objectId, file.Name);

			CreateThumbnailForItem(comunication, item, file.FullName + ".png");
			Console.ReadLine();
		}

		static void CreateThumbnailForItem(AcadIoCommunication comunication, dynamic item, string fileName)
		{
			Console.Write("Creating Thumbnail for file...");
			var stream = comunication.GetThumbnail(new Uri(item.included[0].relationships.thumbnails.meta.link.href.ToString()));
			using (var fileStream = File.Create(fileName))
			{
				stream.Seek(0, SeekOrigin.Begin);
				stream.CopyTo(fileStream);
			}
		}

		static string GetAuthorizationCode()
		{
			Console.Write("Please enter the Authorization Code: ");
			var code = Console.ReadLine();
			return string.IsNullOrEmpty(code) ? GetAuthorizationCode() : code;
		}

		static FileInfo GetFileName()
		{
			Console.Write("Please enter your full file path: ");
			var code = Console.ReadLine();
			return string.IsNullOrEmpty(code) ? GetFileName() : new FileInfo(code);
		}
	}
}
