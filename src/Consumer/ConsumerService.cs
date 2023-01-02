using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class ConsumerService : IHostedService
{
    private const string RequestUri = "http://127.0.0.1:5286/events/head";
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly ILogger<ConsumerService> _logger;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly HttpClient _httpClient;

    public ConsumerService(
        IHostApplicationLifetime hostApplicationLifetime, ILogger<ConsumerService> logger)
    {
        _hostApplicationLifetime = hostApplicationLifetime;
        _logger = logger;
        _cancellationTokenSource = new CancellationTokenSource();
        _httpClient = new HttpClient();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _hostApplicationLifetime.ApplicationStarted.Register(OnStarted);
        _hostApplicationLifetime.ApplicationStopping.Register(OnStopping);
        _hostApplicationLifetime.ApplicationStopped.Register(OnStopped);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    private void OnStarted()
    {
        Task.Run(() => RunAsync(_cancellationTokenSource.Token).Wait());
    }
    private async Task RunAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Connecting to the server - listening starts.");
        try
        {
            await Task.Delay(5000, cancellationToken).ConfigureAwait(false); // Give 5 seconds for server to warm up
            var getChunkResponse = await _httpClient.GetAsync(RequestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            await using var responseStream = await getChunkResponse.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using StreamReader streamReader = new StreamReader(responseStream);
            do
            {
                var chunk = await streamReader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("{chunk}", chunk);
            } while (!cancellationToken.IsCancellationRequested);
        }
        catch (TaskCanceledException ex) when (cancellationToken.Equals(ex.CancellationToken))
        {
            return;
        }
        catch (Exception ex)
        {
            var message = ex.Message;
            _logger.LogCritical("Critical error, listening is off {Message}", message);
        }
    }

    private void OnStopping()
    {
        _cancellationTokenSource.Cancel();
    }

    private void OnStopped()
    {
    }
}