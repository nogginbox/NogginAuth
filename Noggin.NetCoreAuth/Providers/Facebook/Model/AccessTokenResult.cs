using System.Text.Json.Serialization;

namespace Noggin.NetCoreAuth.Providers.Facebook.Model;

internal class AccessTokenResult
{
    [JsonPropertyName("access_token")]
	public string AccessToken { get; init; }

    [JsonPropertyName("token_type")]
    public string TokenType { get; init; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; init; }

    public Error Error { get; init; }
}