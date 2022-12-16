using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

await Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
       services.AddHostedService<ConsumerService>();
    })
    .Build()
    .RunAsync();