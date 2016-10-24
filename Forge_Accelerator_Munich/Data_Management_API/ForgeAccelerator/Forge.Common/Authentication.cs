namespace Forge.Common
{
	public struct Authentication
	{
		public string AccessToken { get; set; }
		public string Type { get; set; }

		public Authentication(string accessToken, string type) : this()
		{
			AccessToken = accessToken;
			Type = type;
		}

		public override string ToString()
		{
			return string.Format("{0} {1}", Type, AccessToken);
		}
	}
}