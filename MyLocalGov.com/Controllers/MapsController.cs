using Microsoft.AspNetCore.Mvc;
using MyLocalGov.com.Services.Interfaces;
using MyLocalGov.com.Services.Models.Maps;

namespace MyLocalGov.com.Controllers
{
	[ApiController]
	[Route("api/address")]
	public class MapsController : ControllerBase
	{
		private readonly IMapsService _mapsService;

		public MapsController(IMapsService mapsService)
		{
			_mapsService = mapsService;
		}

		public record ReverseGeocodeRequest(double Lat, double Lng);
		public record PlaceDetailsRequest(string PlaceId);
		public record GeocodeTextRequest(string Query);
		public record AutocompleteRequest(string Query);

		[HttpPost("reverse-geocode")]
		public async Task<ActionResult<MapResultDto>> ReverseGeocode([FromBody] ReverseGeocodeRequest req, CancellationToken ct)
		{
			var result = await _mapsService.ReverseGeocodeAsync(req.Lat, req.Lng, ct);
			return Ok(result);
		}

		[HttpPost("place-details")]
		public async Task<ActionResult<MapResultDto>> PlaceDetails([FromBody] PlaceDetailsRequest req, CancellationToken ct)
		{
			if (string.IsNullOrWhiteSpace(req.PlaceId)) return BadRequest("PlaceId is required.");
			var result = await _mapsService.PlaceDetailsAsync(req.PlaceId, ct);
			return Ok(result);
		}

		[HttpPost("geocode-text")]
		public async Task<ActionResult<MapResultDto>> GeocodeText([FromBody] GeocodeTextRequest req, CancellationToken ct)
		{
			if (string.IsNullOrWhiteSpace(req.Query)) return BadRequest("Query is required.");
			var result = await _mapsService.GeocodeTextAsync(req.Query, ct);
			return Ok(result);
		}

		[HttpPost("autocomplete")]
		public async Task<ActionResult<List<PlaceSuggestionDto>>> Autocomplete([FromBody] AutocompleteRequest req, CancellationToken ct)
		{
			if (string.IsNullOrWhiteSpace(req.Query)) return Ok(new List<PlaceSuggestionDto>());
			var suggestions = await _mapsService.AutocompleteAsync(req.Query, ct);
			return Ok(suggestions);
		}
	}
}
