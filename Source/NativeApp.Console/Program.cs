using IdentityModel.OidcClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SuperOffice.Security.Principal;
using System;

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
                .ConfigureAppConfiguration(ConfigureAppConfiguration)
                .ConfigureServices(ConfigureServices)
                .ConfigureLogging(ConfigureLogging);

        private static void ConfigureAppConfiguration(HostBuilderContext context, IConfigurationBuilder config)
        {
            config.SetBasePath(Directory.GetCurrentDirectory())
                  .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        }

        private static void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
        {
            var configuration = hostContext.Configuration;
            var appSettings = configuration.GetSection("ApplicationSettings").Get<AppSettings>() ?? throw new Exception("ApplicationSettings not found in appsettings.json");
            services.AddHostedService<AppLogic>()
                    .AddSingleton<SystemBrowser>()
                    .AddSingleton(sp => ConfigureOidcClient(sp, appSettings))
                    .AddNetServerCore<ThreadContextProvider>()
                    .AddServicesProxies()
                    .Configure<AppSettings>(configuration.GetSection("ApplicationSettings"));
        }

        private static OidcClient ConfigureOidcClient(IServiceProvider sp, AppSettings appSettings)
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
        }

        private static void ConfigureLogging(ILoggingBuilder logging)
        {
            logging.ClearProviders()
                   .AddConsole();
        }
    }
}
