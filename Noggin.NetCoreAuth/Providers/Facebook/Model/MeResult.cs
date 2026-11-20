using System.Text.Json.Serialization;

namespace Noggin.NetCoreAuth.Providers.Facebook.Model;

internal class MeResult
{
	/// <summary>
	/// App-scoped user id. Facebook sends this as a numeric string; treat as opaque text.
	/// </summary>
	public string? Id { get; init; }

    public string Name { get; init; }

    [JsonPropertyName("first_name")]
    public string FirstName { get; init; }

    [JsonPropertyName("last_name")]
    public string LastName { get; init; }

    public string Email { get; init; }

    public Error Error { get; init; }
}