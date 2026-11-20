using Noggin.NetCoreAuth.Exceptions;
using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace Noggin.NetCoreAuth.Providers;

/// <summary>
/// Creates and checks the OAuth "state" parameter.
/// </summary>
/// <remarks>
/// A random state value is stored in the user's session and sent to the provider when login starts.
/// The provider sends it back on the callback. If it doesn't match the session value, the callback
/// was not started by this browser (e.g. a login CSRF attack) and must be rejected.
/// Reference: https://datatracker.ietf.org/doc/html/rfc6749#section-10.12
/// </remarks>
internal static class OAuthState
{
    /// <summary>Session key that holds the state between the redirect and the callback</summary>
    internal const string SessionKey = "NogginNetCoreAuth.State";

    /// <summary>Query string parameter name used by OAuth providers</summary>
    internal const string QueryParameterName = "state";

    /// <summary>
    /// Create an unguessable state value (256 random bits, base64url encoded so it's URL safe)
    /// </summary>
    internal static string Create() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));

    /// <summary>
    /// Throws if the state returned by the provider doesn't match the one stored in session
    /// </summary>
    internal static void Validate(string? expectedState, string? returnedState)
    {
        if (string.IsNullOrEmpty(expectedState))
        {
            throw new NogginNetCoreAuthException("No login was started in this session. The session may have expired, or the login was started in a different browser.");
        }

        if (string.IsNullOrEmpty(returnedState))
        {
            throw new NogginNetCoreAuthException("No state was returned by the login provider.");
        }

        var isMatch = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expectedState),
            Encoding.UTF8.GetBytes(returnedState));

        if (!isMatch)
        {
            throw new NogginNetCoreAuthException("The state returned by the login provider does not match the one sent. The login may not have been started by this browser.");
        }
    }
}
