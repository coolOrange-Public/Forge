using AutofacContrib.NSubstitute;
using NUnit.Framework;

namespace Forge.Common.Tests
{
	public class ContainerBasedTests
	{
		internal AutoSubstitute Container;

		[SetUp]
		public void SetupContainer()
		{
			Container = new AutoSubstitute();
		}
	}
}