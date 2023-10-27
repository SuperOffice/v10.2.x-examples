using Microsoft.IdentityModel.Tokens;

namespace Example.Services
{
    public interface IContactService
    {
        void GetContact(TokenValidationResult tokenValidationResult);
    }
}
