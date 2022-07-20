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
using GameLibrary.Helpers;

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
            await Clients.All.SendAsync("getallgame", games);
        }
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            int id = int.Parse(Context.User.Claims.First(x => x.Type == ClaimTypes.Name).Value);
            var games= _connection.GetGames();
            List<Game> connectedGames = new List<Game>();
            var createdgames = games.Where(x => x.StateId == (int)StateType.Created && x.PlayerOne.Id == id);
            var startedgames = games.Where(x => x.StateId == (int)StateType.Started && x.PlayerOne.Id == id || x.PlayerTwo.Id == id);
            foreach (var game in createdgames)
            {
                connectedGames.Add(game);
            }
            foreach (var game in startedgames)
            {
                connectedGames.Add(game);
                var opponentId=id==game.PlayerOne.Id ? game.PlayerTwo.Id : game.PlayerOne.Id;
                if (id==game.PlayerOne.Id)
                {
                    await Clients.User(opponentId.ToString()).SendAsync("ondisconnected", "opponent disconnected!");
                }
            }
            foreach (var game in connectedGames)
            {
                _connection.Ondisconnected(game.Id);
            }
            var availableGames = _connection.GetGames();
            await Clients.All.SendAsync("getallgame",availableGames);
                //games.SingleOrDefault(x => x.StateId == (int)StateType.Started && x.PlayerOne.Id == id || x.PlayerTwo.Id == id);
          
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


        public async Task CreateGame(int boardSize, int scoreTarget)
        {
            int playerOneId= int.Parse(Context.User.Claims.First(x => x.Type == ClaimTypes.Name).Value);
            mainGame = new Game();
            mainGame.CreatedAt = DateTime.Now;
            mainGame.BoardSize = boardSize;
            mainGame.TargetScore = scoreTarget;
            mainGame.StateId = (int)StateType.Created;
            mainGame.PlayerOne = new Player { Id=playerOneId};
            mainGame.PlayerOne.UserName = _connection.GetUsername(playerOneId).Username;
            mainGame.Id=_connection.GameCreate(mainGame).GameId;
            await Clients.All.SendAsync("ongamecreate", mainGame);
            //var waitingForOponent = new WaitingForOponent(mainGame.Id, _connection.GetActiveMatch);
            //mainMatch = waitingForOponent.Waiting();

        }

        public async Task JoinToGame(int gameId)
        {
            
            int playerTwoId = int.Parse(Context.User.Claims.First(x => x.Type == ClaimTypes.Name).Value);
            var join = _connection.JoinToGame(gameId, playerTwoId);

            if (join.ErrorCode!=1)

            {
               await Clients.Caller.SendAsync("ongamejoin", -1,join.ErrorMessage );
               return;
            }
            else
            {
                mainGame = _connection.GetGameByID(gameId);

                await Clients.Caller.SendAsync("ongamejoin", 1, mainGame.PlayerOne);
                List<string>userids=new List<string> { mainGame.PlayerOne.Id.ToString(),mainGame.PlayerTwo.Id.ToString() };
                await Clients.Users(userids).SendAsync("ongamejoin", mainGame.PlayerOne.UserName+"`s turn");
                    //(mainGame.PlayerOne.Id, mainGame.PlayerTwo.Id).SendAsync("ongamejoin", 1, mainGame.PlayerOne.UserName+"`s turn");
            }
            await GameStart(gameId);

        }
        private async Task GameStart(int gameId)
        {
            _connection.GameStart(gameId);
            await MatchStart(gameId);
        }
        private async Task MatchStart(int gameId)
        {
            var game=_connection.GetGameByID(gameId);
            mainMatch=new Match();
            mainMatch.GameId=gameId;
            mainMatch.StateId = (int)StateType.Started;
            mainMatch.CurrentPlayerId=game.PlayerOne.Id;
            mainMatch.StartedAt = DateTime.Now;
            _connection.MatchStart(mainMatch);

        }
        
        public async Task MakeMove(int gameId, int r, int c)
        {
            Match.MoveMade += Match_MoveMade;
            //Match.MatchRestarted += Match_MatchRestarted;
            Match.MatchEnded += Match_MatchEnded;
            mainGame = _connection.GetGameByID(gameId);
            mainMatch=_connection.GetActiveMatch(gameId);
            if (mainMatch==null)
            {
               await Clients.Caller.SendAsync("onmovemade", "wait for oponent",-1);
               return;
            }
            mainMatch.PlayerOne = new Player { Id = mainGame.PlayerOne.Id };
            mainMatch.PlayerTwo = new Player { Id = mainGame.PlayerTwo.Id };
            mainMatch.GameGrid = _connection.FillGrid(mainMatch);
            if (mainMatch.CurrentPlayerId!= int.Parse(Context.User.Claims.First(x => x.Type == ClaimTypes.Name).Value))
            {
                await Clients.Caller.SendAsync("onmovemade", "wait for your turn",-1);
                Match.MoveMade -= Match_MoveMade;
                Match.MatchEnded -= Match_MatchEnded;
                return;
            }
            else
            {
                var makeMove=mainMatch.MakeMove(r, c);
                
                await Clients.Caller.SendAsync("onmovemade", makeMove.ErrorMessage, makeMove.ErrorCode);
                
                
                Match.MoveMade -= Match_MoveMade;
                Match.MatchEnded -= Match_MatchEnded;
            }

            
        }

        private async void Match_MatchEnded(MatchResult obj)
        {
            if (obj.Winner==Mark.X)
            {
                mainMatch.WinnerId = mainGame.PlayerOne.Id;
                mainGame.PlayerOneScore++;
                mainMatch.PlayerOneScore = mainGame.PlayerOneScore;
                _connection.MatchEnd(mainMatch);
            }
            else if (obj.Winner==Mark.O)
            {
                mainMatch.WinnerId = mainGame.PlayerTwo.Id;
                mainGame.PlayerTwoScore++;
                mainMatch.PlayerTwoScore = mainGame.PlayerTwoScore;
                _connection.MatchEnd(mainMatch);
                

            }
            else
            {
                mainMatch.WinnerId = -1;
                _connection.MatchEnd(mainMatch);

            }
            if (mainGame.TargetScore!=mainGame.PlayerOneScore && mainGame.TargetScore!=mainGame.PlayerTwoScore)
            {
               await MatchStart(mainGame.Id);
            }
            else
            {
                mainGame.StateId = 3;
                mainGame.Winner_Player_id = mainMatch.WinnerId;
                _connection.GameEnd(mainGame);
            }
        }
        private void Match_MoveMade(int r, int c)
        {
            _connection.MakeMove(mainMatch, r, c);
        }

        private void Match_MatchRestarted()
        {
            throw new NotImplementedException();
        }

                     
    }
}
