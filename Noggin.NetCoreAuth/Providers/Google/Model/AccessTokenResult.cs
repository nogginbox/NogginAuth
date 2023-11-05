using Newtonsoft.Json;

namespace Noggin.NetCoreAuth.Providers.Google.Model;

internal class AccessTokenResult
{
	/// <summary>
	/// Access Token
	/// </summary>
    [JsonProperty(PropertyName = "access_token")]
    public string AccessToken { get; set; }

	[JsonProperty(PropertyName = "expires_in")]
	public long ExpiresIn { get; set; }

	[JsonProperty(PropertyName = "id_token")]
	public string IdToken { get; set; }

	public string Scope { get; set; }

	/// <summary>
	/// Property Name
	/// </summary>
	[JsonProperty(PropertyName = "token_type")]
	public string TokenType { get; set; }
}