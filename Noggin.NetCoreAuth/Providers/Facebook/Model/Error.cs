namespace Noggin.NetCoreAuth.Providers.Facebook.Model;

internal class Error
{
    public int Code { get; init; }

    public string FbtraceId { get; init; }

    public string Message { get; init; }

    public string Type { get; init; }
    
}