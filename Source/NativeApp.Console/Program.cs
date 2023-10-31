using IdentityModel.OidcClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SuperOffice.Security.Principal;

namespace NativeAppConsole
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            using var host = CreateHostBuilder(args).Build();
            var serviceProvider = host.Services;
            serviceProvider.RegisterWithNetServer();
            await host.RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration((context, config) =>
        {
            config.SetBasePath(Directory.GetCurrentDirectory())
                  .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        })
        .ConfigureServices((hostContext, services) =>
        {
            var configuration = hostContext.Configuration;
            var appSettings = configuration.GetSection("ApplicationSettings").Get<AppSettings>();
            if (appSettings != null)
            {
                services.AddHostedService<AppLogic>();
                services.AddSingleton<SystemBrowser>();
                services.AddSingleton<OidcClient>(sp =>
                {
                    var browser = sp.GetRequiredService<SystemBrowser>();
                    string redirectUri = $"http://127.0.0.1:{browser.Port}";

                    var options = new OidcClientOptions
                    {
                        Authority = $"https://{appSettings.Environment}.superoffice.com",
                        ClientId = appSettings.ClientId,
                        RedirectUri = redirectUri,
                        Scope = "openid profile",
                        FilterClaims = false,
                        Browser = browser,
                        LoadProfile = false
                    };

                    return new OidcClient(options);
                });
                services.AddNetServerCore<ThreadContextProvider>();
                services.AddServicesProxies();

                // Add the configuration instance to the DI container
                services.Configure<AppSettings>(configuration.GetSection("ApplicationSettings"));
            }
            else
            {
                throw new Exception("ApplicationSettings not found in appsettings.json");
            }
        })
        .ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
        });
    }
}