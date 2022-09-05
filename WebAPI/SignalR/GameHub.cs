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
using System.Collections;

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
        private static ConcurrentDictionary<int, HashSet<string>> _users = new ConcurrentDictionary<int, HashSet<string>>();
        private static ConcurrentDictionary<int, List<Game>> _rejoinableGames = new ConcurrentDictionary<int, List<Game>>();

        public override async Task OnConnectedAsync()
        {
            int id = int.Parse(Context.User.Claims.First(x => x.Type == ClaimTypes.Name).Value);
            string connid = Context.User.Claims.First(x => x.Type == ClaimTypes.Role).Value;
            if (_users.ContainsKey(id))
            {
                _users[id].Add(connid);
            }
            else
            {
                _users.TryAdd(id, new HashSet<string>() { connid });
            }
            var games = _connection.GetGames().Result.Where(x => x.PlayerOne.Id == id || x.PlayerTwo.Id == id);
            games.Where(x => x.PlayerTwo.Id == 0).ToList().ForEach(x => _connection.WaitingForReconnect(x.GameId, (int)StateType.Created));

            //var availableGames = games.Where(x => x.StateId == (int)StateType.Created || x.StateId == (int)StateType.Started).ToList();
            var gamess = _connection.GetGames().Result.Where(x => x.StateId == (int)StateType.Created);
            var gamesDTO = mapper.Map<IEnumerable<GameDTO>>(gamess);
            await Clients.All.SendAsync("getallgame", gamesDTO);

            var gamesForReconnect = games.Where(x => (x.StateId == (int)StateType.PlayerOneIsConnected || x.StateId == (int)StateType.NoOneIsConnected) && x.PlayerTwo.Id != 0).ToList();

            if (gamesForReconnect.Count() > 0)
            {
                var gamesForReconnectDTO = mapper.Map<IEnumerable<GameDTO>>(gamesForReconnect);

                await Clients.Caller.SendAsync("gamesforreconnect", gamesForReconnectDTO);
            }
            await base.OnConnectedAsync();
        }
        public async Task Reconnect(int gameId)
        {
            //int gameId = request.Gameid;
            int id = int.Parse(Context.User.Claims.First(x => x.Type == ClaimTypes.Name).Value);

            var game = _connection.GetGameByID(gameId).Result;
            if (game == null)
            {
                await Clients.Caller.SendAsync("ongamerejoin", -1, "game not found");
                return;
            }
            if (game.PlayerOne.Id != id && game.PlayerTwo.Id != id)
            {
                await Clients.Caller.SendAsync("ongamerejoin", -1, "you are not a member of this game", null, null, 0, 0);
                return;
            }
            if (game.StateId == (int)StateType.Started)
            {
                await Clients.Caller.SendAsync("ongamerejoin", -1, "this game is already started", null, null, 0, 0);
                return;
            }
            if ((game.StateId == (int)StateType.PlayerOneIsConnected && id == game.PlayerOne.Id) || (game.StateId == (int)StateType.PlayerTwoIsConnected && id == game.PlayerTwo.Id))
            {
                await Clients.Caller.SendAsync("ongamerejoin", -1, "you are already reconnected to this game", null, null, 0, 0);
                return;
            }
            var gameDTO = mapper.Map<GameDTO>(game);
            var match = _connection.GetActiveMatch(gameId).Result;
            var currentPlayerId = match.CurrentPlayerId;
            var movesHistory = _connection.GetMovesHistory(game.GameId).Result;
            await Clients.Caller.SendAsync("ongamerejoin", 1, "success", gameDTO, movesHistory, id, currentPlayerId);
            if (id == game.PlayerOne.Id)
            {
                if (game.StateId == (int)StateType.PlayerTwoIsConnected)
                {
                    await _connection.WaitingForReconnect(game.GameId, (int)StateType.Started);
                }
                else
                {
                    await _connection.WaitingForReconnect(game.GameId, (int)StateType.PlayerOneIsConnected);
                }
            }
            if (id == game.PlayerTwo.Id)
            {
                if (game.StateId == (int)StateType.PlayerOneIsConnected)
                {
                    await _connection.WaitingForReconnect(game.GameId, (int)StateType.Started);
                }
                else
                {
                    await _connection.WaitingForReconnect(game.GameId, (int)StateType.PlayerTwoIsConnected);
                }
            }

            var num = _rejoinableGames[id].RemoveAll(x => x.GameId == game.GameId);
            var gamesDTOs = mapper.Map<IEnumerable<GameDTO>>(_rejoinableGames[id]);
            await Clients.Caller.SendAsync("gamesforreconnect", gamesDTOs);
        }
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            string connId = Context.User.Claims.First(x => x.Type == ClaimTypes.Role).Value;
            int id = int.Parse(Context.User.Claims.First(x => x.Type == ClaimTypes.Name).Value);


            var games = _connection.GetGames().Result.Where(x => x.PlayerOne.Id == id || x.PlayerTwo.Id == id);
            games.Where(x => x.PlayerTwo.Id == 0).ToList().ForEach(x => _connection.WaitingForReconnect(x.GameId, (int)StateType.NoOneIsConnected));
            foreach (var game in games.Where(x => x.PlayerTwo.Id != 0))
            {
                if (id == game.PlayerOne.Id)
                {
                    if (game.StateId == (int)StateType.Started)
                    {
                        await _connection.WaitingForReconnect(game.GameId, (int)StateType.PlayerTwoIsConnected);
                    }
                    else
                    {
                        await _connection.WaitingForReconnect(game.GameId, (int)StateType.NoOneIsConnected);
                    }
                }
                if (id == game.PlayerTwo.Id)
                {
                    if (game.StateId == (int)StateType.Started)
                    {
                        await _connection.WaitingForReconnect(game.GameId, (int)StateType.PlayerOneIsConnected);
                    }
                    else
                    {
                        await _connection.WaitingForReconnect(game.GameId, (int)StateType.NoOneIsConnected);
                    }
                }
            }
            var rejoinableGames = games.Where(x => x.PlayerTwo.Id != 0).ToList();
            if (_rejoinableGames.ContainsKey(id))
            {
                _rejoinableGames.Remove(id, out var _);
            }

            _rejoinableGames.TryAdd(id, rejoinableGames);

            if (_users[id].Count > 1)
            {
                _users[id].Remove(connId);
            }
            else
            {
                _users.Remove(id, out var _);
            }

            //--after timer
            int i = 0;
            while (!_users.ContainsKey(id))
            {
                await Task.Delay(1000);
                i++;
                if (i==20)
                {
                    games.Where(x => x.PlayerTwo.Id == 0).ToList().ForEach(x => _connection.WaitingForReconnect(x.GameId, (int)StateType.Cancelled));
                   
                }
                
            }


            int j = 0;
            while (_rejoinableGames[id].Count > 0)
            {
                await Task.Delay(1000);
                j++;

                if (j >= 20 - i) 
                {
                    foreach (var game in _rejoinableGames[id])
                    {
                        var gamesDTOs = mapper.Map<IEnumerable<GameDTO>>(_rejoinableGames[id]);
                        await Clients.Caller.SendAsync("gamesforreconnect", gamesDTOs);
                        int opponentId = game.PlayerOne.Id == id ? game.PlayerTwo.Id : game.PlayerOne.Id;
                        var match = _connection.GetActiveMatch(game.GameId).Result;
                        match.WinnerId = opponentId;
                        match.StateId = (int)StateType.Finished;
                        _connection.MatchEnd(match);
                        game.Winner_Player_id = match.WinnerId;
                        game.StateId = (int)StateType.Finished;
                        _connection.GameEnd(game);
                        await Clients.User(opponentId.ToString()).SendAsync("gameend", game.GameId, game.PlayerOneScore, game.PlayerTwoScore, "opponent disconnected, you are a winner");
                    }
                }
            }

            var availableGames = _connection.GetGames().Result.Where(x => x.StateId == (int)StateType.Created);
            await Clients.All.SendAsync("getallgame", mapper.Map<IEnumerable<GameDTO>>(availableGames));

          
            //--after timer


            await base.OnDisconnectedAsync(exception);
        }


        public async Task CreateGame(int boardSize, int scoreTarget)
        {
            //int boardSize = request.BoardSize;
            //int scoreTarget = request.ScoreTarget;

            //if (boardSize < 3)
            //{
            //    await Clients.Caller.SendAsync("ongamecreate", -1, "boardsize cannot be less than 3");
            //    return;
            //}
            //else if (scoreTarget < 1)
            //{
            //    await Clients.Caller.SendAsync("ongamecreate", -1, "target score cannot be less than 1");
            //    return;
            //}
            //else
            //{
            //await Clients.Caller.SendAsync("ongamecreate", 1, "success");
            //}
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
            await Clients.Caller.SendAsync("getcurrentgame", gameDTO);

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
                await Clients.Caller.SendAsync("ongamejoin", -1, "game not found", "");
                return;
            }
            if (game.PlayerTwo.Id != 0)
            {
                await Clients.Caller.SendAsync("ongamejoin", -1, "this game is already started", "");
                return;
            }
            if (game.StateId == (int)StateType.NoOneIsConnected)
            {
                await Clients.Caller.SendAsync("ongamejoin", -1, "game creator is disconnected, wait for reconnect", "");
                return;
            }
            if (game.PlayerOne.Id == playerTwoId)
            {
                await Clients.Caller.SendAsync("ongamejoin", -1, "you can not join to game which is created by you", "");
                return;
            }
            var join = _connection.JoinToGame(gameId, playerTwoId);


            await GameStart(gameId);


            await Clients.User(mainGame.PlayerOne.Id.ToString()).SendAsync("ongamejoin", 1, "opponent connected! your turn ", mainGame.PlayerOne.UserName);
            await Clients.User(mainGame.PlayerTwo.Id.ToString()).SendAsync("ongamejoin", 1, mainGame.PlayerOne.UserName + "`s turn", mainGame.PlayerTwo.UserName);




        }
        private async Task GameStart(int gameId)
        {
            _connection.GameStart(gameId);
            mainGame = _connection.GetGameByID(gameId).Result;
            var games = _connection.GetGames().Result;
            var gameDTO = mapper.Map<GameDTO>(mainGame);
            var gamesDTO = mapper.Map<IEnumerable<GameDTO>>(games.Where(x => x.StateId == (int)StateType.Created));
            await Clients.All.SendAsync("getallgame", gamesDTO);

            await Clients.Users(mainGame.PlayerOne.Id.ToString(), mainGame.PlayerTwo.Id.ToString()).SendAsync("getcurrentgame", gameDTO);

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
            Thread.Sleep(5000);
            await MatchStart(gameId);
            var ids = new List<string> { mainGame.PlayerOne.Id.ToString(), mainGame.PlayerTwo.Id.ToString() };
            await Clients.Users(ids).SendAsync("matchstart", "second match");
            await Clients.User(mainGame.PlayerOne.Id.ToString()).SendAsync("nextturn", 1, "your turn", -1, -1, "");
            await Clients.User(mainGame.PlayerTwo.Id.ToString()).SendAsync("nextturn", 1, mainGame.PlayerOne.UserName + "`s turn", -1, -1, "");
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
                await Clients.Caller.SendAsync("nextturn", -1, "match is not in active mode", -1, -1, "");
                return;
            }


            if (mainMatch.CurrentPlayerId != callerId)
            {
                await Clients.Caller.SendAsync("nextturn", -1, "wait for your turn", -1, -1, "");
                return;

            }
            mainMatch.PlayerOne = mainGame.PlayerOne;
            mainMatch.PlayerTwo = mainGame.PlayerTwo;
            mainMatch.GameGrid = _connection.FillGrid(mainMatch).Result;
            mainMatch.CurrentPlayer = mainMatch.CurrentPlayerId == mainMatch.PlayerOne.Id ? Mark.X : Mark.O;
            var makeMove = mainMatch.MakeMove(r, c);
            if (makeMove.ErrorCode != 1)
            {
                await Clients.Caller.SendAsync("nextturn", -1, makeMove.ErrorMessage, -1, -1, "");
                return;
            }

            var currentmove = mainMatch.CurrentPlayer == Mark.X ? "X" : "O";
            var caller = callerId == mainMatch.PlayerOne.Id ? mainMatch.PlayerOne : mainMatch.PlayerTwo;
            var opponent = caller == mainMatch.PlayerOne ? mainMatch.PlayerTwo : mainMatch.PlayerOne;
            if (mainMatch.MatchOver != true)
            {

                await Clients.Caller.SendAsync("nextturn", 1, opponent.UserName + "`s turn", r, c, currentmove);
                await Clients.User(opponent.Id.ToString()).SendAsync("nextturn", 1, "your turn", r, c, currentmove);
                _connection.MakeMove(mainMatch, r, c);
                var moves = _connection.GetMovesHistory(mainGame.GameId).Result;
                await _connection.UpdateBoardState(moves, mainMatch.Id);
                return;
            }
            else
            {
                await Clients.Caller.SendAsync("nextturn", 1, "match Over", r, c, currentmove);
                await Clients.User(opponent.Id.ToString()).SendAsync("nextturn", 1, "match over", r, c, currentmove);
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
                await Clients.Users(ids).SendAsync("matchend", mainMatch.PlayerOneScore, mainMatch.PlayerTwoScore, "it is a tie");
                await NextMatch(mainGame.GameId);
                // await MatchStart(mainGame.GameId);
                return;
            }
            if (mainGame.TargetScore != mainMatch.PlayerOneScore && mainGame.TargetScore != mainMatch.PlayerTwoScore)
            {

                await Clients.User(winner.Id.ToString()).SendAsync("matchend", mainGame.PlayerOneScore, mainGame.PlayerTwoScore, "you win the match!");
                await Clients.User(loser.Id.ToString()).SendAsync("matchend", mainGame.PlayerOneScore, mainGame.PlayerTwoScore, "you lose the match!");
                _connection.MatchEnd(mainMatch);

                await NextMatch(mainGame.GameId);
                //await MatchStart(mainGame.GameId);
            }

            else
            {
                _connection.MatchEnd(mainMatch);
                mainGame.Winner_Player_id = mainMatch.WinnerId;
                mainGame.StateId = (int)StateType.Finished;
                await Clients.User(winner.Id.ToString()).SendAsync("matchend", mainGame.PlayerOneScore, mainGame.PlayerTwoScore, "you win the match!");
                await Clients.User(loser.Id.ToString()).SendAsync("matchend", mainGame.PlayerOneScore, mainGame.PlayerTwoScore, "you lose the match!");
                Thread.Sleep(3000);
                await Clients.User(winner.Id.ToString()).SendAsync("gameend", mainGame.PlayerOneScore, mainGame.PlayerTwoScore, "you win the game!");
                await Clients.User(loser.Id.ToString()).SendAsync("gameend", mainGame.PlayerOneScore, mainGame.PlayerTwoScore, "you lose the game!");
                _connection.GameEnd(mainGame);
                var games = _connection.GetGames().Result;
                var availableGames = games.Where(x => x.StateId == 1);
                var gamesDTOS = mapper.Map<IEnumerable<GameDTO>>(availableGames);
                await Clients.All.SendAsync("getallgame", gamesDTOS);

            }
        }

    }
}
