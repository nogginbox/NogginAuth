using System.ComponentModel.DataAnnotations;

namespace Noggin.SampleSite.Data
{
    public class UserAuthAccount
    {
        [StringLength(64)]
        public string Id { get; set; }

        [StringLength(32)]
        public string Provider { get; internal set; }

        [StringLength(32)]
        public string UserName { get; internal set; }
    }
}