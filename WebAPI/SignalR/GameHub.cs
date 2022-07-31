using GameLibrary;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Claims;
using WebAPI.Core.Interface;
using GameLibrary.Enums;
using WebAPI.Requests;

namespace WebAPI.SignalR
{

    public class GameHub : Hub
    {
        Game mainGame;
        Match mainMatch;
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
            await Clients.Caller.SendAsync("getallgame", games);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            string connId = Context.User.Claims.First(x => x.Type == ClaimTypes.Authentication).Value;
            int id = int.Parse(Context.User.Claims.First(x => x.Type == ClaimTypes.Name).Value);
            var games = _connection.GetGames();
            List<Game> connectedGames = new List<Game>();
            games.Where(x => x.PlayerOne.Id == id || x.PlayerTwo.Id == id).ToList().ForEach(x => connectedGames.Add(x));
            connectedGames.ForEach(x => _connection.Ondisconnected(x.Id));
            foreach (var game in connectedGames)
            {
                if (game.PlayerTwo.Id != 0)
                {
                    var opponentId = id == game.PlayerOne.Id ? game.PlayerTwo.Id : game.PlayerOne.Id;

                    await Clients.User(opponentId.ToString()).SendAsync("ondisconnected", -1, "opponent disconnected!");

                }


            }

            var availableGames = _connection.GetGames();
            await Clients.All.SendAsync("getallgame", availableGames);

            //var connId = Context.ConnectionId;
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


        public async Task CreateGame(CreateGameRequest request)
        {
            int boardSize = request.BoardSize;

            int scoreTarget = request.ScoreTarget;

            if (boardSize < 3)
            {
                await Clients.Caller.SendAsync("ongamecreate", -1, "boardsize cannot be less than 3");
                return;
            }
            else if (scoreTarget < 1)
            {
                await Clients.Caller.SendAsync("ongamecreate", -1, "target score cannot be less than 1");
                return;
            }
            else
            {
                await Clients.Caller.SendAsync("ongamecreate", 1, "success");
            }
            int playerOneId = int.Parse(Context.User.Claims.First(x => x.Type == ClaimTypes.Name).Value);
            mainGame = new Game();
            mainGame.CreatedAt = DateTime.Now;
            mainGame.BoardSize = boardSize;
            mainGame.TargetScore = scoreTarget;
            mainGame.StateId = (int)StateType.Created;
            mainGame.PlayerOne = new Player { Id = playerOneId };
            mainGame.PlayerOne.UserName = _connection.GetUsername(playerOneId).Username;
            mainGame.Id = _connection.GameCreate(mainGame).GameId;
            var games = _connection.GetGames();
            await Clients.Caller.SendAsync("nextturn",1, "wait for opponent connection");
            await Clients.Others.SendAsync("getallgame", games);
            //var waitingForOponent = new WaitingForOponent(mainGame.Id, _connection.GetActiveMatch);
            //mainMatch = waitingForOponent.Waiting();

        }

        public async Task JoinToGame(JoinToGameRequest request)
        {
            int gameId = request.GameId;
            int playerTwoId = int.Parse(Context.User.Claims.First(x => x.Type == ClaimTypes.Name).Value);
            var join = _connection.JoinToGame(gameId, playerTwoId);



            mainGame = _connection.GetGameByID(gameId);

            await Clients.User(mainGame.PlayerOne.Id.ToString()).SendAsync("nextturn",1, "opponent connected! your turn ");
            await Clients.User(mainGame.PlayerTwo.Id.ToString()).SendAsync("nextturn",1, mainGame.PlayerOne.UserName + "`s turn");

            //(mainGame.PlayerOne.Id, mainGame.PlayerTwo.Id).SendAsync("ongamejoin", 1, mainGame.PlayerOne.UserName+"`s turn");

            await GameStart(gameId);

        }
        private async Task GameStart(int gameId)
        {
            _connection.GameStart(gameId);
            var games = _connection.GetGames();
            await Clients.All.SendAsync("getallgame", games);
            await MatchStart(gameId);
        }
        private async Task MatchStart(int gameId)
        {
            mainGame = _connection.GetGameByID(gameId);
            var match = new Match();
            match.GameId = gameId;
            match.StateId = (int)StateType.Started;
            match.CurrentPlayerId = mainGame.PlayerOne.Id;
            match.StartedAt = DateTime.Now;
            _connection.MatchStart(match);

        }

