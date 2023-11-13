using Newtonsoft.Json;

namespace Noggin.NetCoreAuth.Providers.Microsoft.Model;

internal class UserResult
{
    public string Id { get; init; }

    [JsonProperty(PropertyName = "@odata.context")]
    public string odatacontext { get; init; }
    
    public string UserPrincipalName { get; init; }
    
    public string DisplayName { get; init; }

    public string Surname { get; init; }

    public string GivenName { get; init; }

    public string PreferredLanguage { get; init; }

    public string Mail { get; init; }

    public object MobilePhone { get; init; }
}