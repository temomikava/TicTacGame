using GameLibrary;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Claims;
using WebAPI.Core.Interface;
using GameLibrary.Enums;
using WebAPI.Requests;
using System;
using AutoMapper;

namespace WebAPI.SignalR
{

    public class GameHub : Hub
    {
        Game mainGame;
        Match mainMatch;
        private readonly IMapper mapper;
        private IDatabaseConnection _connection;
        public GameHub(IDatabaseConnection connection, IMapper mapper)
        {
            _connection = connection;
            this.mapper = mapper;
        }
        private static ConcurrentDictionary<int, HashSet<string>> _activeUsers = new ConcurrentDictionary<int, HashSet<string>>();
        //private static ConcurrentDictionary<int, HashSet<Match>> _games = new ConcurrentDictionary<int, HashSet<Match>>();

        public override async Task OnConnectedAsync()
        {
            int id = int.Parse(Context.User.Claims.First(x => x.Type == ClaimTypes.Name).Value);
            string connid = Context.User.Claims.First(x => x.Type == ClaimTypes.Role).Value;
            await _connection.OnConnected(id);
            if (_activeUsers.ContainsKey(id))
            {
                _activeUsers[id].Add(connid);
            }
            else
            {
                _activeUsers.TryAdd(id, new HashSet<string>() { connid });
            }
            var relatedgames = _connection.GetGames().Result.Where(x => x.PlayerOne.Id == id || x.PlayerTwo.Id == id);

            relatedgames.Where(x => x.PlayerTwo.Id == 0).ToList().ForEach(x => _connection.WaitingForReconnect(x.GameId, (int)StateType.Created));

            relatedgames.Where(x => x.StateId == (int)StateType.OneIsConnected).ToList().ForEach(x => _connection.WaitingForReconnect(x.GameId, (int)StateType.Started));

            relatedgames.Where(x => x.PlayerTwo.Id != 0 && x.StateId == (int)StateType.noOneConnected).ToList().ForEach(x => _connection.WaitingForReconnect(x.GameId, (int)StateType.OneIsConnected));

            var rejoinableGames = relatedgames.Where(x => x.PlayerTwo.Id != 0 && (x.StateId == (int)StateType.noOneConnected || x.StateId == (int)StateType.OneIsConnected)).ToList();

            if (rejoinableGames.Count() > 0)
            {
                Dictionary<int, int[]> keyValuePairs = new Dictionary<int, int[]>();
                rejoinableGames.ForEach(x => keyValuePairs.TryAdd(x.GameId, _connection.GetMovesHistory(x.GameId).Result));

                var rejoinableGamesDTO = mapper.Map<IEnumerable<GameDTO>>(rejoinableGames);
                await Clients.Caller.SendAsync("onreconnected", rejoinableGamesDTO, keyValuePairs, id);
            }

            var gamess = _connection.GetGames().Result.Where(x => x.StateId == (int)StateType.Created);
            var gamesDTO = mapper.Map<IEnumerable<GameDTO>>(gamess);
            await Clients.Caller.SendAsync("getallgame", gamesDTO);



        }
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            string connId = Context.User.Claims.First(x => x.Type == ClaimTypes.Role).Value;
            int id = int.Parse(Context.User.Claims.First(x => x.Type == ClaimTypes.Name).Value);
            await _connection.Ondisconnected(id);
            var connectedGames = _connection.GetGames().Result.Where(x => x.PlayerOne.Id == id || x.PlayerTwo.Id == id);

            connectedGames.Where(x => x.StateId == (int)StateType.OneIsConnected || x.StateId == (int)StateType.Created).ToList().ForEach(x => _connection.WaitingForReconnect(x.GameId, (int)StateType.noOneConnected));

            connectedGames.Where(x => x.StateId == (int)StateType.Started).ToList().ForEach(x => _connection.WaitingForReconnect(x.GameId, (int)StateType.OneIsConnected));


