namespace Noggin.SampleSite.Data
{
    public class UserAuthAccount
    {
        public int Id { get; set; }
        public string Provider { get; internal set; }
        public string UserName { get; internal set; }
    }
}