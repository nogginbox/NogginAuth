using System.Text.Json.Serialization;

namespace Noggin.NetCoreAuth.Providers.Facebook.Model;

internal class MeResult
{
	public long Id { get; init; }

    public string Name { get; init; }

    [JsonPropertyName("first_name")]
    public string FirstName { get; init; }

    [JsonPropertyName("last_name")]
    public string LastName { get; init; }

    public string Email { get; init; }

    public Error Error { get; init; }
}