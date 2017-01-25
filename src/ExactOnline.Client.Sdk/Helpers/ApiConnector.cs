using ExactOnline.Client.Sdk.Delegates;
using ExactOnline.Client.Sdk.Enums;
using ExactOnline.Client.Sdk.Exceptions;
using ExactOnline.Client.Sdk.Interfaces;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;

namespace ExactOnline.Client.Sdk.Helpers
{
	/// <summary>
	/// Class for doing request to REST API
	/// </summary>
	public class ApiConnector : IApiConnector
	{
		private readonly AccessTokenManagerDelegate _accessTokenDelegate;

		#region Constructor

		/// <summary>
		/// Creates new instance of ApiConnector
		/// </summary>
		/// <param name="accessTokenDelegate">Valid oAuth Access Token</param>
		public ApiConnector(AccessTokenManagerDelegate accessTokenDelegate)
		{
			if (accessTokenDelegate == null) throw new ArgumentException("accessTokenDelegate");
			_accessTokenDelegate = accessTokenDelegate;
		}

		#endregion

		#region Public methods

		/// <summary>
		/// Read Data: Perform a GET Request on the API
		/// </summary>
		/// <param name="endpoint">{URI}/{Division}/{Resource}/{Entity}</param>
		/// <param name="oDataQuery">oData Querystring</param>
		/// <returns>String with API Response in Json Format</returns>
		public string DoGetRequest(string endpoint, string oDataQuery)
		{
			if (string.IsNullOrEmpty(endpoint)) throw new ArgumentException("Cannot perform request with empty endpoint");

			var request = CreateRequest(endpoint, oDataQuery, RequestTypeEnum.GET);

			Debug.Write("GET ");
			Debug.WriteLine(request.RequestUri);

			return GetResponse(request);
		}

		/// <summary>
		/// Create Data: Perform a POST Request on the API
		/// </summary>
		/// <param name="endpoint">{URI}/{Division}/{Resource}/{Entity}</param>
		/// <param name="postdata">String containing data of new entity in Json format</param>
		/// <returns>String with API Response in Json Format</returns>
		public string DoPostRequest(string endpoint, string postdata)
		{
			if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(postdata)) throw new ArgumentException("Cannot perform request with empty endpoint or postdata");

			var request = CreateRequest(endpoint, null, RequestTypeEnum.POST);

			// Add POST data to the request
			if (!string.IsNullOrEmpty(postdata))
			{
				var bytes = Encoding.GetEncoding("utf-8").GetBytes(postdata);
				request.ContentLength = bytes.Length;

				using (var writeStream = request.GetRequestStream())
				{
					writeStream.Write(bytes, 0, bytes.Length);
				}
			}
			else
			{
				throw new BadRequestException(); // Post request needs data
			}

			Debug.Write("POST ");
			Debug.WriteLine(request.RequestUri);
			Debug.WriteLine(postdata);

			return GetResponse(request);
		}

		/// <summary>
		/// Update data: Perform a PUT Request on API
		/// </summary>
		/// <param name="endpoint">{URI}/{Division}/{Resource}/{Entity}</param>
		/// <param name="putData">String containing updated entity data in Json format</param>
		/// <returns>String with API Response in Json Format</returns>
		public string DoPutRequest(string endpoint, string putData)
		{
			if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(putData)) throw new ArgumentException("Cannot perform request with empty endpoint or putData");

			var request = CreateRequest(endpoint, null, RequestTypeEnum.PUT);

			if (!string.IsNullOrEmpty(putData))
			{
				var bytes = Encoding.GetEncoding("utf-8").GetBytes(putData);
				request.ContentLength = bytes.Length;

				using (var writeStream = request.GetRequestStream())
				{
					writeStream.Write(bytes, 0, bytes.Length);
				}
			}
			else
			{
				// Post request needs data
				throw new BadRequestException();
			}

			Debug.Write("PUT ");
			Debug.WriteLine(request.RequestUri);
			Debug.WriteLine(putData);

