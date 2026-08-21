using System;

namespace FlexiPortal.Mobile.Helpers
{
	public static class ApiConfig
	{
		
		public const string BaseUrl = "https://mobtrack-api.flexihrmcloud.com/"; 

		public static HttpClient CreateClient()
		{
			var handler = new HttpClientHandler
			{
				ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
				{
					
					return true;
				}
			};

			return new HttpClient(handler)
			{
				BaseAddress = new Uri(BaseUrl),
				Timeout = TimeSpan.FromSeconds(30)
			};
		}
	}
}