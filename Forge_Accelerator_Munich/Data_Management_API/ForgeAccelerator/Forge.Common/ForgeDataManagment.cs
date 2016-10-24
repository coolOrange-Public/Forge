using System;
using System.Collections.Generic;
using System.Net.Http;
using AIO.Operations;
using Flurl.Http;
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


	public class ForgeDataManagment
	{
		Authentication _authentication;
		string _redirectUri = "https://www.coolorange.com/*";
		public Uri BaseUri = new Uri("https://developer.api.autodesk.com/");

		public Container Container { get; protected set; }
		public int Timeout { get; protected set; }

		public ForgeDataManagment()
		{
			Timeout = 50000;
			_authentication = new Authentication(string.Empty, string.Empty);
		}

		public void Connect(string clientid, string clientsecret, string code)
		{
			try
			{


				var tokenUrl = string.Format("{0}://{1}//authentication/v1/gettoken", BaseUri.Scheme, BaseUri.Host);

				Container = new Container(BaseUri);
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
				Console.WriteLine(string.Format("Error while connecting to '{0}'", BaseUri.AbsoluteUri), ex);
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
			var scopes = "data:read%20data:write%20bucket:create%20bucket:read%20code:all";
			var authorizationUrl = string.Format("{0}://{1}//authentication/v1/authorize", BaseUri.Scheme, BaseUri.Host);
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


		Dictionary<string, object> GetResponseBody(HttpResponseMessage response)
		{
			var responseContent = response.Content.ReadAsStringAsync().Result;
			if (!response.IsSuccessStatusCode)
				throw new Exception(string.Format("Request for '{0}' failed: '{1}'", response.Headers.Location, responseContent));
			return JsonConvert.DeserializeObject<Dictionary<string, object>>(responseContent);
		}

		FlurlClient CreateRequest(string relativePath)
		{
			var bucketUri = string.Format("{0}://{1}{2}", BaseUri.Scheme, BaseUri.Host, relativePath);
			return bucketUri.WithOAuthBearerToken(_authentication.AccessToken);
		}

	}
}
