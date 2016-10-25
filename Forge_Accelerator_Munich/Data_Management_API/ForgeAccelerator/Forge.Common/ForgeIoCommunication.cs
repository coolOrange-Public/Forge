using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using AIO.ACES.Models;
using AIO.Operations;
using Flurl.Http;
using Flurl.Http.Content;
using Newtonsoft.Json;

namespace Forge.Common
{
	public class Relationship
	{
		public Entity Data;
	}

	public class Entity
	{
		public string Id;
		public Relationships Relationships;
	}

	public class Relationships
	{
		public Relationship RootFolder;
		public Relationship Hub;
	}

	public struct WorkItemDefinition
	{
		public string ActivityId { get; set; }

		public IEnumerable<Argument> InputArguments { get; set; }
		public IEnumerable<Argument> OutputArguments { get; set; }

		public WorkItemDefinition(string activityId) : this()
		{
			ActivityId = activityId;

			OutputArguments = new[]
			{
					new Argument
					{
						Name = "Result",
						Resource = null,
						StorageProvider = StorageProvider.Generic,
						HttpVerb = HttpVerbType.POST
					}
				};
		}

		public void AddOverridenParameters(Dictionary<string, object> parameters)
		{
			InputArguments = new List<Argument>(InputArguments ?? Enumerable.Empty<Argument>())
				{
					new Argument
					{
						Name = "ChangeParameters",
						Resource = "data:application/json," + new JavaScriptSerializer().Serialize(parameters),
						StorageProvider = StorageProvider.Generic,
						ResourceKind = ResourceKind.Embedded
					}
				};
		}
	}

	public struct WorkItemResult
	{
		public Uri Result { get; set; }
		public Uri Report { get; set; }
	}

	public enum PolicyKey
	{
		Transient, Persistent, Temporary
	}

	public struct ActivityDefinition
	{
		public string Id { get; set; }
		public string Script { get; set; }
		public IEnumerable<string> AppPackages { get; set; }
		public string RequiredEngineVersion { get; set; }

		public IEnumerable<Parameter> InputParameters { get; set; }
		public IEnumerable<Parameter> OutputParameters { get; set; }

		public ActivityDefinition(string id) : this()
		{
			Id = id;
			Script = "";
			RequiredEngineVersion = "20.0";
		}
	}

	public interface IForgeIoCommunication
	{
		Container Container { get; }
		int Timeout { get; }
		Authentication Authentication { get; }

		void Connect(string clientid, string clientsecret);
		WorkItemResult SubmitWorkItem(WorkItemDefinition workItem);

		Dictionary<string, object> GetBucket(string bucketName);
		Dictionary<string, object> CreateBucket(string bucketName, PolicyKey policyKey);
		void DeleteFileFromBucket(Uri bucketFileLocation);
		
		Dictionary<string, string> GetActivityDetails();
		Dictionary<string, string> GetAppPackageDetails();
		void DeleteAppPackage(string packageId);

		bool CreateActivity(ActivityDefinition activity);
		bool DeleteActivity(string activityId);
		bool UpdateActivity(ActivityDefinition activity);
		bool CreateAppPackageFromBundle(string packageId, string bundleFolderPath);
		bool CreateAppPackageFromZip(string packageId, string packageZipFilePath);
		bool DeletePackage(string packageId);
	}

	public abstract class ForgeIoCommunication : IForgeIoCommunication
	{
		string _redirectUri = "https://www.coolorange.com/*";
		private const string RelativeBucketUrl = "/oss/v2/buckets/";
		private Authentication _authentication;

		protected abstract Uri GetBaseUri();

		public Authentication Authentication {get { return _authentication; }}

		protected ForgeIoCommunication()
		{
			Timeout = 50000;
			_authentication = new Authentication(string.Empty, string.Empty);
		}

		public Container Container { get; protected set; }
		public int Timeout { get; protected set; }

