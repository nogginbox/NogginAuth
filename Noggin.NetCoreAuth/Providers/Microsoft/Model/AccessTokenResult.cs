using System.Text.Json.Serialization;

namespace Noggin.NetCoreAuth.Providers.Microsoft.Model;

internal class AccessTokenResult
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; init; }

    public string Scope { get; init; }

    [JsonPropertyName("token_type")]
    public string TokenType { get; init; }
}