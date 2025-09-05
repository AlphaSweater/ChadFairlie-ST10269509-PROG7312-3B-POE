using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using MyLocalGov.com.Options;
using MyLocalGov.com.Services.Interfaces;
using MyLocalGov.com.Services.Models.Maps;

namespace MyLocalGov.com.Services.Implementations
{
	public class MapsService : IMapsService
	{
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly GoogleMapsOptions _options;

		public MapsService(IHttpClientFactory httpClientFactory, IOptions<GoogleMapsOptions> options)
		{
			_httpClientFactory = httpClientFactory;
			_options = options.Value ?? new GoogleMapsOptions();
		}

		public async Task<MapResultDto> ReverseGeocodeAsync(double lat, double lng, CancellationToken ct = default)
		{
			var geocode = await GeocodeByLatLngAsync(lat, lng, ct);
			var parts = ExtractAddressParts(geocode);
			return new MapResultDto
			{
				Lat = lat,
				Lng = lng,
				FormattedAddress = parts.Formatted,
				Parts = parts
			};
		}

		public async Task<MapResultDto> PlaceDetailsAsync(string placeId, CancellationToken ct = default)
		{
			var doc = await PlacesGetAsync(placeId, ct);
			var root = doc.RootElement;

			// Gracefully read Places v1 coordinates
			var (lat, lng) = TryReadLatLngFromNode(root);

			var formatted = root.TryGetProperty("formattedAddress", out var fa) ? fa.GetString() : null;

			return new MapResultDto
			{
				Lat = lat,
				Lng = lng,
				FormattedAddress = formatted ?? string.Empty,
				Parts = new AddressPartsDto { Formatted = formatted }
			};
		}

		public async Task<MapResultDto> GeocodeTextAsync(string query, CancellationToken ct = default)
		{
			var doc = await PlacesSearchTextAsync(query, ct);

			// Handle API error payloads cleanly
			if (doc.RootElement.TryGetProperty("error", out var err))
			{
				var msg = err.TryGetProperty("message", out var m) ? m.GetString() : "Unknown Places API error.";
				throw new InvalidOperationException($"Places search failed: {msg}");
			}

			if (!doc.RootElement.TryGetProperty("places", out var places) || places.ValueKind != JsonValueKind.Array || places.GetArrayLength() == 0)
			{
				return new MapResultDto
				{
					Lat = 0,
					Lng = 0,
					FormattedAddress = null,
					Parts = new AddressPartsDto()
				};
			}

			var first = places[0];

			var (lat, lng) = TryReadLatLngFromNode(first);
			var formatted = first.TryGetProperty("formattedAddress", out var fa) ? fa.GetString() : null;

			return new MapResultDto
			{
				Lat = lat,
				Lng = lng,
				FormattedAddress = formatted ?? string.Empty,
				Parts = new AddressPartsDto { Formatted = formatted }
			};
		}

		private async Task<JsonDocument> GeocodeByLatLngAsync(double lat, double lng, CancellationToken ct)
		{
			var key = _options.ServerApiKey ?? _options.BrowserApiKey ?? string.Empty;
			if (string.IsNullOrWhiteSpace(key))
				throw new InvalidOperationException("Google Maps ServerApiKey is not configured.");

			var url = $"https://maps.googleapis.com/maps/api/geocode/json?latlng={lat.ToString(CultureInfo.InvariantCulture)},{lng.ToString(CultureInfo.InvariantCulture)}&key={Uri.EscapeDataString(key)}";
			if (!string.IsNullOrWhiteSpace(_options.Region)) url += $"&region={Uri.EscapeDataString(_options.Region!)}";
			if (!string.IsNullOrWhiteSpace(_options.Language)) url += $"&language={Uri.EscapeDataString(_options.Language!)}";

			var client = _httpClientFactory.CreateClient();
			using var res = await client.GetAsync(url, ct);
			res.EnsureSuccessStatusCode();
			var stream = await res.Content.ReadAsStreamAsync(ct);
			return await JsonDocument.ParseAsync(stream, cancellationToken: ct);
		}

		// Places API (New)
		private async Task<JsonDocument> PlacesSearchTextAsync(string textQuery, CancellationToken ct)
		{
			var key = _options.ServerApiKey ?? _options.BrowserApiKey ?? string.Empty;
			if (string.IsNullOrWhiteSpace(key))
				throw new InvalidOperationException("Google Maps ServerApiKey is not configured.");

			var client = _httpClientFactory.CreateClient();
			using var req = new HttpRequestMessage(HttpMethod.Post, "https://places.googleapis.com/v1/places:searchText");
			req.Headers.Add("X-Goog-Api-Key", key);
			// Request only what we need
			req.Headers.Add("X-Goog-FieldMask", "places.id,places.formattedAddress,places.location");
			var body = new
			{
				textQuery,
				languageCode = _options.Language ?? "en",
				regionCode = _options.Region
			};
			req.Content = JsonContent.Create(body);

			using var res = await client.SendAsync(req, ct);
			var content = await res.Content.ReadAsStringAsync(ct);
			if (!res.IsSuccessStatusCode)
				throw new HttpRequestException($"Places search HTTP {(int)res.StatusCode}: {content}");

			return JsonDocument.Parse(content);
		}