		/// <summary>
		///     Does setup of Forge Application in IO.
		///     This method will need to be invoked once before any other methods of this
		///     utility class can be invoked.
		/// </summary>
		/// <param name="clientid">Forge Client ID - can be obtained from developer.autodesk.com</param>
		/// <param name="clientsecret">Forge IO Client Secret - can be obtained from developer.autodesk.com</param>
		public void Connect(string clientid, string clientsecret)
		{
			var baseUri = GetBaseUri();
			try
			{
				var clientId = clientid;
				var clientSecret = clientsecret;

				Container = new Container(baseUri);
				Container.Format.UseJson();

				using (var client = new HttpClient())
				{
					var values = new List<KeyValuePair<string, string>>();
					values.Add(new KeyValuePair<string, string>("client_id", clientId));
					values.Add(new KeyValuePair<string, string>("client_secret", clientSecret));
					values.Add(new KeyValuePair<string, string>("grant_type", "client_credentials"));
					values.Add(new KeyValuePair<string, string>("scope", "data:read data:write bucket:create bucket:read code:all"));
					var requestContent = new FormUrlEncodedContent(values);
					var response =
						client.PostAsync(string.Format("{0}://{1}/authentication/v1/authenticate", baseUri.Scheme, baseUri.Host), requestContent).Result;
					var responseContent = response.Content.ReadAsStringAsync().Result;
					if(!response.IsSuccessStatusCode)
						throw new Exception(responseContent);
					var resValues = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseContent);
					_authentication.Type = resValues["token_type"];
					_authentication.AccessToken = resValues["access_token"];
					Timeout = int.Parse(resValues["expires_in"]);
					if (!string.IsNullOrEmpty(Authentication.AccessToken))
					{
						Container.SendingRequest2 += (sender, e) => e.RequestMessage.SetHeader("Authorization", Authentication.ToString());
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(string.Format("Error while connecting to '{0}'", baseUri), ex);
				Container = null;
				throw;
			}
		}

		//ThreeLegged OAuth2 
		public void Connect(string clientid, string clientsecret, string code)
		{
			var baseUri = GetBaseUri();
			try
			{
				var tokenUrl = string.Format("{0}://{1}//authentication/v1/gettoken", baseUri.Scheme, baseUri.Host);

				Container = new Container(baseUri);
				Container.Format.UseJson();

				using (var client = new HttpClient())
				{
					var values = new List<KeyValuePair<string, string>>();
					values.Add(new KeyValuePair<string, string>("client_id", clientid));
					values.Add(new KeyValuePair<string, string>("client_secret", clientsecret));
					values.Add(new KeyValuePair<string, string>("code", code));
					values.Add(new KeyValuePair<string, string>("grant_type", "authorization_code"));
					values.Add(new KeyValuePair<string, string>("redirect_uri", _redirectUri));
					values.Add(new KeyValuePair<string, string>("content-type", "application/x-www-form-urlencoded"));

					var requestContent = new FormUrlEncodedContent(values);
					var response = client.PostAsync(tokenUrl, requestContent).Result;
					var responseContent = response.Content.ReadAsStringAsync().Result;
					if (!response.IsSuccessStatusCode)
						throw new Exception(responseContent);
					var resValues = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseContent);
					_authentication.Type = resValues["token_type"];
					_authentication.AccessToken = resValues["access_token"];
					Timeout = int.Parse(resValues["expires_in"]);
					if (!string.IsNullOrEmpty(_authentication.AccessToken))
					{
						Container.SendingRequest2 += (sender, e) => e.RequestMessage.SetHeader("Authorization", _authentication.ToString());
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(string.Format("Error while connecting to '{0}'", baseUri.AbsoluteUri), ex);
				Container = null;
				throw;
			}
		}

		/**
	 * Get the authentication url for a 3-legged flow. Redirect the user to this url for authorizing your application.
	 * @return
	 */
		public string GetAuthenticationUrl(string clientId)
		{
			var baseUri = GetBaseUri();
			var scopes = "data:read%20data:write%20bucket:create%20bucket:read%20code:all";
			var authorizationUrl = string.Format("{0}://{1}//authentication/v1/authorize", baseUri.Scheme, baseUri.Host);
			var oauthUrl = string.Format("{0}?client_id={1}&response_type=code&redirect_uri={2}&scope={3}", authorizationUrl, clientId, _redirectUri, scopes);
			return oauthUrl;
		}

		public IEnumerable<Entity> GetHubs()
		{
			var response = CreateRequest("/project/v1/hubs").GetAsync().Result;
			var hubs = GetResponseBody(response)["data"].ToString();
			return JsonConvert.DeserializeObject<IList<Entity>>(hubs);
		}

		public IEnumerable<Entity> GetProjects(string hubId)
		{
			var response = CreateRequest(string.Format("/project/v1/hubs/{0}/projects", hubId)).GetAsync().Result;
			var hubs = GetResponseBody(response)["data"].ToString();
			return JsonConvert.DeserializeObject<IList<Entity>>(hubs);
		}

		public dynamic TranslateDerivative(string designUrn, string formatType)
		{
			var req = CreateRequest("/modelderivative/v2/designdata/job");
			var base64Urn = Convert.ToBase64String(Encoding.UTF8.GetBytes(designUrn));

			var dataAsJson = req.Settings.JsonSerializer.Serialize(new
			{
				input = new
				{
					urn = base64Urn
				},
				output = new
				{
					formats = new[]
					{
						 new { type = formatType}
					}
				}
			});

			var capturedJsonContent = new CapturedJsonContent(dataAsJson);
			capturedJsonContent.Headers.Add("x-ads-force","true");
			Console.Write("Creating DeritavieJob...");
			var responseContent = req.PostAsync(capturedJsonContent).Result.Content.ReadAsStringAsync().Result;
			dynamic response = req.Settings.JsonSerializer.Deserialize<object>(responseContent);
			if (response.result.ToString() != "success")
				throw new Exception("Failed to create Job!");

			dynamic manifest;
			do
			{
				System.Threading.Thread.Sleep(1000);
				manifest = GetDerivativeManifest(base64Urn);
			} while (manifest.progress.ToString() != "complete");

			Console.Write("DeritavieJob completed. Status : {0}", manifest.status.ToString());
			if (manifest.status.ToString() != "sucess")
				throw new Exception("Translation Failed!");
			return manifest;
		}

		public dynamic GetDerivativeManifest(string base64Urn)
		{
			var req = CreateRequest(string.Format("/modelderivative/v2/designdata/{0}/manifest", base64Urn));
			dynamic responseContent = req.GetAsync().Result;
			return req.Settings.JsonSerializer.Deserialize<object>(responseContent);
		}

		public Stream GetThumbnail(Uri uri)
		{
			return CreateRequest(uri.AbsolutePath).GetAsync().ReceiveStream().Result;
		}

		public byte[] GetThumbnail(string base64Urn)
		{
			return CreateRequest(string.Format("/modelderivative/v2/designdata/{0}/thumbnail", base64Urn)).GetAsync().ReceiveBytes().Result;
		}

		public dynamic CreateStorageLocation(string projectId, string folderId, string fileName)
		{
			var req = CreateRequest("/data/v1/projects/" + projectId + "/storage");
			var dataAsJson = req.Settings.JsonSerializer.Serialize(new
			{
				jsonapi = new { version = "1.0" },
				data = new
				{
					type = "objects",
					attributes = new
					{
						name = fileName
					},
					relationships = new
					{
						target = new
						{
							data = new
							{
								type = "folders",
								id = folderId
							}
						}
					}
				}
			});

			var capturedJsonContent = new CapturedJsonContent(dataAsJson);
			capturedJsonContent.Headers.ContentType.MediaType = "application/vnd.api+json";
			capturedJsonContent.Headers.ContentType.CharSet = null;
			var responseContent = req.SendAsync(HttpMethod.Post, (HttpContent)capturedJsonContent).Result.Content.ReadAsStringAsync().Result;
			dynamic response = req.Settings.JsonSerializer.Deserialize<object>(responseContent);
			return response.data;
		}

		public Dictionary<string, object> UploadFileToBucket(string bucketName, string objectName, FileInfo file)
		{
			if (!file.Exists)
				throw new Exception(string.Format("File for uploading does not exist: {0}", file.FullName));
			var bucketRequest = CreateRequest(string.Format("{0}{1}/objects/{2}", RelativeBucketUrl, bucketName, objectName));
			var streamContent = new StreamContent(file.OpenRead());
			var response = bucketRequest.PutAsync(streamContent).Result;
			return GetResponseBody(response);
		}


		public dynamic CreateItem(string projectId, string folderId, string objectId, string fileName)
		{
			var req = CreateRequest("/data/v1/projects/" + projectId + "/items");
			var dataAsJson = req.Settings.JsonSerializer.Serialize(new
			{
				jsonapi = new { version = "1.0" },
				data = new
				{
					type = "items",
					attributes = new
					{
						displayName = fileName,
						extension = new
						{
							type = "items:autodesk.core:File",
							version = "1.0"
						}
					},
					relationships = new
					{
						tip = new
						{
							data = new
							{
								type = "versions",
								id = "1"
							}
						},
						parent = new
						{
							data = new
							{
								type = "folders",
								id = folderId
							}
						}
					}
				},
				included = new[]
					{
						new
						{
							type = "versions",
							id ="1",
							attributes = new
							{
								name = fileName,
								extension = new
								{
									type="versions:autodesk.core:File",
									version="1.0"
								}
							},
							relationships = new
							{
								storage = new
								{
									data = new
									{
										type = "objects",
										id = objectId
									}
								}
							}
						}
					}

			});

			var capturedJsonContent = new CapturedJsonContent(dataAsJson);
			capturedJsonContent.Headers.ContentType.MediaType = "application/vnd.api+json";
			capturedJsonContent.Headers.ContentType.CharSet = null;
			var responseContent = req.SendAsync(HttpMethod.Post, capturedJsonContent).Result.Content.ReadAsStringAsync().Result;
			return req.Settings.JsonSerializer.Deserialize<object>(responseContent);
		}

		/// <summary>
		///     Creates a new WorkItem
		/// </summary>
		/// <returns>true if WorkItem was created, false otherwise</returns>
		public WorkItemResult SubmitWorkItem(WorkItemDefinition workItem )
		{
			WorkItem wi = new WorkItem
			{
				Id = "",
				Arguments = new Arguments(),
				ActivityId = workItem.ActivityId
			};
			foreach (var inputArgument in workItem.InputArguments)
				wi.Arguments.InputArguments.Add(inputArgument);

			foreach (var outputArgument in workItem.OutputArguments)
				wi.Arguments.OutputArguments.Add(outputArgument);

			Container.MergeOption = Microsoft.OData.Client.MergeOption.OverwriteChanges;
			Container.AddToWorkItems(wi);
			Container.SaveChanges();

			Console.WriteLine("Creating WorkItem: {0}", wi.Id);
			do
			{
				System.Threading.Thread.Sleep(5000);
				wi = Container.WorkItems.Where(p => p.Id == wi.Id).SingleOrDefault();
			} while (wi.Status == ExecutionStatus.Pending ||
					 wi.Status == ExecutionStatus.InProgress);

			Console.WriteLine("WorkItem completed. Status : {0}", wi.Status);
			return new WorkItemResult
			{
				Result = wi.Status == AIO.ACES.Models.ExecutionStatus.Succeeded ?
					new Uri(wi.Arguments.OutputArguments.First().Resource) : null,
				Report = new Uri(wi.StatusDetails.Report)
			};
		}

		public Dictionary<string, object> GetBucket(string bucketName)
		{
			var response = CreateRequest(string.Format("{0}{1}/details", RelativeBucketUrl, bucketName)).GetAsync().Result;
			return GetResponseBody(response);
		}

		public Dictionary<string, object> CreateBucket(string bucketName, PolicyKey policyKey)
		{
			var response = CreateRequest(RelativeBucketUrl).PostJsonAsync(new { bucketKey = bucketName, policyKey = Enum.GetName(typeof(PolicyKey), policyKey) }).Result;
			return GetResponseBody(response);
		}

		public void DeleteFileFromBucket(Uri bucketFileLocation)
		{
			var bucketRequest = CreateRequest(bucketFileLocation.PathAndQuery);
			var response = bucketRequest.DeleteAsync().Result;
			GetResponseBody(response);
		}

		Dictionary<string, object> GetResponseBody(HttpResponseMessage response)
		{
			var responseContent = response.Content.ReadAsStringAsync().Result;
			if (!response.IsSuccessStatusCode)
				throw new Exception(string.Format("Request for '{0}' failed: '{1}'", response.Headers.Location, responseContent));
			return JsonConvert.DeserializeObject<Dictionary<string, object>>(responseContent);
		}

		FlurlClient CreateRequest(string relativePath)
		{
			var baseUri = GetBaseUri();
			var bucketUri =  string.Format("{0}://{1}{2}", baseUri.Scheme, baseUri.Host, relativePath);
			return bucketUri.WithOAuthBearerToken(Authentication.AccessToken);
		}

		/// <summary>
		///     Get the activity name and script associated with the activities
		/// </summary>
		/// <returns>Key Value pair of the activity names and script associated with each activity</returns>
		public Dictionary<string, string> GetActivityDetails()
		{
			Dictionary<string, string> activityDetails = new Dictionary<string, string>();
			try
			{
				foreach (Activity act in Container.Activities)
				{
					string activityId = act.Id;
					Instruction activityInstruction = act.Instruction;
					activityDetails.Add(string.Format("{0}", activityId), act.Instruction.Script);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error while calling GetActivityDetails", ex);
			}
			return activityDetails;
		}

		/// <summary>
		///     Get the appPackage name and resource associated with the appPackages
		/// </summary>
		/// <returns>Key Value pair of the appPackage name and resource url associated with each appPackage</returns>
		public Dictionary<string, string> GetAppPackageDetails()
		{
			Dictionary<string, string> packageDetails = new Dictionary<string, string>();
			try
			{
				foreach (AppPackage appPackage in Container.AppPackages)
				{
					string packageId = appPackage.Id;
					packageDetails.Add(string.Format("{0}", packageId), appPackage.Resource);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error while calling GetAppPackageDetails", ex);
			}
			return packageDetails;
		}


		public void DeleteAppPackage(string packageId)
		{
			var appPackage = Container.AppPackages.ByKey(packageId);
			Container.DeleteObject(appPackage);
			Container.SaveChanges();
		}

		/// <summary>
		///     Creates a new activity
		/// </summary>
		/// <returns>true if activity was created, false otherwise</returns>
		public bool CreateActivity(ActivityDefinition activity)
		{
			if (string.IsNullOrEmpty(activity.Id))
				return false;

			bool created = false;
			try
			{
				foreach (Activity act1 in Container.Activities)
				{
					if (activity.Id.Equals(act1.Id))
					{
						Console.WriteLine("Activity with name {0} already exists !", act1.Id);
						return false;
					}
				}

				// Create a new activity
				var act = new Activity
				{
					Id = activity.Id,
					Version = 1,
					Instruction = new Instruction
					{
						Script = activity.Script
					},
					Parameters = new Parameters
					{
						InputParameters = activity.InputParameters != null ?  new ObservableCollection<Parameter>(activity.InputParameters) : new ObservableCollection<Parameter>(),
						OutputParameters = activity.OutputParameters != null ? new ObservableCollection<Parameter>(activity.OutputParameters) : new ObservableCollection<Parameter>()
					},
					RequiredEngineVersion = activity.RequiredEngineVersion,
					AppPackages = activity.AppPackages != null ? new ObservableCollection<string>(activity.AppPackages) : new ObservableCollection<string>()
				};
				Container.AddToActivities(act);
				Container.SaveChanges();

				created = true;
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error while calling CreateActivity", ex);
			}

			return created;
		}

		/// <summary>
		///     Removes an existing activity
		/// </summary>
		/// <param name="activityId">
		///     Unique name identifying the activity to be removed. Activity with this name must already
		///     exist.
		/// </param>
		/// <returns>true if activity was removed, false otherwise</returns>
		public bool DeleteActivity(string activityId)
		{
			bool deleted = false;

			try
			{
				foreach (Activity act1 in Container.Activities)
				{
					if (activityId.Equals(act1.Id))
					{
						Container.DeleteObject(act1);
						Container.SaveChanges();
						deleted =true;
						break;
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error while calling DeleteActivity", ex);
			}
			return deleted;
		}

		/// <summary>
		///     Updates an existing activity script
		/// </summary>
		/// <returns>true if activity was updated, false otherwise</returns>
		public bool UpdateActivity(ActivityDefinition activity)
		{
			if (string.IsNullOrEmpty(activity.Id) || string.IsNullOrEmpty(activity.Script))
				return false;

			// Identify the result file in the script.
			// The result file name can be any name of your choice, AutoCAD IO does not have a restriction on that.
			// But just to make the code generic and to have it identify the result file automatically from the script,
			// we look for anything that sounds like result.pdf, Result.dwf, RESULT.DWG etc.
			string resultLocalFileName = string.Empty;
			foreach (Match m in Regex.Matches(activity.Script, "(?i)result.[a-z][a-z][a-z]"))
			{
				resultLocalFileName = m.Value;
			}

			if (string.IsNullOrEmpty(resultLocalFileName))
			{
				// Script did not have file name like Result.pdf, Result.dwg ....
				Console.WriteLine("Could not identify the result output file in the provided script ! Please use result.* as the output of the script.");
				return false;
			}

			bool activityUpdated = false;

			try
			{
				foreach (Activity act1 in Container.Activities)
				{
					if (activity.Id.Equals(act1.Id))
					{
						var ins = new Instruction();
						ins.Script = activity.Script;
						act1.Instruction = ins;
						Container.UpdateObject(act1);
						Container.SaveChanges();
						activityUpdated = true;
						break;
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error while calling UpdateActivity", ex);
			}
			return activityUpdated;
		}

		/// <summary>
		///     Creates a new AppPackage
		/// </summary>
		/// <param name="packageId">
		///     Unique name identifying the appPackage to be created. AppPackage must not already exist with
		///     the same name.
		/// </param>
		/// <param name="bundleFolderPath">
		///     Local folder path to the autoloader bundle. This path must contain the
		///     PackageContents.xml
		/// </param>
		/// <returns>true if appPackage was created, false otherwise</returns>
		public bool CreateAppPackageFromBundle(string packageId, string bundleFolderPath)
		{
			bool appPackageCreated = false;

			if (string.IsNullOrEmpty(packageId))
				return appPackageCreated;

			// Check if the selected folder contains "PackageContents.xml" as a check for 
			// a valid bundle folder that we can zip and upload
			if (File.Exists(Path.Combine(bundleFolderPath, "PackageContents.xml")) == false)
			{
				Console.WriteLine("{0} is not a bundle folder. Please select a valid bundle.", bundleFolderPath);
				return appPackageCreated;
			}

			string bundleName = Path.GetFileName(bundleFolderPath);

			string tempPath = Path.GetTempPath();
			string packageZipFilePath = Path.Combine(tempPath, string.Format("{0}.zip", bundleName));
			if (File.Exists(packageZipFilePath))
			{
				// Delete existing zip file if any from the temp folder
				File.Delete(packageZipFilePath);
			}

			System.IO.Compression.ZipFile.CreateFromDirectory(bundleFolderPath, packageZipFilePath,
				System.IO.Compression.CompressionLevel.Optimal, true);

			if (File.Exists(packageZipFilePath))
			{
				// Zip was created. Create a App Package using it.
				if (CreateAppPackageFromZip(packageId, packageZipFilePath))
				{
					// App Package created ok
					appPackageCreated = true;
					Console.WriteLine("Created new app package.");
				}
				else
				{
					Console.WriteLine("Could not create new app package.");
					appPackageCreated = false;
				}
			}

			return appPackageCreated;
		}

		/// <summary>
		///     Creates a new AppPackage
		/// </summary>
		/// <param name="packageId">
		///     Unique name identifying the appPackage to be created. AppPackage must not already exist with
		///     the same name.
		/// </param>
		/// <param name="packageZipFilePath">Local path to the autoloader bundle after it has been zipped.</param>
		/// <returns>true if appPackage was created, false otherwise</returns>
		public bool CreateAppPackageFromZip(string packageId, string packageZipFilePath)
		{
			if (string.IsNullOrEmpty(packageId) || !File.Exists(packageZipFilePath))
				return false;

			AppPackage appPackage = null;

			foreach (AppPackage pack in Container.AppPackages)
			{
				if (pack.Id.Equals(packageId))
				{
					appPackage = pack;
					break;
				}
			}

			if (appPackage == null)
			{
				try
				{
					// First step -- query for the url to upload the AppPackage file
					UriBuilder builder = new UriBuilder(Container.BaseUri);
					builder.Path += "AppPackages/Operations.GetUploadUrl";
					var url = Container.Execute<string>(builder.Uri, "GET", true, null).First();

					// Second step -- upload AppPackage file
					if (GeneralUtils.UploadObject(url, packageZipFilePath))
					{
						// third step -- after upload, create the AppPackage
						appPackage = new AppPackage
						{
							Id = packageId,
							Version = 1,
							RequiredEngineVersion = "20.0",
							Resource = url
						};
						Container.AddToAppPackages(appPackage);
						Container.SaveChanges();
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine("Error while calling CreateAppPackageFromZip", ex);
				}
			}

			return (appPackage != null);
		}

		/// <summary>
		///     Removes an existing appPackage
		/// </summary>
		/// <param name="activityId">
		///     Unique name identifying the appPackage to be removed. appPackage with this name must already
		///     exist.
		/// </param>
		/// <returns>true if appPackage was removed, false otherwise</returns>
		public bool DeletePackage(string packageId)
		{
			if (string.IsNullOrEmpty(packageId))
				return false;

			bool deleted = false;
			try
			{
				foreach (AppPackage pack in Container.AppPackages)
				{
					if (packageId.Equals(pack.Id))
					{
						UriBuilder builder = new UriBuilder(Container.BaseUri);
						builder.Path += string.Format("AppPackages('{0}')", packageId);
						HttpWebRequest httpRequest = HttpWebRequest.Create(builder.Uri) as HttpWebRequest;
						httpRequest.Method = "DELETE";
						httpRequest.Headers.Add("Authorization", Authentication.ToString());
						HttpWebResponse response = httpRequest.GetResponse() as HttpWebResponse;
						//When Delete succeeds, it returns “204 No Content”. Else, you will get other error status.
						deleted = (response.StatusCode == HttpStatusCode.NoContent);
						break;
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error while calling DeletePackage", ex);
			}

			return deleted;
		}

	}
}