using System.Threading;
using System.Threading.Tasks;
using MyLocalGov.com.Services.Models.Maps;

namespace MyLocalGov.com.Services.Interfaces
{
	public interface IMapsService
	{
		Task<MapResultDto> ReverseGeocodeAsync(double lat, double lng, CancellationToken ct = default);
		Task<MapResultDto> PlaceDetailsAsync(string placeId, CancellationToken ct = default);
		Task<MapResultDto> GeocodeTextAsync(string query, CancellationToken ct = default);
		Task<List<PlaceSuggestionDto>> AutocompleteAsync(string query, CancellationToken ct = default);
	}
}
