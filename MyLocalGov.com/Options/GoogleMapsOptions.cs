namespace MyLocalGov.com.Options;

public sealed class GoogleMapsOptions
{
	public string ApiKey { get; set; } = string.Empty;
	public string? RegionCode { get; set; }
	public string? LanguageCode { get; set; }
}