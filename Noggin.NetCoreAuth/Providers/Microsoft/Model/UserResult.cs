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

/*
 {"@odata.context":"https://graph.microsoft.com/v1.0/$metadata#users/$entity","userPrincipalName":"richard.garside@nogginbox.co.uk","id":"990fceb1639eb15a","displayName":"Richard Garside","surname":"Garside","givenName":"Richard","preferredLanguage":"en-GB","mail":"richard.garside@nogginbox.co.uk","mobilePhone":null,"jobTitle":null,"officeLocation":null,"businessPhones":[]}
*/