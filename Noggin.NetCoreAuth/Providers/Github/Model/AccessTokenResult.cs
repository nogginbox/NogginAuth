using System.Text.Json.Serialization;

namespace Noggin.NetCoreAuth.Providers.GitHub.Model;

internal class AccessTokenResult
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; init; }

    public string Scope { get; init; }

    [JsonPropertyName("token_type")]
    public string TokenType { get; init; }


    #region Error responses (null if everything is fine)

    public string? Error { get; init; }

    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; init; }

    [JsonPropertyName("error_uri")]
    public string? ErrorUri { get; init; }

    #endregion
}