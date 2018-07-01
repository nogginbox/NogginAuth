namespace Noggin.NetCoreAuth.Providers.Facebook.Model
{
    internal class ErrorResult
    {
        public int Code { get; set; }

        public string FbtraceId { get; set; }

        public string Message { get; set; }

        public string Type { get; set; }
        
    }
}