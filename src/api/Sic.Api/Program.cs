using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sic.Core.Services;
using Sic.Cosmos;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddScoped<BookingService>();
        services.AddScoped<UserService>();
        services.AddScoped<InviteService>();

        var cosmosConnection = Environment.GetEnvironmentVariable("CosmosDbConnectionString")
            ?? throw new InvalidOperationException("CosmosDbConnectionString is not configured.");
        services.AddCosmosRepositories(cosmosConnection);
    })
    .Build();

host.Run();
