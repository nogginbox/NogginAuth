namespace Noggin.NetCoreAuth.Providers.GitHub.Model
{
    internal class AccessTokenResult
    {
        public string AccessToken { get; set; }

        public string Scope { get; set; }

        public string TokenType { get; set; }

        #region Error responses (null if everything is fine)

        public string Error { get; set; }

        public string ErrorDescription { get; set; }

        public string ErrorUri { get; set; }

        #endregion
    }
}