namespace Noggin.NetCoreAuth.Providers.Google.Model
{
    internal class AccessTokenResult
    {
		public string AccessToken { get; set; }
		public string TokenType { get; set; }
		public long ExpiresIn { get; set; }
		public string IdToken { get; set; }
	}
}