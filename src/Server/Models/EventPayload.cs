namespace MrMatrix.Net.LongPolling.WebServer.Models;

public record EventPayload
{
    public required int GlobalSequenceNumber { get; init; }

    public required string Payload { get; init; }
}