namespace Noggin.NetCoreAuth.Model;

public class AccessToken
{
    public AccessToken(string publicToken, string secretToken)
    {
        PublicToken = publicToken;
        SecretToken = secretToken;
    }

    public string PublicToken { get; }
    public string SecretToken { get; }
}
