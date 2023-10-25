namespace Noggin.NetCoreAuth.Providers.Facebook.Model;

internal class MeResult
{
	public long Id { get; init; }

    public string Name { get; init; }

    public string FirstName { get; init; }

    public string LastName { get; init; }

    public string Link { get; init; }

    public string Username { get; init; }

    public string Email { get; init; }

    public long Timezone { get; init; }

    public string Locale { get; init; }

    public bool Verified { get; init; }

    public string UpdatedTime { get; init; }

    public string Gender { get; init; }

    public ErrorResult Error { get; init; }
}