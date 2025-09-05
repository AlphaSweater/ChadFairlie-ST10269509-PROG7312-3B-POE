using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Options;
using MyLocalGov.com.Options;
using MyLocalGov.com.Services.Interfaces;

namespace MyLocalGov.com.Services.Implementations
{
	public class AddressValidationService : IAddressValidationService
	{
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly GoogleMapsOptions _options;

		public AddressValidationService(IHttpClientFactory httpClientFactory, IOptions<GoogleMapsOptions> options)
		{
			_httpClientFactory = httpClientFactory;
			_options = options.Value ?? new GoogleMapsOptions();
		}

		public async Task<string> ValidateAsync(string requestJson, CancellationToken ct = default)
		{
			var apiKey = _options.ServerApiKey ?? _options.BrowserApiKey;
			if (string.IsNullOrWhiteSpace(apiKey))
				throw new InvalidOperationException("Google Maps ServerApiKey is not configured.");

			var url = $"https://addressvalidation.googleapis.com/v1:validateAddress?key={WebUtility.UrlEncode(apiKey)}";
			var req = new HttpRequestMessage(HttpMethod.Post, url)
			{
				Content = new StringContent(requestJson ?? "{}", Encoding.UTF8, "application/json")
			};

			var client = _httpClientFactory.CreateClient();
			var res = await client.SendAsync(req, ct);
			var body = await res.Content.ReadAsStringAsync(ct);

			if (!res.IsSuccessStatusCode)
			{
				throw new HttpRequestException($"Google Address Validation failed: {(int)res.StatusCode} {res.ReasonPhrase} - {body}");
			}

			return body; // raw JSON
		}
	}
}
