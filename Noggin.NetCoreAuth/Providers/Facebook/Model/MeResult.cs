using Newtonsoft.Json;

namespace Noggin.NetCoreAuth.Providers.Facebook.Model;

internal class MeResult
{
	public long Id { get; init; }

    public string Name { get; init; }

    [JsonProperty(PropertyName = "first_name")]
    public string FirstName { get; init; }

    [JsonProperty(PropertyName = "last_name")]
    public string LastName { get; init; }

    public string Email { get; init; }

    public ErrorResult Error { get; init; }
}