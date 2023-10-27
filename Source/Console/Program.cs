// See https://aka.ms/new-console-template for more information
using Example.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SuperOffice.CRM.Services;
using SuperOffice.Security.Principal;

internal class Program
{
  private static async System.Threading.Tasks.Task Main(string[] args)
  {
    var builder = Host.CreateDefaultBuilder(args)
          .ConfigureAppConfiguration((hostingContext, config) =>
          {
            config.SetBasePath(AppContext.BaseDirectory)
                     .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
          })
          .ConfigureServices((hostContext, services) =>
          {
            services.AddOptions();
            services.Configure<Configuration>(hostContext.Configuration.GetSection("Configuration"));
            services.AddSingleton<IAuthService, AuthService>();
            services.AddSingleton<IContactService, ContactService>();
            services.AddHostedService<StartupService>();
            services.AddNetServerCore<ThreadContextProvider>();
            services.AddServicesProxies();
          });

    var host = builder.Build();
    var serviceProvider = host.Services;
    serviceProvider.RegisterWithNetServer();
    await host.RunAsync();
  }
}