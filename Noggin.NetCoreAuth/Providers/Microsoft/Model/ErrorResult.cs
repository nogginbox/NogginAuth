using Newtonsoft.Json;

namespace Noggin.NetCoreAuth.Providers.Microsoft.Model;

internal class ErrorResult
{
    
    public string Error { get; init; }

    [JsonProperty(PropertyName = "error_description")]
    public string ErrorDescription { get; init; }

    [JsonProperty(PropertyName = "error_codes")]
    public int[] ErrorCodes { get; init; }

    public string Timestamp { get; init; }

    [JsonProperty(PropertyName = "trace_id")]
    public string TraceId { get; init; }
    
    [JsonProperty(PropertyName = "correlation_id")]
    public string CorrelationId { get; init; }
}
