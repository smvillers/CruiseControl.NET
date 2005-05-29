using NMock;
using NUnit.Framework;
using ThoughtWorks.CruiseControl.Core.Reporting.Dashboard.Navigation;

namespace ThoughtWorks.CruiseControl.UnitTests.WebDashboard.Dashboard
{
	[TestFixture]
	public class DefaultCruiseUrlBuilderTest
	{
		private DynamicMock urlBuilderMock;
		private DefaultCruiseUrlBuilder cruiseUrlBuilder;
		private DefaultServerSpecifier serverSpecifier;
		private IProjectSpecifier projectSpecifier;
		private DefaultBuildSpecifier buildSpecifier;

		[SetUp]
		public void Setup()
		{
			urlBuilderMock = new DynamicMock(typeof(IUrlBuilder));
			serverSpecifier = new DefaultServerSpecifier("myserver");
			projectSpecifier = new DefaultProjectSpecifier(serverSpecifier, "myproject");
			buildSpecifier = new DefaultBuildSpecifier(projectSpecifier, "mybuild");
			cruiseUrlBuilder = new DefaultCruiseUrlBuilder((IUrlBuilder) urlBuilderMock.MockInstance);
		}

		private void VerifyAll()
		{
			urlBuilderMock.Verify();
		}

		[Test]
		public void ShouldBuildServerUrlAddingCorrectlyFormattedAction()
		{
			// Setup
			urlBuilderMock.ExpectAndReturn("BuildUrl", "myUrl", "myAction", "server=myserver");
			
			// Execute
			string url = cruiseUrlBuilder.BuildServerUrl("myAction", serverSpecifier);

			// Verify
			Assert.AreEqual("myUrl", url);
			VerifyAll();
		}

		[Test]
		public void ShouldBuildServerUrlAddingCorrectlyFormattedActionAndQueryString()
		{
			// Setup
			urlBuilderMock.ExpectAndReturn("BuildUrl", "myUrl", "myAction", "server=myserver&query1=arg1");
			
			// Execute
			string url = cruiseUrlBuilder.BuildServerUrl("myAction", serverSpecifier, "query1=arg1");

			// Verify
			Assert.AreEqual("myUrl", url);
			VerifyAll();
		}

		[Test]
		public void ShouldBuildProjectUrlAddingCorrectlyFormattedAction()
		{
			// Setup
			urlBuilderMock.ExpectAndReturn("BuildUrl", "myUrl", "myAction", "server=myserver&project=myproject");

			// Execute
			string url = cruiseUrlBuilder.BuildProjectUrl("myAction", projectSpecifier);

			// Verify
			Assert.AreEqual("myUrl", url);
			VerifyAll();
		}

		[Test]
		public void ShouldBuildBuildUrlAddingCorrectlyFormattedAction()
		{
			// Setup
			urlBuilderMock.ExpectAndReturn("BuildUrl", "myUrl", "myAction", "server=myserver&project=myproject&build=mybuild");

			// Execute
			string url = cruiseUrlBuilder.BuildBuildUrl("myAction", buildSpecifier);

			// Verify
			Assert.AreEqual("myUrl", url);
			VerifyAll();
		}

		[Test]
		public void ShouldBuildBuildUrlAddingCorrectlyFormattedActionWithSpecifiedFileName()
		{
			// Setup
			urlBuilderMock.ExpectAndReturn("BuildUrl", "myUrl", "myAction", "server=myserver&project=myproject&build=mybuild", "another.html");

			// Execute
			string url = cruiseUrlBuilder.BuildBuildUrl("myAction", buildSpecifier, "another.html");

			// Verify
			Assert.AreEqual("myUrl", url);
			VerifyAll();
		}
	}
}
