using WebAPI.Models;

namespace WebAPI.Core.Interface
{
    public interface IDatabaseConnection
    {
        public (int Error, string ErrorMessage) Authorization(AuthorizationModel authorization);
        public (int Error, string ErrorMessage) Registration(RegistrationModel registration);
    }
}
