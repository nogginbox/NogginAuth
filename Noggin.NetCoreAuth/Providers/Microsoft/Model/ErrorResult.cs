using System.Text.Json.Serialization;

namespace Noggin.NetCoreAuth.Providers.Microsoft.Model;

internal class ErrorResult
{

    public string Error { get; init; }

    [JsonPropertyName("error_description")]
    public string ErrorDescription { get; init; }

    [JsonPropertyName("error_codes")]
    public int[] ErrorCodes { get; init; }

    public string Timestamp { get; init; }

    [JsonPropertyName("trace_id")]
    public string TraceId { get; init; }

    [JsonPropertyName("correlation_id")]
    public string CorrelationId { get; init; }
}
