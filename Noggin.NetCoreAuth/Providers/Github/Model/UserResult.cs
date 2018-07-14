namespace Noggin.NetCoreAuth.Providers.GitHub.Model
{
    internal class UserResult
    {
        public long Id { get; set; }

        public string AvatarUrl { get; set; }

        public string Bio { get; set; }

        public string Blog { get; set; }

        public string Company { get; set; }

        public string Email { get; set; }

        public string HtmlUrl { get; set; }

        public string Location { get; set; }

        public string Login { get; set; }

        public string Name { get; set; }

        #region Error responses (null if everything is fine)

        /// <summary>
        /// Error message (null if everything is fine)
        /// </summary>
        public string Message { get; set; }
        
        #endregion
    }
}