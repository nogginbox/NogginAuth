namespace Noggin.NetCoreAuth.Providers.Google.Model
{
	internal class Name
	{
		public string FamilyName { get; set; }
		public string GivenName { get; set; }

		public override string ToString()
		{
			return string.Format("{0} {1}", GivenName, FamilyName).Trim();
		}
	}
}
