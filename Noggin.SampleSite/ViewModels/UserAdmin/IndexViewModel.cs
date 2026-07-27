using System.Collections.Generic;

namespace Noggin.SampleSite.ViewModels.UserAdmin;

public class IndexViewModel
{
	public required UserViewModel User { get; set; }
	public required IList<UserViewModel> AllUsers { get; set; }
	public required IEnumerable<string> UnlinkedProviders { get; set; }
}