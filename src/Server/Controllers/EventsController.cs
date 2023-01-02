using Microsoft.AspNetCore.Mvc;
using MrMatrix.Net.LongPolling.Server.Models;
using System.Runtime.CompilerServices;

namespace MrMatrix.Net.LongPolling.Server.Controllers;

[ApiController]
[Route("[controller]")]
public sealed class EventsController : ControllerBase
{
    private static volatile TaskCompletionSource _taskCompletionSource = new();
    /* 
        Emulation of external store for events.
        Keep in mind that this store is in memory, so be aware of out of memory exception.
    */
    private static SynchronizedCollection<string> _payloads = new();

    private readonly ILogger<EventsController> _logger;

    public EventsController(ILogger<EventsController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="globalSequenceNumber"></param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Stream of events.</returns>
    [HttpGet()]
    [Route("head")]
    [Route("next/gsn/{globalSequenceNumber}")]
    [Produces("application/x-ndjson")]
    public async IAsyncEnumerable<EventPayload> Get(int globalSequenceNumber = 0, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Consumer connected");

        cancellationToken.Register(() =>
        {
            _logger.LogWarning("Consumer disconnected");
        });

        /*
         This trick sends first bytes in first message chunk - mesage formater can handle it. 
         It allows to send headers and fight with 100 seconds default timeout for HttpClient.

         Comment out line below to get in the Consumer error:
         `The request was canceled due to the configured HttpClient.Timeout of 100 seconds elapsing.`
         */
        await this.HttpContext.Response.WriteAsync(" ", cancellationToken).ConfigureAwait(false); // <== HttpClient.Timeout hack.

        for (var gsn = globalSequenceNumber; !cancellationToken.IsCancellationRequested; gsn++)
        {
            var task = _taskCompletionSource.Task; // Task is taken first to avoid potential lock.
            while (_payloads.Count <= gsn)
            {
                await task.WaitAsync(cancellationToken).ConfigureAwait(false);
                if (cancellationToken.IsCancellationRequested)
                {
                    yield break;
                }
                task = _taskCompletionSource.Task;
            }

            yield return new EventPayload
            {
                GlobalSequenceNumber = gsn + 1,
                Payload = _payloads[gsn]
            };
        }
    }

    /// <summary>
    /// Appends single payload to the store.
    /// </summary>
    /// <param name="payload">The payload to be added.</param>
    [HttpPost("append")]
    public IActionResult Append([FromBody] string payload)
    {
        _payloads.Add(payload);
        TriggerTaskCompletion();
        return NoContent();
    }

    /// <summary>
    /// Appends multiple payloads to the store.
    /// </summary>
    /// <param name="payloads">The payloads collection to be added.</param>
    [HttpPost("append-range")]
    public IActionResult AppendRange([FromBody] List<string> payloads)
    {
        payloads.ForEach(_payloads.Add);
        TriggerTaskCompletion();
        return NoContent();
    }

    private void TriggerTaskCompletion()
    {
        var newTcs = new TaskCompletionSource();
        var previousTcs = Interlocked.Exchange(ref _taskCompletionSource, newTcs);

        if (previousTcs.TrySetResult())
        {
            _logger.LogInformation("TaskCompletionSource.TrySetResult succeeded.");
        }
        else
        {
            _logger.LogWarning("TaskCompletionSource.TrySetResult failed.");
        }
    }
}