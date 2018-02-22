using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace Noggin.SampleSite
{
    public static class ClaimsCollectionExtentsions
    {
		/// <summary>
		/// A helper to make extracting int values from a claim easier
		/// </summary>
		public static int? FindIntClaimValue(this IEnumerable<Claim> claims, string type)
		{
			var claim = claims.FirstOrDefault(c => c.Type == type);
			if (claim == null) return null;

			var isIntValue = Int32.TryParse(claim.Value, out int claimValue);

			return isIntValue
				? (int?)claimValue
				: null;
		}
        
    }
}
