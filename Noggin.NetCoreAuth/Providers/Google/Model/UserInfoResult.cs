using System.Text.Json.Serialization;

namespace Noggin.NetCoreAuth.Providers.Google.Model;

internal class UserInfoResult
{
	public string Sub { get; set; }

	public string Email { get; set; }

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