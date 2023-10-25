namespace Noggin.NetCoreAuth.Model;

public class UserInformation
{
    public string Id { get; init; }

		public AccessToken AccessToken { get; init; }

		public string Email { get; init; }

		public string Name { get; init; }

		public string Locale { get; init; }
		
		public string Picture { get; init; }

		public string UserName { get; init; }
	}