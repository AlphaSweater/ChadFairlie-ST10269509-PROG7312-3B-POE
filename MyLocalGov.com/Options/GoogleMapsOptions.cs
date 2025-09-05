namespace MyLocalGov.com.Options
{
	public class GoogleMapsOptions
	{
		public string? BrowserApiKey { get; set; }
		public string? ServerApiKey { get; set; }
		public string? Region { get; set; }      // Optional default region (e.g., "ZA")
		public string? Language { get; set; }    // Optional default language (e.g., "en")
	}
}