            if (_activeUsers[id].Count > 1)
            {
                _activeUsers[id].Remove(connId);
            }
            else
            {
                _activeUsers.Remove(id, out var _);
            }

            int i = 0;
            while (!_activeUsers.ContainsKey(id))
            {
                await Task.Delay(1000);
                i++;
                if (i==20)
                {
                    connectedGames = _connection.GetGames().Result.Where(x => x.PlayerOne.Id == id || x.PlayerTwo.Id == id);
                    connectedGames.Where(x => x.PlayerTwo.Id == 0).ToList().ForEach(x => _connection.WaitingForReconnect(x.GameId, (int)StateType.Cancelled));
                    foreach (var game in connectedGames.Where(x => x.PlayerTwo.Id != 0 && x.StateId != (int)StateType.Finished))
                    {
                        var match = _connection.GetActiveMatch(game.GameId).Result;
                        var opponentId = game.PlayerOne.Id == id ? game.PlayerTwo.Id : game.PlayerOne.Id;
                        match.WinnerId = opponentId;
                        game.Winner_Player_id = opponentId;
                        _connection.MatchEnd(match);
                        game.StateId = (int)StateType.Finished;
                        _connection.GameEnd(game);
                        await Clients.User(opponentId.ToString()).SendAsync("gameend",game.GameId,game.PlayerOneScore,game.PlayerOneScore,"opponent disconneced, so you are a winner");
                        var availableGames = _connection.GetGames().Result.Where(x => x.StateId == (int)StateType.Created);
                        var gamesDTOS = mapper.Map<IEnumerable<GameDTO>>(availableGames);
                        await Clients.All.SendAsync("getallgame", gamesDTOS);
                    }
                    return;
                }
            }

            await base.OnDisconnectedAsync(exception);
          
        }


        public async Task CreateGame(int boardSize, int scoreTarget)
        {
            //int boardSize = request.BoardSize;
            //int scoreTarget = request.ScoreTarget;
            int playerOneId = int.Parse(Context.User.Claims.First(x => x.Type == ClaimTypes.Name).Value);
            mainGame = new Game();
            mainGame.CreatedAt = DateTime.Now;
            mainGame.BoardSize = boardSize;
            mainGame.TargetScore = scoreTarget;
            mainGame.StateId = (int)StateType.Created;
            mainGame.PlayerOne = new Player { Id = playerOneId };
            mainGame.PlayerOne.UserName = _connection.GetUsername(playerOneId).Result.Username;
            mainGame.GameId = _connection.GameCreate(mainGame).Result.gameId;
            var games = _connection.GetGames().Result.Where(x => x.StateId == (int)StateType.Created);
            var gameDTO = mapper.Map<GameDTO>(mainGame);
            //await Clients.Caller.SendAsync("getcurrentgame", gameDTO);
            //await Clients.Caller.SendAsync("ongamecreate", "created",mainGame.GameId);

            var gamesDTO = mapper.Map<IEnumerable<GameDTO>>(games);
            await Clients.All.SendAsync("getallgame", gamesDTO);
            //var waitingForOponent = new WaitingForOponent(mainGame.GameId, _connection.GetActiveMatch);
            //mainMatch = waitingForOponent.Waiting();

        }

