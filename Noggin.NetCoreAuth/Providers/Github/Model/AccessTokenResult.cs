using Newtonsoft.Json;

namespace Noggin.NetCoreAuth.Providers.GitHub.Model;

internal class AccessTokenResult
{
    [JsonProperty(PropertyName = "access_token")]
    public string AccessToken { get; init; }

    public string Scope { get; init; }

    [JsonProperty(PropertyName = "token_type")]
    public string TokenType { get; init; }


    #region Error responses (null if everything is fine)

    public string? Error { get; init; }

    [JsonProperty(PropertyName = "error_description")]
    public string? ErrorDescription { get; init; }

    [JsonProperty(PropertyName = "error_uri")]
    public string? ErrorUri { get; init; }

    #endregion
}