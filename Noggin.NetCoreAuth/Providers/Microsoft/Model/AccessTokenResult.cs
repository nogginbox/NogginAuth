using Newtonsoft.Json;

namespace Noggin.NetCoreAuth.Providers.Microsoft.Model;

internal class AccessTokenResult
{
    [JsonProperty(PropertyName = "access_token")]
    public string AccessToken { get; init; }

    public string Scope { get; init; }

    [JsonProperty(PropertyName = "token_type")]
    public string TokenType { get; init; }
}