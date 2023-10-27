// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.DependencyInjection;
using SuperOffice;
using SuperOffice.Configuration;
using SuperOffice.CRM.Services;
using SuperOffice.Security.Principal;
using SuperOffice.SystemUser;

internal class Program
{
  private static readonly string _clientSecret = "";
  private static readonly string _contextIdentifier = "";
  private static readonly string _subDomain = "";
  private static readonly string _systemUserToken = "";
  private static readonly string _privateKey = @"<RSAKeyValue>
  </RSAKeyValue>";

  private static async System.Threading.Tasks.Task Main(string[] args)
  {
    var services = new ServiceCollection();
    ConfigureServices(services);
    var provider = services.BuildServiceProvider(true);
    provider.RegisterWithNetServer();

    // code
    await MyFunction();
  }

  private static SystemUserInfo GetSystemUserInfo()
  {
    var sysUser = new SystemUserInfo
    {
      ClientSecret = _clientSecret,
      ContextIdentifier = _contextIdentifier,
      SubDomain = _subDomain,
      SystemUserToken = _systemUserToken,
      PrivateKey = _privateKey
    };
    return sysUser;
  }

  private static void ConfigureServices(IServiceCollection services)
  {
    services.AddNetServerCore<ThreadContextProvider>();
    services.AddServicesProxies();
  }

  private static async System.Threading.Tasks.Task MyFunction()
  {
    var client = new SystemUserClient(GetSystemUserInfo(), new HttpClient());
    var tokenResult = await client.GetSystemUserJwtAsync();
    var tokenValidationResult = await client.ValidateSystemUserResultAsync(tokenResult);
    if (tokenValidationResult.IsValid)
    {
      Console.WriteLine("Token is valid, next step is to log in as the system user and fetch a contact name");
      var contextIdentifier = tokenValidationResult.Claims["http://schemes.superoffice.net/identity/ctx"].ToString();

      ConfigFile.WebServices.RemoteBaseURL = tokenValidationResult.Claims["http://schemes.superoffice.net/identity/netserver_url"].ToString();
      ConfigFile.Services.ApplicationToken = _clientSecret;

      try
      {
        var ticket = tokenValidationResult.Claims["http://schemes.superoffice.net/identity/ticket"].ToString();
        // Log in as the system user
        using var session = SoSession.Authenticate(new SoCredentials(ticket));
        //// Do work as the system user
        using var ca = new ContactAgent();
        var ce = ca.GetContactEntity(5);
        Console.WriteLine("Logged on to context {0} as {1}, and fetched name for contactId with id 5: {2}", contextIdentifier, SoContext.CurrentPrincipal == null ? "Unknown" : SoContext.CurrentPrincipal.Associate, ce.Name);
      }
      catch (Exception ex)
      {
        while (ex.InnerException != null)
        {
          ex = ex.InnerException;
        }
        Console.WriteLine("Exception during authentication for customer {0}: {1}", contextIdentifier, ex.Message);
      }
    }
    else
    {
      Console.WriteLine("Token is invalid");
    }
  }
}