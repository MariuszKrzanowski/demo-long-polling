using System.Text.Json.Serialization;

namespace MrMatrix.Net.LongPolling.WebServer.Models;

public record EventPayload
{
    [JsonPropertyName("gsn")]
    public required int GlobalSequenceNumber { get; init; }

    public required string Payload { get; init; }
}