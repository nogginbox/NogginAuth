using Noggin.SampleSite.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace Noggin.SampleSite
{
    public static class UsefulLoginExtensions
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


		/// <summary>
		/// A helper to make extracting string values from a claim easier
		/// </summary>
		public static string FindStringClaimValue(this IEnumerable<Claim> claims, string type)
		{
			var claim = claims.FirstOrDefault(c => c.Type == type);
			return claim?.Value;
		}

		public static User FindUserWithProvider(this IQueryable<User> users, string userId, string provider)
		{
			return users.FirstOrDefault(u => u.AuthAccounts.Any(a => a.Provider == provider && a.Id == userId));
		}
	}
}