			return GetResponse(request);
		}

		/// <summary>
		/// Delete entity: Perform a DELETE Request on API
		/// </summary>
		/// <param name="endpoint">{URI}/{Division}/{Resource}/{Entity}</param>
		/// <returns>String with API Response in Json Format</returns>
		public string DoDeleteRequest(string endpoint)
		{
			if (string.IsNullOrEmpty(endpoint)) throw new ArgumentException("Cannot perform request with empty endpoint");

			var request = CreateRequest(endpoint, null, RequestTypeEnum.DELETE);

			Debug.Write("DELETE ");
			Debug.WriteLine(request.RequestUri);

			return GetResponse(request);
		}

		/// <summary>
		/// Request without 'Accept' Header
		/// </summary>
		/// <param name="uri"></param>
		/// <returns></returns>
		public string DoCleanRequest(string uri) // Build for doing $count function
		{
			var request = (HttpWebRequest)WebRequest.Create(uri);
			request.Method = RequestTypeEnum.GET.ToString();
			request.ContentType = "application/json";
			request.Headers.Add("Authorization", "Bearer " + _accessTokenDelegate());
			return GetResponse(request);
		}

		public int GetCurrentDivision(string website)
		{
			var url = website + "/api/v1/current/Me";
			const string oDataQuery = "$select=CurrentDivision";

			var request = CreateRequest(url, oDataQuery, RequestTypeEnum.GET);
			var response = GetResponse(request);
			var jsonObject = JsonConvert.DeserializeObject<dynamic>(response);

			return (int)jsonObject.d["results"][0]["CurrentDivision"].Value;
		}

        #endregion

        #region Private methods

	    private HttpWebRequest CreateRequest(string url, string oDataQuery, RequestTypeEnum method, string acceptContentType = "application/json")
		{
			if (!string.IsNullOrEmpty(oDataQuery))
			{
				url += "?" + oDataQuery;
			}

			var request = (HttpWebRequest)WebRequest.Create(url);
			request.Method = method.ToString();
			request.ContentType = "application/json";
		    if (!string.IsNullOrEmpty(acceptContentType))
		    {
		        request.Accept = acceptContentType;
		    }
		    request.Headers.Add("Authorization", "Bearer " + _accessTokenDelegate());

			return request;
		}

		private string GetResponse(HttpWebRequest request)
		{
			// Grab the response
			var responseValue = string.Empty;

			Debug.WriteLine("RESPONSE");

			// Get response. If this fails: Throw the correct Exception (for testability)
			try
			{
				WebResponse response = request.GetResponse();
				using (Stream responseStream = response.GetResponseStream())
				{
					if (responseStream != null)
					{
						var reader = new StreamReader(responseStream);
						responseValue = reader.ReadToEnd();
					}
				}
			}
			catch (WebException ex)
			{
				var statusCode = (((HttpWebResponse)ex.Response).StatusCode);
				Debug.WriteLine(ex.Message);

				var messageFromServer = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
				Debug.WriteLine(messageFromServer);
				Debug.WriteLine("");

				switch (statusCode)
				{
					case HttpStatusCode.BadRequest: // 400
					case HttpStatusCode.MethodNotAllowed: // 405
						throw new BadRequestException(ex.Message, ex);

					case HttpStatusCode.Unauthorized: //401
						throw new UnauthorizedException(ex.Message, ex); // 401

					case HttpStatusCode.Forbidden:
						throw new ForbiddenException(ex.Message, ex); // 403

					case HttpStatusCode.NotFound:
						throw new NotFoundException(ex.Message, ex); // 404

					case HttpStatusCode.InternalServerError: // 500
						throw new InternalServerErrorException(messageFromServer, ex);
				}

				throw;
			}

			Debug.WriteLine(responseValue);
			Debug.WriteLine("");

			return responseValue;
		}

        /// <summary>
        /// Request without 'Accept' Header, including parameters
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="oDataQuery"></param>
        /// <returns></returns>
	    public string DoCleanRequest(string uri, string oDataQuery)
	    {
            if (string.IsNullOrEmpty(uri)) throw new ArgumentException("Cannot perform request with empty endpoint");

            var request = CreateRequest(uri, oDataQuery, RequestTypeEnum.GET, null);

            Debug.Write("GET ");
            Debug.WriteLine(request.RequestUri);

            return GetResponse(request);
        }

        public byte[] DoGetFileRequest(string endpoint, string acceptContentType)
        {
            var request = CreateRequest(endpoint, null, RequestTypeEnum.GET, acceptContentType);
            return GetFileResponse(request);
        }

        private byte[] GetFileResponse(HttpWebRequest request)
        {
            // Grab the response
            byte[] responseValue = default(byte[]);

            // Get response. If this fails: Throw the correct Exception (for testability)
            try
            {
                var response = request.GetResponse();
                using (var responseStream = response.GetResponseStream())
                {
                    if (responseStream != null)
                    {
                        var reader = new StreamReader(responseStream);

                        using (var memstream = new MemoryStream())
                        {
                            var buffer = new byte[512];
                            int bytesRead;

                            while ((bytesRead = reader.BaseStream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                memstream.Write(buffer, 0, bytesRead);
                            }

                            responseValue = memstream.ToArray(); 
                        }
                    }
                }
            }
			catch (WebException ex)
			{
				var statusCode = (((HttpWebResponse)ex.Response).StatusCode);
				Debug.WriteLine(ex.Message);

				var messageFromServer = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
				Debug.WriteLine(messageFromServer);
				Debug.WriteLine("");

				switch (statusCode)
				{
					case HttpStatusCode.BadRequest: // 400
					case HttpStatusCode.MethodNotAllowed: // 405
						throw new BadRequestException(ex.Message, ex);

					case HttpStatusCode.Unauthorized: //401
						throw new UnauthorizedException(ex.Message, ex); // 401

					case HttpStatusCode.Forbidden:
						throw new ForbiddenException(ex.Message, ex); // 403

					case HttpStatusCode.NotFound:
						throw new NotFoundException(ex.Message, ex); // 404

					case HttpStatusCode.InternalServerError: // 500
						throw new InternalServerErrorException(messageFromServer, ex);
				}

				throw;
			}

            return responseValue;
        }
        #endregion

    }
}