using ExactOnline.Client.Sdk.Controllers;
using ExactOnline.Client.Sdk.TestContext;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExactOnline.Client.Sdk.UserAcceptanceTests.UserLevel
{
	[TestClass]
	public class GetCurrentMe
	{
		[TestMethod, Ignore()]
		[TestCategory("User Acceptance Tests")]
		public void ExactClient_GetCurrentMe_Succeeds()
		{
			var toc = new TestObjectsCreator();
			var client = new ExactOnlineClient(toc.EndPoint, toc.GetOAuthAuthenticationToken);
			// Assert.IsNotNull(client.CurrentMe());
		}
	}
}
