using System.Text.Json.Serialization;

namespace Noggin.NetCoreAuth.Providers.Google.Model;

internal class AccessTokenResult
{
	/// <summary>
	/// Access Token
	/// </summary>
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }

	[JsonPropertyName("expires_in")]
	public long ExpiresIn { get; set; }

	[JsonPropertyName("id_token")]
	public string IdToken { get; set; }

	public string Scope { get; set; }

	/// <summary>
	/// Property Name
	/// </summary>
	[JsonPropertyName("token_type")]
	public string TokenType { get; set; }
}