using Noggin.SampleSite.Data;
using System.Linq;

namespace Noggin.SampleSite.ViewModels.UserAdmin
{
    public class UserViewModel
    {
        public UserViewModel(string username, User user)
        {
            Name = username;
            AuthAccountNames = string.Join(", ", user.AuthAccounts.Select(a => a.Provider));
            NumberOfAuthAccount = user.AuthAccounts.Count;
        }

        public string Name { get; private set; }

        public string AuthAccountNames { get; private set; }

        public int NumberOfAuthAccount { get; private set; }
    }
}