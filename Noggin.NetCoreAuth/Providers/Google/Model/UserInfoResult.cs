using System.Text.Json.Serialization;

namespace Noggin.NetCoreAuth.Providers.Google.Model;

/// <summary>
/// Response from Google's v3 userinfo endpoint.
/// </summary>
/// <remarks>
/// Only the fields common to all providers are mapped to <see cref="Noggin.NetCoreAuth.Model.UserInformation"/>.
/// Google-specific fields (<see cref="EmailVerified"/>, <see cref="GivenName"/>, <see cref="FamilyName"/>,
/// <see cref="Hd"/>) are deliberately kept here, unmapped, so they are available if we later expose
/// provider-specific properties (e.g. as a free-form object on UserInformation).
/// </remarks>
internal class UserInfoResult
{
	public string Sub { get; set; }

	public string Email { get; set; }

	/// <summary>
	/// Whether Google has verified the user owns <see cref="Email"/>. Google-specific (other providers
	/// have no equivalent), so not currently exposed on UserInformation - see class remarks.
	/// Must stay a JSON boolean-compatible type: System.Text.Json fails the whole response on a type mismatch.
	/// </summary>
    [JsonPropertyName("email_verified")]
    public bool EmailVerified { get; set; }

	[JsonPropertyName("family_name")]
    public string FamilyName { get; set; }

    [JsonPropertyName("given_name")]
    public string GivenName { get; set; }
	
	public string Name { get; set; }
	
	public string Hd { get; set; }
	
	public string Picture { get; set; }

}