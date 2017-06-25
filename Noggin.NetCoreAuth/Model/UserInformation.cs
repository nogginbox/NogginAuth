
namespace Noggin.NetCoreAuth.Model
{
    public class UserInformation
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string UserName { get; set; }
        public string Picture { get; set; }
        public AccessToken AccessToken { get; set; }
        public string Locale { get; set; }
    }
}
