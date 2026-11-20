using System.Text.Json.Serialization;

namespace Noggin.NetCoreAuth.Providers.Facebook.Model;

internal class Error
{
    public int Code { get; init; }

    /// <summary>
    /// Facebook's trace id for the failed request, used when reporting problems to Facebook.
    /// </summary>
    [JsonPropertyName("fbtrace_id")]
    public string? FbtraceId { get; init; }

    public string Message { get; init; }

    public string Type { get; init; }
    
}