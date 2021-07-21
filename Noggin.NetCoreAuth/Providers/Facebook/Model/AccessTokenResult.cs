namespace Noggin.NetCoreAuth.Providers.Facebook.Model
{
    internal class AccessTokenResult
	{
		public string AccessToken { get; init; }

        public string TokenType { get; init; }

        public int ExpiresIn { get; init; }

        public ErrorResult Error { get; init; }
	}
}