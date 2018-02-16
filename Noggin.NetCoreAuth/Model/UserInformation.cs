namespace Noggin.NetCoreAuth.Model
{
    public class UserInformation
    {
        public string Id { get; set; }

		public AccessToken AccessToken { get; set; }

		public string Email { get; set; }

		public string Gender { get; set; }

		public string Name { get; set; }

		public string Locale { get; set; }
		
		public string Picture { get; set; }

		public string UserName { get; set; }
	}
}