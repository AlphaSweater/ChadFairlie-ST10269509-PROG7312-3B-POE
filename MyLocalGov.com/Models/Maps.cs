using System.Text.Json.Serialization;

namespace MyLocalGov.com.Models.Maps;

// Places - Autocomplete
public sealed class AutocompleteRequestDto
{
	public string Input { get; set; } = "";
	public string? SessionToken { get; set; }
	public double? Lat { get; set; }
	public double? Lng { get; set; }
	public int? RadiusMeters { get; set; }
	public string? LanguageCode { get; set; }
	public string? RegionCode { get; set; }
}

public sealed class AutocompletePredictionDto
{
	public string PlaceId { get; set; } = "";
	public string Text { get; set; } = "";
	public string? SecondaryText { get; set; }
	public string[] Types { get; set; } = [];
	public int? DistanceMeters { get; set; }
}

public sealed class AutocompleteResponseDto
{
	public string SessionToken { get; set; } = "";
	public List<AutocompletePredictionDto> Predictions { get; set; } = new();
}

// Places - Details
public sealed class PlaceDetailsResponseDto
{
	public string PlaceId { get; set; } = "";
	public string? FormattedAddress { get; set; }
	public double? Lat { get; set; }
	public double? Lng { get; set; }

	// Normalized components for your form
	public string? StreetNumber { get; set; }

	public string? Route { get; set; }
	public string? Street => string.Join(" ", new[] { StreetNumber, Route }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();

	public string? Suburb { get; set; }
	public string? City { get; set; }
	public string? PostalCode { get; set; }

	public string[] Types { get; set; } = [];
}

// Address Validation - request (compatible with your JS payload)
public sealed class ValidateAddressDto
{
	public PostalAddressDto Address { get; set; } = new();
	public string? PreviousResponseId { get; set; }
	public bool EnableUspsCass { get; set; }
}

public sealed class PostalAddressDto
{
	public string RegionCode { get; set; } = "ZA";
	public string? LanguageCode { get; set; }
	public List<string>? AddressLines { get; set; }
	public string? Locality { get; set; }
	public string? PostalCode { get; set; }
}

// Address Validation - shaped response for frontend scoring
public sealed class AddressValidationShapedResponse
{
	// Put verdict at the top level so map-helper.js can read validation.verdict
	[JsonPropertyName("verdict")]
	public object? Verdict { get; set; }

	[JsonPropertyName("address")]
	public object? Address { get; set; }

	[JsonPropertyName("geocode")]
	public object? Geocode { get; set; }

	// Raw typed result if you ever need it
	[JsonPropertyName("raw")]
	public object? Raw { get; set; }
}