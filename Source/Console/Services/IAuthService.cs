using Microsoft.IdentityModel.Tokens;

namespace Example.Services
{
    public interface IAuthService
    {
        Task<TokenValidationResult> Authenticate();
    }
}
