using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class LongPollingCustomerService : IHostedService
{
    private const string RequestUri = "http://127.0.0.1:5286/events/head";
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly ILogger<LongPollingCustomerService> _logger;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly HttpClient _httpClient;

    public LongPollingCustomerService(
        IHostApplicationLifetime hostApplicationLifetime, ILogger<LongPollingCustomerService> logger)
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

        try
        {
            var getChunkResponse = await _httpClient.GetAsync(RequestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            await using var responseStream = await getChunkResponse.Content.ReadAsStreamAsync(cancellationToken);
            using StreamReader streamReader = new StreamReader(responseStream);
            do
            {
                var chunk = await streamReader.ReadLineAsync(cancellationToken);
                _logger.LogInformation(chunk);
            } while (!cancellationToken.IsCancellationRequested);
        }
        catch (TaskCanceledException ex) when (cancellationToken.Equals(ex.CancellationToken))
        {
            return;
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