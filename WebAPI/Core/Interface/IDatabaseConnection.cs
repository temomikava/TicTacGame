using WebAPI.Models;

namespace WebAPI.Core.Interface
{
    public interface IDatabaseConnection
    {
        (int Error, string ErrorMessage, Guid SessionId) Authorization(AuthorizationModel authorization);
        (int Error, string ErrorMessage) Registration(RegistrationModel registration);
        (int Error, string ErrorMessage) CreateMatch(Matchup match);
        int GetUserId(Guid sessionId);
    }
}
