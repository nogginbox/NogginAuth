namespace Noggin.NetCoreAuth.Providers.Facebook.Model
{
    internal class AccessTokenResult
	{
		public string AccessToken { get; set; }

        public string TokenType { get; set; }

        public int ExpiresIn { get; set; }

        public ErrorResult Error { get; set; }
	}
}