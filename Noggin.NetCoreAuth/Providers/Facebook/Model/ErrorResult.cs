namespace Noggin.NetCoreAuth.Providers.Facebook.Model
{
    internal class ErrorResult
    {
        public int Code { get; init; }

        public string FbtraceId { get; init; }

        public string Message { get; init; }

        public string Type { get; init; }
        
    }
}