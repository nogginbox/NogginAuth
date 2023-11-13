using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace Noggin.NetCoreAuth.Extensions;

public static class UsefulClaimExtensions
{
    /// <summary>
    /// A helper to make extracting int values from a claim easier
    /// </summary>
    public static int? FindIntClaimValue(this IEnumerable<Claim> claims, string type)
    {
        var claim = claims.FirstOrDefault(c => c.Type == type);
        if (claim == null) return null;

        var isIntValue = int.TryParse(claim.Value, out var claimValue);

        return isIntValue
            ? claimValue
            : null;
    }

    /// <summary>
    /// A helper to make extracting string values from a claim easier
    /// </summary>
    public static string? FindStringClaimValue(this IEnumerable<Claim> claims, string type)
    {
        var claim = claims.FirstOrDefault(c => c.Type == type);
        return claim?.Value;
    }
}
