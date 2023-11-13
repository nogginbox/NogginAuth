using Newtonsoft.Json;

namespace Noggin.NetCoreAuth.Providers.Google.Model;

internal class UserInfoResult
{
	public string Sub { get; set; }
	
	public string Email { get; set; }

    [JsonProperty(PropertyName = "email_verified")]
    public bool EmailVerified { get; set; }
    
	[JsonProperty(PropertyName = "family_name")]
    public string FamilyName { get; set; }

    [JsonProperty(PropertyName = "given_name")]
    public string GivenName { get; set; }
	
	public string Name { get; set; }
	
	public string Hd { get; set; }
	
	public string Picture { get; set; }

}