using System.Text.Json.Serialization;

namespace Noggin.NetCoreAuth.Providers.Google.Model;

public class GoogleError
{
	public string Error { get; init; }

	[JsonPropertyName("error_description")]
	public string Description { get; init; }
}