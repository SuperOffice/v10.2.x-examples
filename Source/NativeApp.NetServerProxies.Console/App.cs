using IdentityModel.Client;
using IdentityModel.OidcClient;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SuperOffice;
using SuperOffice.Configuration;
using SuperOffice.CRM.Services;
using SuperOffice.Security.Principal;
using Task = System.Threading.Tasks.Task;

namespace NativeApp.Console
{
    public class App : IHostedService
    {
        private readonly ILogger<App> _logger;
        private readonly OidcClient _oidcClient;
        private readonly HttpClient _httpClient;
        private string _accessToken = "";
        private string _netserverURI = "";
        private string _webapiURI = "";
        private string _refreshToken = "";
        private string _contextIdentifier = "";
        private readonly AppSettings _appSettings;
        //private readonly string _clientSecret = "a0850bea72c9210c12414b2002b395b2";

       public App(ILogger<App> logger, OidcClient oidcClient, IHttpClientFactory httpClientFactory, IOptions<AppSettings> appSettings)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _oidcClient = oidcClient ?? throw new ArgumentNullException(nameof(oidcClient));
        _httpClient = httpClientFactory.CreateClient();
        _appSettings = appSettings.Value ?? throw new ArgumentNullException(nameof(appSettings));
    }

        public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("App started. Press 'Enter' to sign in...");
        await Task.Run(() => System.Console.ReadLine(), cancellationToken);

        await LoginAsync(cancellationToken);
    }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // Cleanup or shutdown activities can be added here.
            _logger.LogInformation("App is shutting down...");
            return Task.CompletedTask;
        }

        private async Task LoginAsync(CancellationToken cancellationToken)
        {
            var result = await _oidcClient.LoginAsync(new LoginRequest(), cancellationToken);
            if (result.IsError)
            {
                _logger.LogError("Login failed: {Error}", result.Error);
                return;
            }

            _accessToken = result.AccessToken;
            _refreshToken = result.RefreshToken;
            if(result.User.FindFirst("http://schemes.superoffice.net/identity/webapi_url") != null)
                _webapiURI = result.User.FindFirst("http://schemes.superoffice.net/identity/webapi_url").Value;
            if(result.User.FindFirst("http://schemes.superoffice.net/identity/netserver_url") != null)    
                _netserverURI = result.User.FindFirst("http://schemes.superoffice.net/identity/netserver_url").Value;
            if(result.User.FindFirst("http://schemes.superoffice.net/identity/ctx") != null)
                _contextIdentifier = result.User.FindFirst("http://schemes.superoffice.net/identity/ctx").Value;

            ShowResult(result);
            await NextSteps();
        }

        private void ShowResult(LoginResult result)
        {
            System.Console.WriteLine("\nClaims:");
            foreach (var claim in result.User.Claims)
            {
                System.Console.WriteLine("{0}: {1}", claim.Type, claim.Value);
            }

            System.Console.WriteLine("\nidentity token: {0}", result.IdentityToken);
            System.Console.WriteLine("access token:   {0}", result.AccessToken);
            System.Console.WriteLine("refresh token:  {0}", result.RefreshToken ?? "none");
        }

        private async Task NextSteps()
        {
            while (true)
            {
                System.Console.WriteLine("\nChoose an option:");
                System.Console.WriteLine("1: Call Rest API");
                System.Console.WriteLine("2: Call NetServer Proxies");
                System.Console.WriteLine("3: Refresh token");
                System.Console.WriteLine("4: Exit");
                var choice = System.Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        await CallRestApiAsync();
                        break;
                    case "2":
                        await CallNetServerProxiesAsync();
                        break;
                    case "3":
                        await RefreshTokenAsync();
                        break;
                    case "4":
                        return;
                }
            }
        }

        private async Task CallRestApiAsync()
    {
        try
        {
            _httpClient.BaseAddress = new Uri(_webapiURI);
            _httpClient.SetBearerToken(_accessToken);

            var response = await _httpClient.GetAsync("v1/Contact/5");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                System.Console.WriteLine("API response:\n{0}", json);
            }
            else
            {
                _logger.LogError("API call failed: {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call Rest API");
        }
    }

        private async Task CallNetServerProxiesAsync()
        {
            try
            {
                ConfigFile.WebServices.RemoteBaseURL = _netserverURI;
                ConfigFile.Services.ApplicationToken = _appSettings.ClientSecret;
                var ticket = _accessToken;
                // Log in as the system user
                using var session = SoSession.Authenticate(new SoCredentials(ticket));
                //// Do work as the system user
                using var ca = new ContactAgent();
                var ce = ca.GetContactEntity(5);
                System.Console.WriteLine("Logged on to context {0} as {1}, and fetched name for contactId with id 5: {2}", _contextIdentifier, SoContext.CurrentPrincipal == null ? "Unknown" : SoContext.CurrentPrincipal.Associate, ce.Name);
            }
            catch (Exception ex)
            {
                while (ex.InnerException != null)
                {
                    ex = ex.InnerException;
                }
                System.Console.WriteLine("Exception during authentication for customer {0}: {1}", _contextIdentifier, ex.Message);
            }
        }
        private async Task RefreshTokenAsync()
        {
            if (_refreshToken == null)
            {
                _logger.LogWarning("No refresh token available.");
                return;
            }

            var result = await _oidcClient.RefreshTokenAsync(_refreshToken);
            if (result.IsError)
            {
                _logger.LogError("Token refresh failed: {Error}", result.Error);
                return;
            }

            _accessToken = result.AccessToken;
            _refreshToken = result.RefreshToken;

            System.Console.WriteLine("Tokens refreshed successfully!");
            System.Console.WriteLine("access token:   {0}", _accessToken);
            System.Console.WriteLine("refresh token:  {0}", _refreshToken ?? "none");
        }
    }
}
