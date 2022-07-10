using WebAPI.Models;
using GameLibrary;
namespace WebAPI.Core.Interface
{
    public interface IDatabaseConnection
    {
        (int ErrorCode, string ErrorMessage, Guid? SessionId) Authorization(AuthorizationModel authorization);
        (int Error, string ErrorMessage) Registration(RegistrationModel registration);
        (int ErrorCode, string ErrorMessage, int GameId) SaveGameToDb(IMatch game);
        int GetUserId(Guid sessionId);
        public List<IMatch> GetGames();
        public (int ErrorCode, string ErrorMessage) JoinToGame(IMatch game);
        public (int ErrorCode, string ErrorMessage, int matchId) MatchStart(IMatch game);
        public (int ErrorCode, string ErrorMessage) MatchEnd(IMatch match);



    }
}
