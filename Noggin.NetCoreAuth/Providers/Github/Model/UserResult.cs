namespace Noggin.NetCoreAuth.Providers.GitHub.Model
{
    internal class UserResult
    {
        public long Id { get; init; }

        public string AvatarUrl { get; init; }

        public string Bio { get; init; }

        public string Blog { get; init; }

        public string Company { get; init; }

        public string Email { get; init; }

        public string HtmlUrl { get; init; }

        public string Location { get; init; }

        public string Login { get; init; }

        public string Name { get; init; }

        #region Error responses (null if everything is fine)

        /// <summary>
        /// Error message (null if everything is fine)
        /// </summary>
        public string Message { get; init; }
        
        #endregion
    }
}