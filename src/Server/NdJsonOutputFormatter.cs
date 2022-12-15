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
                    /*
                         This trick sends first bytes in first message chunk - message formatter can handle it. 
                         It allows to send headers and fight with 100 seconds default timeout for HttpClient.
                    */
                    if (payload is null)
                    {
                        await httpContext.Response.WriteAsync($" ", httpContext.RequestAborted);
                        continue;
                    }

                    await httpContext.Response.WriteAsync(String.Concat(JsonSerializer.Serialize<EventPayload>(payload, SerializerOptions), "\n"), httpContext.RequestAborted);
                }
            }
            catch (TaskCanceledException) when (httpContext.RequestAborted.IsCancellationRequested)
            {
            }
            return;
        }

        throw new NotImplementedException();
    }
}