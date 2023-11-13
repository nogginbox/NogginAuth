using Noggin.SampleSite.Data;
using System.Linq;

namespace Noggin.SampleSite;

public static class UsefulLoginExtensions
{
	public static User FindUserWithProvider(this IQueryable<User> users, string userId, string provider)
	{
		return users.FirstOrDefault(u => u.AuthAccounts.Any(a => a.Provider == provider && a.Id == userId));
	}
}
