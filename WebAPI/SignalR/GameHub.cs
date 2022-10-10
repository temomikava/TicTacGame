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
using Newtonsoft.Json;

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
        private static ConcurrentDictionary<int, HashSet<string>> _hubConnections = new ConcurrentDictionary<int, HashSet<string>>();
        public override async Task OnConnectedAsync()
        {
            int id = int.Parse(Context.User.Claims.First(x => x.Type == ClaimTypes.Name).Value);
            string connid = Context.User.Claims.First(x => x.Type == ClaimTypes.Role).Value;
            await _connection.OnConnected(id);



            _users.TryAdd(id, new HashSet<string>() );
            _hubConnections.TryAdd(id, new HashSet<string> ());
            if (_hubConnections[id].Count()==0)
            {
                _hubConnections[id].Add(Context.ConnectionId);
                _users[id].Add(connid);
            }
            if ( !_users[id].Contains(connid)  )
            {
                
                await Clients.Client(_hubConnections[id].First()).SendAsync("disconnect", "you are connected from another session");

            }
            _users[id].Add(connid);
            _hubConnections[id].Add(Context.ConnectionId);




            var gamesHistory = _connection.GetGamesHistory(id).Result;
            var gamesHistoryDTO = mapper.Map<List<GameDTO>>(gamesHistory);
            var history = new List<MatchDTO>();


            while (history.Count() != 5)
            {
                foreach (var game in gamesHistoryDTO)
                {
                    var matches = _connection.GetMatchesHistory(game.GameId);


                    foreach (var match in matches)
                    {
                        if (history.Count() != 5)
                        {
                            match.YourName = id == game.PlayerOne.Id ? game.PlayerOne.UserName : game.PlayerTwo.UserName;
                            match.OpponentName = match.YourName == game.PlayerOne.UserName ? game.PlayerTwo.UserName : game.PlayerOne.UserName;
                            match.TargetScore = game.TargetScore;
                            match.YourScore = id == game.PlayerOne.Id ? game.PlayerOneScore : game.PlayerTwoScore;
                            match.OpponentScore = id == game.PlayerOne.Id ? game.PlayerTwoScore : game.PlayerOneScore;
                            match.BoardSize = game.BoardSize;
                            match.WinnerName = match.WinnerId == game.PlayerOne.Id ? game.PlayerOne.UserName : match.WinnerId == -1 ? "" : game.PlayerTwo.UserName;
                            history.Add(match);
                        }
                    }

                    if (history.Count() == 5)
                    {
                        break;
                    }
                }
                break;
            }

            await Clients.User(id.ToString()).SendAsync("gethistory", history);

            var games = _connection.GetGames().Result.Where(x => x.PlayerOne.Id == id || x.PlayerTwo.Id == id);
            games.Where(x => x.PlayerTwo.Id == 0).ToList().ForEach(x => _connection.WaitingForReconnect(x.GameId, (int)StateType.Created));
            var gamess = _connection.GetGames().Result.Where(x => x.StateId == (int)StateType.Created);
            var gamesDTO = mapper.Map<IEnumerable<GameDTO>>(gamess);
            await Clients.All.SendAsync("getallgame", gamesDTO);

            //var gamesForReconnect = games.Where(x => x.StateId != (int)StateType.Started && x.PlayerTwo.Id != 0).ToList();
            var gamesForReconnect = (from game in games
                                     where game.PlayerTwo.Id != 0
                                     select game).ToList();

            if (gamesForReconnect.Count() > 0)
            {
                var gamesForReconnectDTO = mapper.Map<IEnumerable<GameDTO>>(gamesForReconnect);
                await Clients.Caller.SendAsync("gamesforreconnect", gamesForReconnectDTO);
            }
            await base.OnConnectedAsync();
        }

        public async Task Reconnect(ReconnectRequest request)
        {
            int gameId = request.GameId;
            int id = int.Parse(Context.User.Claims.First(x => x.Type == ClaimTypes.Name).Value);

            var game = _connection.GetGameByID(gameId).Result;
            if (game == null)
            {
                await Clients.Caller.SendAsync("ongamerejoin", -1, $"game with id: {gameId} not found", null, null, null, 0);
                return;
            }
            if (game.PlayerOne.Id != id && game.PlayerTwo.Id != id)
            {
                await Clients.Caller.SendAsync("ongamerejoin", -1, "you are not a member of this game", null, null, null, 0);
                return;
            }
            if (game.StateId == (int)StateType.Started)
            {
                await Clients.Caller.SendAsync("ongamerejoin", -1, "this game is already started", null, null, null, 0);
                return;
            }
            if ((game.StateId == (int)StateType.PlayerOneIsConnected && id == game.PlayerOne.Id) || (game.StateId == (int)StateType.PlayerTwoIsConnected && id == game.PlayerTwo.Id))
            {
                await Clients.Caller.SendAsync("ongamerejoin", -1, "you are already reconnected to this game", null, null, null, 0);
                return;
            }
            if (game.StateId == (int)StateType.Finished)
            {
                await Clients.Caller.SendAsync("ongamerejoin", -1, "this game is already finished", null, null, null, 0);
                return;
            }
            if (game.StateId == (int)StateType.Created)
            {
                await Clients.Caller.SendAsync("ongamerejoin", -1, "this game is in created state,you cannot rejoin", null, null, null, 0);
                return;
            }
            var gameDTO = mapper.Map<GameDTO>(game);
            var match = _connection.GetActiveMatch(gameId).Result;
            var currentPlayerId = match.CurrentPlayerId;
            var movesHistory = _connection.GetMovesHistory(game.GameId).Result;
            var username = game.PlayerOne.Id == id ? game.PlayerOne.UserName : game.PlayerTwo.UserName;
            await Clients.Caller.SendAsync("ongamerejoin", 1, "success", gameDTO, movesHistory, username, currentPlayerId);
            if (id == game.PlayerOne.Id)
            {
                if (game.StateId == (int)StateType.PlayerTwoIsConnected)
                {
                    await _connection.WaitingForReconnect(game.GameId, (int)StateType.Started);
                    await Clients.User(game.PlayerTwo.Id.ToString()).SendAsync("alert", 1, "opponent connected! ");

                }
                else if (game.StateId == (int)StateType.NoOneIsConnected)
                {
                    await _connection.WaitingForReconnect(game.GameId, (int)StateType.PlayerOneIsConnected);
                }
            }
            if (id == game.PlayerTwo.Id)
            {
                if (game.StateId == (int)StateType.PlayerOneIsConnected)
                {
                    await _connection.WaitingForReconnect(game.GameId, (int)StateType.Started);
                    await Clients.User(game.PlayerOne.Id.ToString()).SendAsync("alert", 1, "opponent connected! ");

                }
                else if (game.StateId == (int)StateType.NoOneIsConnected)
                {
                    await _connection.WaitingForReconnect(game.GameId, (int)StateType.PlayerTwoIsConnected);
                }
            }
            if (_rejoinableGames.ContainsKey(id))
            {
                var num = _rejoinableGames[id].RemoveAll(x => x.GameId == game.GameId);
                var gamesDTOs = mapper.Map<IEnumerable<GameDTO>>(_rejoinableGames[id]);
                await Clients.Caller.SendAsync("gamesforreconnect", gamesDTOs);
            }

        }
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            string connId = Context.User.Claims.First(x => x.Type == ClaimTypes.Role).Value;
            int id = int.Parse(Context.User.Claims.First(x => x.Type == ClaimTypes.Name).Value);


            _users[id].Clear();
            _hubConnections[id].Clear();


            await _connection.Ondisconnected(id);

            var games = _connection.GetGames().Result.Where(x => x.PlayerOne.Id == id || x.PlayerTwo.Id == id);
            games.Where(x => x.PlayerTwo.Id == 0).ToList().ForEach(x => _connection.WaitingForReconnect(x.GameId, (int)StateType.NoOneIsConnected));
            foreach (var game in games.Where(x => x.PlayerTwo.Id != 0))
            {
                if (id == game.PlayerOne.Id)
                {
                    if (game.StateId == (int)StateType.Started)
                    {
                        await _connection.WaitingForReconnect(game.GameId, (int)StateType.PlayerTwoIsConnected);
                        await Clients.User(game.PlayerTwo.Id.ToString()).SendAsync("alert", -1, "opponent disconnected, wait for reconnect! ");
                    }
                    else if (game.StateId == (int)StateType.PlayerOneIsConnected)
                    {
                        await _connection.WaitingForReconnect(game.GameId, (int)StateType.NoOneIsConnected);
                    }
                }
                if (id == game.PlayerTwo.Id)
                {
                    if (game.StateId == (int)StateType.Started)
                    {
                        await _connection.WaitingForReconnect(game.GameId, (int)StateType.PlayerOneIsConnected);
                        await Clients.User(game.PlayerOne.Id.ToString()).SendAsync("alert", -1, "opponent disconnected, wait for reconnect! ");

                    }
                    else if (game.StateId == (int)StateType.PlayerTwoIsConnected)
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



            //--after timer


            games.Where(x => x.PlayerTwo.Id == 0).ToList().ForEach(x => _connection.WaitingForReconnect(x.GameId, (int)StateType.Cancelled));


            int j = 0;
            while (_rejoinableGames[id].Count > 0)
            {
                await Task.Delay(1000);
                j++;

                if (j >= 25)
                {
                    foreach (var game in _rejoinableGames[id])
                    {
                        int opponentId = game.PlayerOne.Id == id ? game.PlayerTwo.Id : game.PlayerOne.Id;
                        var match = _connection.GetActiveMatch(game.GameId).Result;
                        if (match != null)
                        {

                            // await Clients.User(opponentId.ToString()).SendAsync("gameend", mainGame.GameId, mainGame.PlayerOneScore, mainGame.PlayerTwoScore, mainGame.PlayerOne.UserName, mainGame.PlayerTwo.UserName, "opponent disconnected, you are a winner");

                            match.WinnerId = opponentId;
                            match.StateId = (int)StateType.Finished;
                            _connection.MatchEnd(match);
                            game.Winner_Player_id = match.WinnerId;
                            game.StateId = (int)StateType.Finished;
                            _connection.GameEnd(game);
                            await Clients.User(opponentId.ToString()).SendAsync("alert", 2, "opponent did not reconnect, you win the game!");

                        }
                    }
                    var rejoinablegames = _connection.GetGames().Result.Where(x => (x.PlayerOne.Id == id || x.PlayerTwo.Id == id) && x.StateId != (int)StateType.Started && x.PlayerTwo.Id != 0);
                    var gamesDTOs = mapper.Map<IEnumerable<GameDTO>>(rejoinablegames).ToList();
                    await Clients.User(id.ToString()).SendAsync("gamesforreconnect", gamesDTOs);

                    break;
                }
            }
            var availableGames = _connection.GetGames().Result;
            await Clients.All.SendAsync("getallgame", mapper.Map<IEnumerable<GameDTO>>(availableGames.Where(x => x.StateId == (int)StateType.Created)));

            //--after timer

            await base.OnDisconnectedAsync(exception);
        }


        public void CreateGame(CreateGameRequest request)
        {
            int boardSize = request.BoardSize;
            int scoreTarget = request.ScoreTarget;

            int playerOneId = int.Parse(Context.User.Claims.First(x => x.Type == ClaimTypes.Name).Value);
            if (boardSize < 3 || scoreTarget < 1)
            {
                return;
            }
            mainGame = new Game();
            mainGame.CreatedAt = DateTime.Now;
            mainGame.BoardSize = boardSize;
            mainGame.TargetScore = scoreTarget;
            mainGame.StateId = (int)StateType.Created;
            mainGame.PlayerOne = new Player { Id = playerOneId };
            mainGame.PlayerOne.UserName = _connection.GetUsername(playerOneId).Result.Username;
            mainGame.GameId = _connection.GameCreate(mainGame).Result.gameId;
            var games = _connection.GetGames().Result.Where(x => x.StateId == (int)StateType.Created);

            var gamesDTO = mapper.Map<IEnumerable<GameDTO>>(games);
            Clients.All.SendAsync("getallgame", gamesDTO);
        }

        public void JoinToGame(JoinToGameRequest request)
        {
            int gameId = request.GameId;
            int playerTwoId = int.Parse(Context.User.Claims.First(x => x.Type == ClaimTypes.Name).Value);
            var game = _connection.GetGameByID(gameId).Result;
            if (game == null)
            {
                Clients.Caller.SendAsync("ongamejoin", -1, gameId, $"game with id {gameId} not found", "");
                return;
            }
            if (game.PlayerTwo.Id != 0)
            {
                Clients.Caller.SendAsync("ongamejoin", -1, gameId, "this game is already started", "");
                return;
            }
            if (game.StateId == (int)StateType.NoOneIsConnected)
            {
                Clients.Caller.SendAsync("ongamejoin", -1, gameId, "game creator is disconnected, wait for reconnect", "");
                return;
            }
            if (game.PlayerOne.Id == playerTwoId)
            {
                Clients.Caller.SendAsync("ongamejoin", -1, gameId, "you can not join to game which is created by you", "");
                return;
            }
            var join = _connection.JoinToGame(gameId, playerTwoId);

            GameStart(gameId);

            Clients.User(mainGame.PlayerOne.Id.ToString()).SendAsync("ongamejoin", 1, gameId, "opponent connected! your turn ", mainGame.PlayerOne.UserName);
            Clients.User(mainGame.PlayerTwo.Id.ToString()).SendAsync("ongamejoin", 1, gameId, mainGame.PlayerOne.UserName + "`s turn", mainGame.PlayerTwo.UserName);


        }

        private void GameStart(int gameId)
        {
            _connection.GameStart(gameId);
            mainGame = _connection.GetGameByID(gameId).Result;
            var games = _connection.GetGames().Result;
            var gamesDTO = mapper.Map<IEnumerable<GameDTO>>(games.Where(x => x.StateId == (int)StateType.Created));
            Clients.All.SendAsync("getallgame", gamesDTO);
            var gameDTO = mapper.Map<GameDTO>(mainGame);
            Clients.User(mainGame.PlayerOne.Id.ToString()).SendAsync("getcurrentgame", gameDTO, mainGame.PlayerOne.Id);
            Clients.User(mainGame.PlayerTwo.Id.ToString()).SendAsync("getcurrentgame", gameDTO, mainGame.PlayerTwo.Id);
            MatchStart(gameId);
        }

        private void MatchStart(int gameId)
        {
            var match = new Match();
            match.GameId = gameId;
            match.StateId = (int)StateType.Started;
            match.CurrentPlayerId = mainGame.PlayerOne.Id;
            match.StartedAt = DateTime.Now;
            mainMatch = match;
            _connection.MatchStart(match);
        }

        public void NextMatch(int gameId)
        {
            Thread.Sleep(3000);
            MatchStart(gameId);
            var ids = new List<string> { mainGame.PlayerOne.Id.ToString(), mainGame.PlayerTwo.Id.ToString() };
            Clients.Users(ids).SendAsync("matchstart", gameId);
            Clients.User(mainGame.PlayerOne.Id.ToString()).SendAsync("nextturn", 1, mainGame.GameId, "your turn", -1, -1, "");
            Clients.User(mainGame.PlayerTwo.Id.ToString()).SendAsync("nextturn", 1, mainGame.GameId, mainGame.PlayerOne.UserName + "`s turn", -1, -1, "");
        }

        public async Task MakeMove(MakeMoveRequest request)
        {
            int gameId = request.GameId;
            int r = request.Row;
            int c = request.Column;
            int callerId = int.Parse(Context.User.Claims.First(x => x.Type == ClaimTypes.Name).Value);
            mainGame = _connection.GetGameByID(gameId).Result;
            mainMatch = _connection.GetActiveMatch(gameId).Result;


            if (mainGame == null)
            {
                await Clients.Caller.SendAsync("nextturn", -1, -1, $"game  with id : {gameId} not found", -1, -1, "");
                return;
            }

            if (mainMatch == null)
            {
                await Clients.Caller.SendAsync("nextturn", -1, mainGame.GameId, $"game  with id : {gameId} is not in active state", -1, -1, "");
                return;
            }
            if (mainGame.StateId == (int)StateType.NoOneIsConnected)
            {
                await Clients.Caller.SendAsync("nextturn", -1, mainGame.GameId, "you are not connected to this game", -1, -1, "");
                return;
            }

            if (mainMatch.CurrentPlayerId != callerId)
            {
                await Clients.Caller.SendAsync("nextturn", -1, mainGame.GameId, "wait for your turn", -1, -1, "");
                return;
            }
            mainMatch.PlayerOne = mainGame.PlayerOne;
            mainMatch.PlayerTwo = mainGame.PlayerTwo;
            mainMatch.GameGrid = _connection.FillGrid(mainMatch).Result;
            mainMatch.CurrentPlayer = mainMatch.CurrentPlayerId == mainMatch.PlayerOne.Id ? Mark.X : Mark.O;
            var makeMove = mainMatch.MakeMove(r, c);
            if (makeMove.ErrorCode != 1)
            {
                await Clients.Caller.SendAsync("nextturn", makeMove.ErrorCode, mainGame.GameId, makeMove.ErrorMessage, -1, -1, "");
                return;
            }

            var currentmove = mainMatch.CurrentPlayer == Mark.X ? "X" : "O";
            var caller = callerId == mainMatch.PlayerOne.Id ? mainMatch.PlayerOne : mainMatch.PlayerTwo;
            var opponent = caller == mainMatch.PlayerOne ? mainMatch.PlayerTwo : mainMatch.PlayerOne;
            if (mainMatch.MatchOver != true)
            {

                await Clients.Caller.SendAsync("nextturn", 1, mainGame.GameId, opponent.UserName + "`s turn", r, c, currentmove);
                await Clients.User(opponent.Id.ToString()).SendAsync("nextturn", makeMove.ErrorCode, mainGame.GameId, "your turn", r, c, currentmove);
                _connection.MakeMove(mainMatch, r, c);
                var moves = _connection.GetMovesHistory(mainGame.GameId).Result;
                await _connection.UpdateBoardState(moves, mainMatch.Id);
                return;
            }
            else
            {
                await Clients.Caller.SendAsync("nextturn", 1, mainGame.GameId, "match Over", r, c, currentmove);
                await Clients.User(opponent.Id.ToString()).SendAsync("nextturn", makeMove.ErrorCode, mainGame.GameId, "match over", r, c, currentmove);
                _connection.MakeMove(mainMatch, r, c);
                var moves = _connection.GetMovesHistory(mainGame.GameId).Result;
                await _connection.UpdateBoardState(moves, mainMatch.Id);
            }
            MatchEnd();
        }
        private void MatchEnd()
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
                Clients.Users(ids).SendAsync("matchend", mainGame.GameId, mainGame.PlayerOneScore, mainGame.PlayerTwoScore, mainGame.PlayerOne.UserName, mainGame.PlayerTwo.UserName, "it is a tie");
                NextMatch(mainGame.GameId);
                return;
            }
            if (mainGame.TargetScore != mainMatch.PlayerOneScore && mainGame.TargetScore != mainMatch.PlayerTwoScore)
            {
                Clients.User(winner.Id.ToString()).SendAsync("matchend", mainGame.GameId, mainGame.PlayerOneScore, mainGame.PlayerTwoScore, mainGame.PlayerOne.UserName, mainGame.PlayerTwo.UserName, "you win the match!");
                Clients.User(loser.Id.ToString()).SendAsync("matchend", mainGame.GameId, mainGame.PlayerOneScore, mainGame.PlayerTwoScore, mainGame.PlayerOne.UserName, mainGame.PlayerTwo.UserName, "you lose the match!");

                _connection.MatchEnd(mainMatch);
                NextMatch(mainGame.GameId);
            }

            else
            {
                _connection.MatchEnd(mainMatch);
                mainGame.Winner_Player_id = mainMatch.WinnerId;
                mainGame.StateId = (int)StateType.Finished;

                Clients.User(winner.Id.ToString()).SendAsync("matchend", mainGame.GameId, mainGame.PlayerOneScore, mainGame.PlayerTwoScore, mainGame.PlayerOne.UserName, mainGame.PlayerTwo.UserName, "you win the match!");
                Clients.User(loser.Id.ToString()).SendAsync("matchend", mainGame.GameId, mainGame.PlayerOneScore, mainGame.PlayerTwoScore, mainGame.PlayerOne.UserName, mainGame.PlayerTwo.UserName, "you lose the match!");
                Thread.Sleep(2000);

                Clients.User(winner.Id.ToString()).SendAsync("gameend", mainGame.GameId, mainGame.PlayerOneScore, mainGame.PlayerTwoScore, mainGame.PlayerOne.UserName, mainGame.PlayerTwo.UserName, "you win the game!");
                Clients.User(loser.Id.ToString()).SendAsync("gameend", mainGame.GameId, mainGame.PlayerOneScore, mainGame.PlayerTwoScore, mainGame.PlayerOne.UserName, mainGame.PlayerTwo.UserName, "you lose the game!");
                _connection.GameEnd(mainGame);
                var games = _connection.GetGames().Result;
                var availableGames = games.Where(x => x.StateId == 1);
                var gamesDTOS = mapper.Map<IEnumerable<GameDTO>>(availableGames);
                Clients.All.SendAsync("getallgame", gamesDTOS);
            }
        }

    }
}
