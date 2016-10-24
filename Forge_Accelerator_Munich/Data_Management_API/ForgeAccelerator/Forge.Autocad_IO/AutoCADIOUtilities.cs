using System;
using Forge.Common;

namespace Forge.Autocad_IO
{
	public class AcadIoCommunication : ForgeIoCommunication
	{
		protected override Uri GetBaseUri()
		{
			return new Uri("https://developer.api.autodesk.com/autocad.io/us-east/v2/");
		}
	}
}