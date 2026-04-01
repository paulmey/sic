using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sic.Core.Repositories;
using Sic.Core.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddScoped<BookingService>();
        services.AddScoped<UserService>();
        services.AddScoped<InviteService>();

        // TODO: Register repository implementations when Cosmos DB layer is added
        // services.AddScoped<IBookingRepository, CosmosBookingRepository>();
        // services.AddScoped<IUserRepository, CosmosUserRepository>();
        // etc.
    })
    .Build();

host.Run();
