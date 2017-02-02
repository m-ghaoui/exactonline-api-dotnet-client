using System;

namespace ExactOnline.Client.Sdk.Interfaces
{
	public interface IApiConnection
	{
		string Get(string parameters);

		string GetEntity(string keyname, string guid, string parameters);

		string Post(string data);

		Boolean Put(string keyName, string guid, string data);

		Boolean Delete(string keyName, string guid);

		int Count(string parameters);

		byte[] GetPdf();
	}
}
