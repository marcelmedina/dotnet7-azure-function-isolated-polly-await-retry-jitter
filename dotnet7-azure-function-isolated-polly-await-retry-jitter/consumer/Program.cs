using consumer.TypedHttpClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using Polly.Contrib.WaitAndRetry;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration((hostingContext, config) =>
    {
        var currentDirectory = hostingContext.HostingEnvironment.ContentRootPath;

        config.SetBasePath(currentDirectory)
            .AddJsonFile("settings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();
        config.Build();
    })
    .ConfigureServices((services) =>
    {
        var delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(1), retryCount: 3);

        var retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(response => !response.IsSuccessStatusCode)
            .WaitAndRetryAsync(delay,
                onRetry: (message, retryCount) =>
                {
                    Console.Out.WriteLine("----------------------------------------------------");
                    Console.Out.WriteLine($"### RequestMessage: {message.Result.RequestMessage}");
                    Console.Out.WriteLine($"### StatusCode: {message.Result.StatusCode}");
                    Console.Out.WriteLine($"### ReasonPhrase: {message.Result.ReasonPhrase}");
                    Console.Out.WriteLine($"### Retry: {retryCount}");
                    Console.Out.WriteLine("----------------------------------------------------");
                });

        services.AddHttpClient<StateCounterHttpClient>()
            .AddPolicyHandler(retryPolicy);
    })
    .Build();

host.Run();
