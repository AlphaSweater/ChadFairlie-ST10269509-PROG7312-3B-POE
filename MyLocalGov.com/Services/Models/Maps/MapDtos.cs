using System.Text.Json.Serialization;

namespace MyLocalGov.com.Services.Models.Maps
{
	public record LatLngDto(double Lat, double Lng);

	public class AddressPartsDto
	{
		public string? StreetNumber { get; set; }
		public string? Route { get; set; }
		public string? Street { get; set; }
		public string? Suburb { get; set; }
		public string? City { get; set; }
		public string? PostalCode { get; set; }
		public string? Formatted { get; set; }
	}

	public class MapResultDto
	{
		public double Lat { get; set; }
		public double Lng { get; set; }
		public string? FormattedAddress { get; set; }
		public AddressPartsDto Parts { get; set; } = new();
		public string? GeocodeLocationType { get; set; }
		public LatLngDto? GeocodedPoint { get; set; }
	}

	// New: simplified suggestion DTO for the client dropdown
	public class PlaceSuggestionDto
	{
		public string? PlaceId { get; set; }
		public string? Description { get; set; } // e.g., "Main text, secondary"
		public string? MainText { get; set; }
		public string? SecondaryText { get; set; }
	}
}