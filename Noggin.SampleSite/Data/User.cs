

using System.Collections.Generic;

namespace Noggin.SampleSite.Data
{
    public class User
    {
        public User()
        {
            AuthAccounts = new List<UserAuthAccount>();
        }

        public int Id { get; set; }
        public string Name { get; set; }

        public IList<UserAuthAccount> AuthAccounts { get;set;}
    }
}