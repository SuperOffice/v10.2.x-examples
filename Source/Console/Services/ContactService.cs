using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SuperOffice;
using SuperOffice.Configuration;
using SuperOffice.CRM.Services;
using SuperOffice.Security.Principal;

namespace Example.Services
{
    public class ContactService : IContactService
    {
        private readonly Configuration _configuration;
        public ContactService(IOptions<Configuration> configuration, IAuthService authService)
        {
            _configuration = configuration.Value;

        }

        public void GetContact(TokenValidationResult tokenValidationResult)
        {
            try
            {
                //var tokenValidationResult = await _authService.Authenticate();
                var contextIdentifier = tokenValidationResult.Claims["http://schemes.superoffice.net/identity/ctx"].ToString();

                ConfigFile.WebServices.RemoteBaseURL = tokenValidationResult.Claims["http://schemes.superoffice.net/identity/netserver_url"].ToString();
                ConfigFile.Services.ApplicationToken = _configuration.ClientSecret;
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
                Console.WriteLine("Exception during authentication for customer {0}: {1}", _configuration.ContextIdentifier, ex.Message);
            }
        }
    }
}