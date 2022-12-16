using Microsoft.AspNetCore.Mvc.Formatters;
using MrMatrix.Net.LongPolling.WebServer.Models;
using System.Text.Json;

public sealed class NdJsonOutputFormatter : OutputFormatter
{
    private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public NdJsonOutputFormatter()
    {
        SupportedMediaTypes.Add("application/x-ndjson");
    }

    public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
    {
        var httpContext = context.HttpContext;

        if (context.Object is IAsyncEnumerable<EventPayload> payloads)
        {
            try
            {
                await foreach (var payload in payloads)
                {
                    await httpContext.Response.WriteAsync(String.Concat(JsonSerializer.Serialize<EventPayload>(payload, SerializerOptions), "\n"), httpContext.RequestAborted);
                }
            }
            catch (TaskCanceledException) when (httpContext.RequestAborted.IsCancellationRequested)
            {
                // Task was canceled by a consumer.
            }
            catch (OperationCanceledException) when (httpContext.RequestAborted.IsCancellationRequested) 
            {
                // Task was canceled by a consumer.
            }
            return;
        }

        throw new NotSupportedException($"Only IAsyncEnumerable<EventPayload> is supported. Unsupported type {context.ObjectType}");
    }
}