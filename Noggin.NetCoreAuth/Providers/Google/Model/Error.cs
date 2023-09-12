namespace Noggin.NetCoreAuth.Providers.Google.Model
{
	public class GoogleApiError
	{
		public ErrorContent Error { get; init; }

		public class ErrorContent
		{ 
			public int Code { get; init; }
			public string Message { get; init; }
			public string Status { get; init; }
		}
		
	}


}