using GameLibrary;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using WebAPI.Core.Interface;
using WebAPI.Core.Services;
using WebAPI.Models;
using GameLibrary;
using GameLibrary.Enums;

namespace WebAPI.SignalR
{

    public class GameHub : Hub
    {
        Game game;
        Match match;
        private IDatabaseConnection _connection;
        public GameHub(IDatabaseConnection connection)
        {
            _connection = connection;
        }
        private static ConcurrentDictionary<int, HashSet<string>> _users = new ConcurrentDictionary<int, HashSet<string>>();
        //private static ConcurrentDictionary<int, HashSet<Match>> _games = new ConcurrentDictionary<int, HashSet<Match>>();

        public override async Task OnConnectedAsync()
        {
            int id = int.Parse(Context.User.Claims.First(x => x.Type == ClaimTypes.Name).Value);
            string connid = Context.User.Claims.First(x => x.Type == ClaimTypes.Authentication).Value;
            if (_users.ContainsKey(id))
            {
                _users[id].Add(connid);
            }
            else
            {
                _users.TryAdd(id, new HashSet<string>() { connid });
            }
            var games = _connection.GetGames();
            //await Clients.All.SendAsync("getallgame", _games);
        }
        public override async Task OnDisconnectedAsync(Exception? exception)
        {

            int id = int.Parse(Context.User.Claims.First(x => x.Type == ClaimTypes.Name).Value);
            string connectionid = Context.User.Claims.First(x => x.Type == ClaimTypes.Authentication).Value;
            var connId = Context.ConnectionId;
            if (_users[id].Count > 1)
            {
                _users[id].Remove(connId);
            }
            else
            {
                _users.Remove(id, out var _);
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendAllGameToCaller()
        {
            var games = _connection.GetGames();
            await Clients.Caller.SendAsync("getallgame", games);
        }
        public async Task CreateGame(int boardSize, int scoreTarget)
        {
            game = new Game();
            var id = Context.User.Claims.First(x => x.Type == ClaimTypes.Name).Value;
            string errorMessage = "success";
            int errorCode = 1;
            if (boardSize < 3)
            {
                errorMessage = "board size cannot be less than 3";
                errorCode = -1;
            }
            else if (scoreTarget < 1)
            {
                errorMessage = "scoretarget cannot be less than 1";
                errorCode = -1;
            }
            else
            {

                game.PlayerOne.Id = int.Parse(id);
                game.CreatedAt = DateTime.Now;
                game.StateId = (int)StateType.Created;
                game.TargetScore = scoreTarget;
                game.BoardSize = boardSize;
                var saveGameToDb = _connection.GameCreate(game);
                if (saveGameToDb.ErrorCode == 1)
                {
                    await SendGamesToAllClient();
                }
                else
                {
                    errorMessage = "my bad";
                    errorCode = -1;
                }
            }
            await Clients.Caller.SendAsync("ongamecreate", errorMessage, errorCode);
        }
        private async Task SendGamesToAllClient()
        {
            List<Game> games = _connection.GetGames();
            await Clients.All.SendAsync("getgames", games);

        }

        public async Task JoinToGame(int gameId)
        {
            var id = Context.User.Claims.First(x => x.Type == ClaimTypes.Name).Value;

            var games = _connection.GetGames();
            var waiterGames = games.Where(x => x.StateId == (int)StateType.Created);
            if (waiterGames.All(x => x.Id != gameId))
            {
                await Clients.Caller.SendAsync("ongamejoin", "here is no waiter game for this id");
                return;
            }
            else
            {
                var game = waiterGames.First(game => game.Id == gameId);
                game.StartedAt = DateTime.Now;
                game.StateId = (int)StateType.Started;
                game.PlayerTwo.Id = int.Parse(id);
                var updateGamesToDb = _connection.GameStart(game);
                await MatchStart();
                await Clients.Users(game.PlayerOne.Id.ToString(), game.PlayerTwo.Id.ToString()).SendAsync(game.PlayerOne.UserName + "`s Turn");
                await SendGamesToAllClient();

            }

        }
        public async Task MakeMove(int r, int c)
        {
            if (match.MatchOver)
            {
               await Clients.Caller.SendAsync("onmovemade", -1, "matchisover");
               return;
            }
            if (match.CurrentPlayer == Mark.X &&
                game.PlayerOne.Id == int.Parse(Context.User.Claims.First(x => x.Type == ClaimTypes.Name).Value))
            {
                
                
                var movemade = match.MakeMove(r, c);

                if (movemade.ErrorCode == (int)ErrorCode.Success)
                {
                    Move move = new Move();
                    move.PlayerId = game.PlayerOne.Id;
                    move.MatchId = match.Id;
                    move.RowCoordinate = r;
                    move.ColumnCoordinate = c;
                    _connection.MakeMove(move);

                    if (!match.MatchOver)
                    {
                        await Clients.Users(game.PlayerOne.Id.ToString(), game.PlayerTwo.Id.ToString()).SendAsync(game.PlayerTwo.UserName + "`s Turn");
                    }
                    else
                    {                       
                           await MatchEnd();                      
                    }
                    
                }
                else
                {
                    await Clients.Caller.SendAsync("onmovemade", movemade.ErrorCode, movemade.ErrorMessage);

                    return;
                }

            }
            else if (match.CurrentPlayer == Mark.O &&
                game.PlayerTwo.Id == int.Parse(Context.User.Claims.First(x => x.Type == ClaimTypes.Name).Value))
            {
                if (match.MatchOver)
                {
                    await Clients.Caller.SendAsync("onmovemade", -1, "matchisover");
                    return;
                }
                
                match.MakeMove(r, c);

            }
            else
            {
                await Clients.Caller.SendAsync("onmovemade", -1, "wait for your turn");
                return;
            }



            //MatchStart();

        }
        private async Task MatchStart()
        {
            match = new Match();
            match.GameGrid = new Mark[game.BoardSize, game.BoardSize];
            match.StateId = (int)StateType.Started;
            match.GameId = game.Id;
            match.Id = _connection.MatchStart(match).matchId;
            game.MatchList.Add(match);

        }
        private async Task MatchEnd()
        {
            match.WinnerId = match.CurrentPlayer == Mark.X ? game.PlayerOne.Id : match.CurrentPlayer == Mark.O ? game.PlayerTwo.Id : -1;
            var targetscore = match.WinnerId == game.PlayerOne.Id ? ++game.PlayerOneScore : match.WinnerId == match.PlayerTwo.Id ? ++game.PlayerTwoScore : -1;

            match.StateId = (int)StateType.Finishid;
            match.FinishedAt = DateTime.Now;
            _connection.MatchEnd(match);
            if (targetscore != game.TargetScore)
            {
                await MatchStart();
            }
            else
            {
                await GameEnd(game);
            }
        }
        //public async Task GameStart(Game game)
        //{
        //await MatchStart();
        //}
        public async Task GameEnd(Game game)
        {
            game.Winner_Player_id = game.PlayerOneScore > game.PlayerOneScore ? game.PlayerOne.Id : game.PlayerTwo.Id;
            game.FinishedAt = DateTime.Now;     
            game.StateId = (int)StateType.Finishid;
            
            _connection.GameEnd(game);
        }
        public async Task<int> Notify(string messagge)
        {
            await Clients.All.SendAsync("ReceiveMessage", "from server " + messagge);
            return 888;
        }


        //public string GetConnectionId() => Context.ConnectionId;
        //public async Task AddToGroup(string groupName)
        // =>    await Groups.AddToGroupAsync(Context.ConnectionId, groupName);


    }
}
