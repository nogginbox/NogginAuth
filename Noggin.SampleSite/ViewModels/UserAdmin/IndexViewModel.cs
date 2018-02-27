using System.Collections.Generic;

namespace Noggin.SampleSite.ViewModels.UserAdmin
{
    public class IndexViewModel
    {
		public UserViewModel User { get; set; }
		public IList<UserViewModel> AllUsers { get; set; }
		public IEnumerable<string> UnlinkedProviders { get; internal set; }
	}
}