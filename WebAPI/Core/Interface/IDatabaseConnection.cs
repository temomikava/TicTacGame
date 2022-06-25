using WebAPI.Models;

namespace WebAPI.Core.Interface
{
    public interface IDatabaseConnection
    {
         (int Error, string ErrorMessage) Authorization(AuthorizationModel authorization);
         (int Error, string ErrorMessage) Registration(RegistrationModel registration);
         (int Error, string ErrorMessage) CreateMatch(Matchup match);
    }
}
