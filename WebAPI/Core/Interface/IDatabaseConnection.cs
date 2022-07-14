using WebAPI.Models;
using GameLibrary;
namespace WebAPI.Core.Interface
{
    public interface IDatabaseConnection
    {
        (int ErrorCode, string ErrorMessage, Guid? SessionId) Authorization(AuthorizationModel authorization);
        (int Error, string ErrorMessage) Registration(RegistrationModel registration);
        (int ErrorCode, string ErrorMessage, int GameId) GameCreate(Game game);
        int GetUserId(Guid sessionId);
        public List<Game> GetGames();
        public (int ErrorCode, string ErrorMessage) GameStart(Game game);
        public (int ErrorCode, string ErrorMessage, int matchId) MatchStart(Match match);
        public (int ErrorCode, string ErrorMessage) MatchEnd(Match match);
        public (int ErrorCode, string ErrorMessage, string Username) GetUsername(int userId);
        public (int ErrorCode, string ErrorMessage) GameEnd(Game game);
        public (int ErrorCode, string ErrorMessage) MakeMove(Move move);






    }
}
