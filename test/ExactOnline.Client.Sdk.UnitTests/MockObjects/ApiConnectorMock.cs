using System;
using ExactOnline.Client.Sdk.Interfaces;

namespace ExactOnline.Client.Sdk.UnitTests.MockObjects
{

	/// <summary>
	/// Simulates APIConnector class
	/// </summary>
	public class ApiConnectorMock : IApiConnector
	{
		public String Data { get; set; }

		public string DoCleanRequest(string uri)
		{
			return "";
		}

		public string DoCleanRequest(string uri, string oDataQuery)
		{
			return "";
		}

		public int Count()
		{
			return 0;
		}

		#region IAPIConnector Members

		public string DoGetRequest(string endpoint, string parameters)
		{
			return string.Empty;
		}

		public string DoPostRequest(string endpoint, string postdata)
		{
			Data = postdata;
			return string.Empty;
		}

		public string DoPutRequest(string endpoint, string putData)
		{
			return string.Empty;
		}

		public string DoDeleteRequest(string endpoint)
		{
			return string.Empty;
		}

		public int GetCurrentDivision(string website)
		{
			return -1;
		}

		#endregion
	}
}