        public async Task JoinToGame(int gameId)
        {
            //int gameId = request.GameId;
            int playerTwoId = int.Parse(Context.User.Claims.First(x => x.Type == ClaimTypes.Name).Value);
            var game = _connection.GetGameByID(gameId).Result;
            if (game == null)
            {
                await Clients.Caller.SendAsync("ongamejoin", -1, -1, "game not found", "");
                return;
            }
            if (game.PlayerTwo.Id != 0)
            {
                await Clients.Caller.SendAsync("ongamejoin", -1, -1, "this game is already started", "");
                return;
            }
            if (game.StateId == (int)StateType.noOneConnected)
            {
                await Clients.Caller.SendAsync("ongamejoin", -1, -1, "game creator is disconnected, wait for reconnect", "");
                return;
            }
            if (game.PlayerOne.Id == playerTwoId)
            {
                await Clients.Caller.SendAsync("ongamejoin", -1, -1, "you can not join to game which is created by you", "");
                return;
            }
            var join = _connection.JoinToGame(gameId, playerTwoId);


            await GameStart(gameId);


            await Clients.User(mainGame.PlayerOne.Id.ToString()).SendAsync("ongamejoin", 1, mainGame.GameId, "opponent connected! your turn ", mainGame.PlayerOne.UserName);
            await Clients.User(mainGame.PlayerTwo.Id.ToString()).SendAsync("ongamejoin", 1, mainGame.GameId, mainGame.PlayerOne.UserName + "`s turn", mainGame.PlayerTwo.UserName);




        }
        private async Task GameStart(int gameId)
        {
            int id = int.Parse(Context.User.Claims.First(x => x.Type == ClaimTypes.Name).Value);

            _connection.GameStart(gameId);
            mainGame = _connection.GetGameByID(gameId).Result;
            var games = _connection.GetGames().Result;
            var gameDTO = mapper.Map<GameDTO>(mainGame);
            var gamesDTO = mapper.Map<IEnumerable<GameDTO>>(games.Where(x => x.StateId == (int)StateType.Created));
            await Clients.All.SendAsync("getallgame", gamesDTO);
            var opponentId = mainGame.PlayerOne.Id == id ? mainGame.PlayerTwo.Id : mainGame.PlayerOne.Id;
            await Clients.Caller.SendAsync("getcurrentgame", gameDTO,id);
            await Clients.Users(opponentId.ToString()).SendAsync("getcurrentgame", gameDTO,opponentId);

            await MatchStart(gameId);
        }
        private async Task MatchStart(int gameId)
        {
            var match = new Match();
            match.GameId = gameId;
            match.StateId = (int)StateType.Started;
            match.CurrentPlayerId = mainGame.PlayerOne.Id;
            match.StartedAt = DateTime.Now;
            mainMatch = match;
            _connection.MatchStart(match);



        }
        public async Task NextMatch(int gameId)
        {
            Thread.Sleep(3000);
            await MatchStart(gameId);
            var ids = new List<string> { mainGame.PlayerOne.Id.ToString(), mainGame.PlayerTwo.Id.ToString() };
            await Clients.Users(ids).SendAsync("matchstart", gameId);
            await Clients.User(mainGame.PlayerOne.Id.ToString()).SendAsync("nextturn", -1, mainGame.GameId, "your turn", -1, -1, "");
            await Clients.User(mainGame.PlayerTwo.Id.ToString()).SendAsync("nextturn", -1, mainGame.GameId, mainGame.PlayerOne.UserName + "`s turn", -1, -1, "");
        }

