using Azure.Identity;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core;

IHost host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
        .ConfigureAppConfiguration((hostingContext, configBuilder) =>
        {
            IHostEnvironment env = hostingContext.HostingEnvironment;

            DefaultAzureCredential credentials = new();

            string? u = Environment.GetEnvironmentVariable("VaultUri");

            if (u is null)
                throw new Exception("Need vault Uri...");

            Uri vaultUri = new(u);

            configBuilder
                .AddEnvironmentVariables()
                .AddUserSecrets("41ce1c77-8906-4946-8720-9d5fc8a37b95")
                .AddAzureKeyVault(vaultUri, credentials);
        })
    .ConfigureServices((context, services) =>
    {
        IConfiguration config = context.Configuration;

        Logger l = ConfiguredSerilog(config);

        services.AddLogging(lb => lb.AddSerilog(l));

        const string tokenUri = "https://discord.com/api/oauth2/token";
        services.AddHttpClient("DiscordGetToken",
            client => client.BaseAddress = new Uri(tokenUri));

        const string userUri = "https://discord.com/api/users";
        services.AddHttpClient("DiscordGetUser",
            client => client.BaseAddress = new Uri(userUri));

        services.AddAzureClients(clientBuilder =>
        {
            string? c = config["HighscoreDatabase"];

            clientBuilder.AddTableServiceClient(c);
        });
    })
    .Build();

await host.RunAsync();

static Logger ConfiguredSerilog(IConfiguration config)
{
    string? highscoreDatabase = config["HighscoreDatabase"];

    if (highscoreDatabase is null)
        throw new Exception("Missing URI for the tablestorage.");

    return new LoggerConfiguration()
       .MinimumLevel.Warning()
       .WriteTo.Console()
       .WriteTo.AzureTableStorage(highscoreDatabase)
       .CreateLogger();
}