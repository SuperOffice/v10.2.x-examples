using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SuperOffice.SystemUser;

namespace Example.Services
{
  public class AuthService : IAuthService
  {
    private readonly Configuration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public AuthService(IOptions<Configuration> configuration, IHttpClientFactory httpClientFactory)
    {
      _configuration = configuration.Value;
      _httpClientFactory = httpClientFactory;
    }

    private SystemUserInfo GetSystemUserInfo()
    {
      var sysUser = new SystemUserInfo
      {
        ClientSecret = _configuration.ClientSecret,
        ContextIdentifier = _configuration.ContextIdentifier,
        SubDomain = _configuration.SubDomain,
        SystemUserToken = _configuration.SystemUserToken,
        PrivateKey = _configuration.PrivateKey
      };
      return sysUser;
    }

    public async Task<TokenValidationResult> Authenticate()
    {
      var httpClient = _httpClientFactory.CreateClient();
      var client = new SystemUserClient(GetSystemUserInfo(), httpClient);
      var tokenResult = await client.GetSystemUserJwtAsync();
      var tokenValidationResult = await client.ValidateSystemUserResultAsync(tokenResult);
      if (tokenValidationResult.IsValid)
      {
        Console.WriteLine("Token is valid");
        return tokenValidationResult;
      }
      else
      {
        throw new Exception("Token is invalid");
      }
    }
  }
}
