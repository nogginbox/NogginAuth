namespace Noggin.NetCoreAuth.Providers.GitHub.Model;

internal class AccessTokenResult
{
    public string AccessToken { get; init; }

    public string Scope { get; init; }

    public string TokenType { get; init; }

    #region Error responses (null if everything is fine)

    public string? Error { get; init; }

    public string? ErrorDescription { get; init; }

    public string? ErrorUri { get; init; }

    #endregion
}