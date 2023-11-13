using Newtonsoft.Json;

namespace Noggin.NetCoreAuth.Providers.Facebook.Model;

internal class AccessTokenResult
{
    [JsonProperty(PropertyName = "access_token")]
	public string AccessToken { get; init; }

    [JsonProperty(PropertyName = "token_type")]
    public string TokenType { get; init; }

    [JsonProperty(PropertyName = "expires_in")]
    public int ExpiresIn { get; init; }

    public Error Error { get; init; }
}