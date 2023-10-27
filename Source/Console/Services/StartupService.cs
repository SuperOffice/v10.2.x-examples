using Example.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SuperOffice;
using SuperOffice.Configuration;
using SuperOffice.Security.Principal;

public class StartupService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    public StartupService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
        var tokenValidationResult = await authService.Authenticate();
        
       // Pass the result to another service if needed
        var getPersonService = scope.ServiceProvider.GetRequiredService<IContactService>();
        getPersonService.GetContact(tokenValidationResult);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