		private async Task<JsonDocument> PlacesGetAsync(string placeId, CancellationToken ct)
		{
			var key = _options.ServerApiKey ?? _options.BrowserApiKey ?? string.Empty;
			if (string.IsNullOrWhiteSpace(key))
				throw new InvalidOperationException("Google Maps ServerApiKey is not configured.");

			var client = _httpClientFactory.CreateClient();
			using var req = new HttpRequestMessage(HttpMethod.Get, $"https://places.googleapis.com/v1/places/{Uri.EscapeDataString(placeId)}");
			req.Headers.Add("X-Goog-Api-Key", key);
			req.Headers.Add("X-Goog-FieldMask", "id,formattedAddress,location,addressComponents");

			using var res = await client.SendAsync(req, ct);
			var content = await res.Content.ReadAsStringAsync(ct);
			if (!res.IsSuccessStatusCode)
				throw new HttpRequestException($"Places get HTTP {(int)res.StatusCode}: {content}");

			return JsonDocument.Parse(content);
		}

		private static AddressPartsDto ExtractAddressParts(JsonDocument doc)
		{
			// Places v1 list shape
			if (doc.RootElement.TryGetProperty("places", out var places) && places.ValueKind == JsonValueKind.Array && places.GetArrayLength() > 0)
			{
				var first = places[0];
				return ExtractPartsFromNewPlaces(first);
			}
			// Places v1 single place shape
			if (doc.RootElement.TryGetProperty("formattedAddress", out _))
			{
				return ExtractPartsFromNewPlaces(doc.RootElement);
			}

			// Legacy Geocoding shape
			var results = doc.RootElement.TryGetProperty("results", out var r) ? r : default;
			var firstLegacy = results.ValueKind == JsonValueKind.Array && results.GetArrayLength() > 0 ? results[0] : default;
			return ExtractPartsFromLegacy(firstLegacy);
		}

		private static AddressPartsDto ExtractPartsFromLegacy(JsonElement node)
		{
			var parts = new AddressPartsDto
			{
				Formatted = node.TryGetProperty("formatted_address", out var fmt) ? fmt.GetString() : null
			};

			if (node.ValueKind != JsonValueKind.Undefined &&
			    node.TryGetProperty("address_components", out var comps) && comps.ValueKind == JsonValueKind.Array)
			{
				foreach (var c in comps.EnumerateArray())
				{
					var types = c.GetProperty("types").EnumerateArray().Select(x => x.GetString() ?? "").ToArray();
					var longName = c.GetProperty("long_name").GetString() ?? "";

					if (types.Contains("street_number")) parts.StreetNumber = longName;
					if (types.Contains("route")) parts.Route = longName;
					if (types.Contains("sublocality_level_1") || types.Contains("sublocality") || types.Contains("neighborhood"))
						parts.Suburb ??= longName;
					if (types.Contains("locality")) parts.City = longName;
					if (parts.City == null && types.Contains("administrative_area_level_2")) parts.City = longName;
					if (types.Contains("postal_code")) parts.PostalCode = longName;
				}
			}

			parts.Street = string.Join(" ", new[] { parts.StreetNumber, parts.Route }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();
			return parts;
		}

		private static AddressPartsDto ExtractPartsFromNewPlaces(JsonElement node)
		{
			var parts = new AddressPartsDto
			{
				Formatted = node.TryGetProperty("formattedAddress", out var fmt) ? fmt.GetString() : null
			};

			if (node.TryGetProperty("addressComponents", out var comps) && comps.ValueKind == JsonValueKind.Array)
			{
				foreach (var c in comps.EnumerateArray())
				{
					var types = c.GetProperty("types").EnumerateArray().Select(x => x.GetString() ?? "").ToArray();
					var text = c.GetProperty("longText").GetString() ?? "";

					if (types.Contains("street_number")) parts.StreetNumber = text;
					if (types.Contains("route")) parts.Route = text;
					if (types.Contains("sublocality")) parts.Suburb ??= text;
					if (types.Contains("locality")) parts.City = text;
					if (parts.City == null && types.Contains("administrative_area_level_2")) parts.City = text;
					if (types.Contains("postal_code")) parts.PostalCode = text;
				}
			}

			parts.Street = string.Join(" ", new[] { parts.StreetNumber, parts.Route }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();
			return parts;
		}

		private static (double lat, double lng) TryReadLatLngFromNode(JsonElement node)
		{
			double lat = 0, lng = 0;
			if (node.TryGetProperty("location", out var locEl))
			{
				// v1 typically: { "location": { "latitude": 1.23, "longitude": 4.56 } }
				if (locEl.TryGetProperty("latitude", out var la)) lat = la.GetDouble();
				if (locEl.TryGetProperty("longitude", out var lo)) lng = lo.GetDouble();

				// Defensive: accept nested latLng shape if returned by upstream components
				if ((lat == 0 && lng == 0) && locEl.TryGetProperty("latLng", out var latLng))
				{
					if (latLng.TryGetProperty("latitude", out var la2)) lat = la2.GetDouble();
					if (latLng.TryGetProperty("longitude", out var lo2)) lng = lo2.GetDouble();
				}
			}
			return (lat, lng);
		}
	}
}