        public async Task MakeMove(int gameId, int r, int c)
        {
            //int gameId = request.GameId;
            //int r = request.Row;
            //int c = request.Column;
            int callerId = int.Parse(Context.User.Claims.First(x => x.Type == ClaimTypes.Name).Value);
            mainGame = _connection.GetGameByID(gameId).Result;
            mainMatch = _connection.GetActiveMatch(gameId).Result;

            if (mainMatch == null)
            {
                await Clients.Caller.SendAsync("nextturn", -1, gameId, "match not found", -1, -1, "");
                return;
            }


            if (mainMatch.CurrentPlayerId != callerId)
            {
                await Clients.Caller.SendAsync("nextturn", -1, gameId, "wait for your turn", -1, -1, "");
                return;

            }
            mainMatch.PlayerOne = mainGame.PlayerOne;
            mainMatch.PlayerTwo = mainGame.PlayerTwo;
            mainMatch.GameGrid = _connection.FillGrid(mainMatch).Result;
            mainMatch.CurrentPlayer = mainMatch.CurrentPlayerId == mainMatch.PlayerOne.Id ? Mark.X : Mark.O;
            var makeMove = mainMatch.MakeMove(r, c);
            if (makeMove.ErrorCode != 1)
            {
                await Clients.Caller.SendAsync("nextturn", -1, gameId, makeMove.ErrorMessage, -1, -1, "");
                return;
            }

            var currentmove = mainMatch.CurrentPlayer == Mark.X ? "X" : "O";
            var caller = callerId == mainMatch.PlayerOne.Id ? mainMatch.PlayerOne : mainMatch.PlayerTwo;
            var opponent = caller == mainMatch.PlayerOne ? mainMatch.PlayerTwo : mainMatch.PlayerOne;
            if (mainMatch.MatchOver != true)
            {

                await Clients.Caller.SendAsync("nextturn", 1, gameId, opponent.UserName + "`s turn", r, c, currentmove);
                await Clients.User(opponent.Id.ToString()).SendAsync("nextturn", 1, gameId, "your turn", r, c, currentmove);
                _connection.MakeMove(mainMatch, r, c);
                var moves = _connection.GetMovesHistory(mainGame.GameId).Result;
                await _connection.UpdateBoardState(moves, mainMatch.Id);
                return;
            }
            else
            {
                await Clients.Caller.SendAsync("nextturn", 1, gameId, "match Over", r, c, currentmove);
                await Clients.User(opponent.Id.ToString()).SendAsync("nextturn", 1, gameId, "match over", r, c, currentmove);
                _connection.MakeMove(mainMatch, r, c);
                var moves = _connection.GetMovesHistory(mainGame.GameId).Result;
                await _connection.UpdateBoardState(moves, mainMatch.Id);
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
                winner = mainMatch.CurrentPlayerId == mainMatch.PlayerOne.Id ? mainMatch.PlayerTwo : mainMatch.PlayerOne;
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
                await Clients.Users(ids).SendAsync("matchend", mainGame.GameId, mainMatch.PlayerOneScore, mainMatch.PlayerTwoScore, "it is a tie");
                await NextMatch(mainGame.GameId);
                // await MatchStart(mainGame.GameId);
                return;
            }
            if (mainGame.TargetScore != mainMatch.PlayerOneScore && mainGame.TargetScore != mainMatch.PlayerTwoScore)
            {

                await Clients.User(winner.Id.ToString()).SendAsync("matchend", mainGame.GameId, mainGame.PlayerOneScore, mainGame.PlayerTwoScore, "you win the match!");
                await Clients.User(loser.Id.ToString()).SendAsync("matchend", mainGame.GameId, mainGame.PlayerOneScore, mainGame.PlayerTwoScore, "you lose the match!");
                _connection.MatchEnd(mainMatch);

                await NextMatch(mainGame.GameId);
                //await MatchStart(mainGame.GameId);
            }

            else
            {
                _connection.MatchEnd(mainMatch);
                mainGame.Winner_Player_id = mainMatch.WinnerId;
                mainGame.StateId = (int)StateType.Finished;
                await Clients.User(winner.Id.ToString()).SendAsync("matchend", mainGame.GameId, mainGame.PlayerOneScore, mainGame.PlayerTwoScore, "you win the match!");
                await Clients.User(loser.Id.ToString()).SendAsync("matchend", mainGame.GameId, mainGame.PlayerOneScore, mainGame.PlayerTwoScore, "you lose the match!");
                Thread.Sleep(2000);
                await Clients.User(winner.Id.ToString()).SendAsync("gameend", mainGame.GameId, mainGame.PlayerOneScore, mainGame.PlayerTwoScore, "you win the game!");
                await Clients.User(loser.Id.ToString()).SendAsync("gameend", mainGame.GameId, mainGame.PlayerOneScore, mainGame.PlayerTwoScore, "you lose the game!");
                _connection.GameEnd(mainGame);
                var games = _connection.GetGames().Result;
                var availableGames = games.Where(x => x.StateId == 1);
                var gamesDTOS = mapper.Map<IEnumerable<GameDTO>>(availableGames);
                await Clients.All.SendAsync("getallgame", gamesDTOS);

            }
        }

    }
}
