using Newtonsoft.Json;

namespace Noggin.NetCoreAuth.Providers.Google.Model;

public class GoogleError
{
	public string Error { get; init; }

	[JsonProperty(PropertyName = "error_description")]
	public string Description { get; init; }
}