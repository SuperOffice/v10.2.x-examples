using IdentityModel.Client;
using IdentityModel.OidcClient;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SuperOffice;
using SuperOffice.Configuration;
using SuperOffice.CRM.Services;
using SuperOffice.Security.Principal;
using System;
using System.Threading;
using System.Threading.Tasks;
using HttpClient = System.Net.Http.HttpClient;
using Task = System.Threading.Tasks.Task;

namespace NativeAppConsole
{
    public class AppLogic : IHostedService
    {
        private const string WebApiUrlClaim = "http://schemes.superoffice.net/identity/webapi_url";
        private const string NetServerUrlClaim = "http://schemes.superoffice.net/identity/netserver_url";
        private const string ContextIdentifierClaim = "http://schemes.superoffice.net/identity/ctx";

        //ContactId to fetch from the API or NetServer
        private const int ContactId = 5;

        private readonly ILogger<AppLogic> _logger;
        private readonly OidcClient _oidcClient;
        private readonly HttpClient _httpClient;
        private readonly AppSettings _appSettings;

        private string _accessToken = "";
        private string _netserverURI = "";
        private string _webapiURI = "";
        private string _refreshToken = "";
        private string _contextIdentifier = "";

        public AppLogic(ILogger<AppLogic> logger, OidcClient oidcClient, IHttpClientFactory httpClientFactory, IOptions<AppSettings> appSettings)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _oidcClient = oidcClient ?? throw new ArgumentNullException(nameof(oidcClient));
            _httpClient = httpClientFactory?.CreateClient() ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _appSettings = appSettings?.Value ?? throw new ArgumentNullException(nameof(appSettings));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("+-----------------------+");
            Console.WriteLine("|  Sign in with OIDC    |");
            Console.WriteLine("+-----------------------+");
            Console.WriteLine("");
            Console.WriteLine("Press any key to sign in...");

            await Task.Run(() => Console.ReadKey(), cancellationToken);
            await LoginAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("App is shutting down...");
            return Task.CompletedTask;
        }

        private async Task LoginAsync(CancellationToken cancellationToken)
        {
            try
            {
                var result = await _oidcClient.LoginAsync(new LoginRequest(), cancellationToken);
                if (result.IsError)
                {
                    _logger.LogError("Login failed: {Error}", result.Error);
                    return;
                }

                // Store the tokens and claims for later use
                _accessToken = result.AccessToken ?? throw new InvalidOperationException("Access token was not received.");
                _refreshToken = result.RefreshToken;
                _webapiURI = result.User.FindFirst(WebApiUrlClaim)?.Value ?? throw new InvalidOperationException("Web API URL was not received.");
                _netserverURI = result.User.FindFirst(NetServerUrlClaim)?.Value ?? throw new InvalidOperationException("NetServer URL was not received.");
                _contextIdentifier = result.User.FindFirst(ContextIdentifierClaim)?.Value ?? throw new InvalidOperationException("Context identifier was not received.");

                ShowResult(result);
                await NextSteps();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during login.");
            }
        }

        private void ShowResult(LoginResult result)
        {
            Console.WriteLine("\nClaims:");
            foreach (var claim in result.User.Claims)
            {
                Console.WriteLine("{0}: {1}", claim.Type, claim.Value);
            }

            Console.WriteLine("\nIdentity token: {0}", result.IdentityToken);
            Console.WriteLine("Access token:   {0}", result.AccessToken);
            Console.WriteLine("Refresh token:  {0}", result.RefreshToken ?? "none");
        }

        private async Task NextSteps()
        {
            while (true)
            {
                Console.WriteLine("\nChoose an option:");
                Console.WriteLine("1: Call Rest API");
                Console.WriteLine("2: Call NetServer Proxies");
                Console.WriteLine("3: Refresh token");
                Console.WriteLine("4: Exit");
                var choice = Console.ReadLine();

                try
                {
                    switch (choice)
                    {
                        case "1":
                            await CallRestApiAsync();
                            break;
                        case "2":
                            CallNetServerProxies();
                            break;
                        case "3":
                            await RefreshTokenAsync();
                            break;
                        case "4":
                            return;
                        default:
                            _logger.LogWarning("Invalid choice: {Choice}", choice);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while processing the choice.");
                }
            }
        }

        private async Task CallRestApiAsync()
        {
            try
            {
                _httpClient.BaseAddress = new Uri(_webapiURI);
                _httpClient.SetBearerToken(_accessToken);

                var response = await _httpClient.GetAsync($"v1/Contact/{ContactId}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("API response:\n{Json}", json);
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

        private void CallNetServerProxies()
        {
            try
            {
                ConfigFile.WebServices.RemoteBaseURL = _netserverURI;
                ConfigFile.Services.ApplicationToken = _appSettings.ClientSecret;
                var ticket = _accessToken;

                using var session = SoSession.Authenticate(new SoCredentials(ticket));
                using var ca = new ContactAgent();
                var ce = ca.GetContactEntity(ContactId);
                Console.WriteLine("Logged on to context {0} as {1}, and fetched name for contactId with id 5: {2}", _contextIdentifier, SoContext.CurrentPrincipal?.Associate ?? "Unknown", ce.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during authentication for customer {ContextIdentifier}", _contextIdentifier);
            }
        }

        private async Task RefreshTokenAsync()
        {
            if (string.IsNullOrEmpty(_refreshToken))
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

            Console.WriteLine("Tokens refreshed successfully!");
            Console.WriteLine("Access token:   {0}", _accessToken);
            Console.WriteLine("Refresh token:  {0}", _refreshToken ?? "none");
        }
    }
}
