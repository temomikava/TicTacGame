using WebAPI.Models;
using GameLibrary;
using GameLibrary.Enums;

namespace WebAPI.Core.Interface
{
    public interface IDatabaseConnection
    {
        (int ErrorCode, string ErrorMessage, Guid? SessionId) Authorization(AuthorizationModel authorization);
        (int Error, string ErrorMessage) Registration(RegistrationModel registration);
        (int ErrorCode, string ErrorMessage, int GameId) GameCreate(Game game);
        int GetUserId(Guid sessionId);
        public Task<IEnumerable<Game>> GetGames();
        public (int ErrorCode, string ErrorMessage) GameStart(int gameId);
        public (int ErrorCode, string ErrorMessage, int matchId) MatchStart(Match match);
        public (int ErrorCode, string ErrorMessage) MatchEnd(Match match);
        public (int ErrorCode, string ErrorMessage, string Username) GetUsername(int userId);
        public (int ErrorCode, string ErrorMessage) GameEnd(Game game);
        public Task<Match> GetActiveMatch(int gameId);
        public (int ErrorCode, string ErrorMessage) JoinToGame(int gameId, int playerId);
        public Task<Game> GetGameByID(int gameId);
        public void MakeMove(Match match, int r, int c);
        public  Task<Mark[,]> FillGrid(Match match);
        public Task Ondisconnected(int gameId);











    }
}