        public async Task MakeMove(MakeMoveRequest request)
        {
            int gameId = request.GameId;
            int r = request.Row;
            int c = request.Column;
            int callerId = int.Parse(Context.User.Claims.First(x => x.Type == ClaimTypes.Name).Value);
            mainGame = _connection.GetGameByID(gameId);
            mainMatch = _connection.GetActiveMatch(gameId);

            if (mainMatch == null)
            {
                await Clients.Caller.SendAsync("nextturn", -1, "match is not started");
                return;
            }

           
            if (mainMatch.CurrentPlayerId != callerId)
            {
                await Clients.Caller.SendAsync("nextturn", -1, "wait for your turn");
                return;

            }
            mainMatch.PlayerOne = mainGame.PlayerOne;
            mainMatch.PlayerTwo = mainGame.PlayerTwo;
            mainMatch.GameGrid = _connection.FillGrid(mainMatch);
            mainMatch.CurrentPlayer = mainMatch.CurrentPlayerId == mainMatch.PlayerOne.Id ? Mark.X : Mark.O;
            var makeMove = mainMatch.MakeMove(r, c);
            if (makeMove.ErrorCode != 1)
            {
                await Clients.Caller.SendAsync("nextturn", -1, makeMove.ErrorMessage);
                return;
            }
            if (mainMatch.MatchOver != true)
            {
                var caller = callerId == mainMatch.PlayerOne.Id ? mainMatch.PlayerOne : mainMatch.PlayerTwo;
                var opponent = caller == mainMatch.PlayerOne ? mainMatch.PlayerTwo : mainMatch.PlayerOne;
                await Clients.Caller.SendAsync("nextturn", 1, opponent.UserName + "`s turn");
                await Clients.User(opponent.Id.ToString()).SendAsync("nextturn", 1, "your turn");
                _connection.MakeMove(mainMatch, r, c);
                return;
            }
            MatchEnd();
        }
        private async void MatchEnd()
        {
            List<string> ids = new List<string> { mainMatch.PlayerOne.Id.ToString(), mainMatch.PlayerTwo.Id.ToString() };

            Player winner = new Player();
            Player loser = new Player();
            if (mainMatch.MatchResult.Winner != Mark.None)
            {
                winner = mainMatch.CurrentPlayerId == mainMatch.PlayerOne.Id ? mainMatch.PlayerOne : mainMatch.PlayerTwo;
                loser = winner == mainMatch.PlayerOne ? mainMatch.PlayerTwo : mainMatch.PlayerOne;

                if (winner == mainMatch.PlayerOne)
                {
                    mainMatch.PlayerOneScore = ++mainGame.PlayerOneScore;
                    mainMatch.PlayerTwoScore = mainGame.PlayerTwoScore;
                    mainMatch.WinnerId = mainMatch.PlayerOne.Id;
                }
                else
                {
                    mainMatch.PlayerOneScore = mainGame.PlayerOneScore;
                    mainMatch.PlayerTwoScore = ++mainGame.PlayerTwoScore;
                    mainMatch.WinnerId = mainMatch.PlayerTwo.Id;
                }

                _connection.MatchEnd(mainMatch);
            }
            else
            {
                mainMatch.PlayerOneScore = mainGame.PlayerOneScore;
                mainMatch.PlayerTwoScore = mainGame.PlayerTwoScore;
                mainMatch.WinnerId = -1;
                _connection.MatchEnd(mainMatch);
                await Clients.Users(ids).SendAsync("matchend", mainMatch.PlayerOneScore, mainMatch.PlayerTwoScore, "it is a tie");
                await MatchStart(mainGame.Id);
                return;
            }
            if (mainGame.TargetScore != mainMatch.PlayerOneScore && mainGame.TargetScore != mainMatch.PlayerTwoScore)
            {

                await Clients.User(winner.Id.ToString()).SendAsync("matchend", mainGame.PlayerOneScore, mainGame.PlayerTwoScore, "you win the match!");
                await Clients.User(loser.Id.ToString()).SendAsync("matchend", mainGame.PlayerOneScore, mainGame.PlayerTwoScore, "you lose the match!");
                _connection.MatchEnd(mainMatch);

                await MatchStart(mainGame.Id);
            }
            else
            {
                _connection.MatchEnd(mainMatch);
                mainGame.Winner_Player_id = mainMatch.WinnerId;
                mainGame.StateId = (int)StateType.Finished;
                await Clients.User(winner.Id.ToString()).SendAsync("gameend", mainGame.PlayerOneScore, mainGame.PlayerTwoScore, "you win the game!");
                await Clients.User(loser.Id.ToString()).SendAsync("gameend", mainGame.PlayerOneScore, mainGame.PlayerTwoScore, "you lose the game!");
                _connection.GameEnd(mainGame);
            }
        }

    }
}
