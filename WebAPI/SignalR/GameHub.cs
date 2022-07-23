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
            await Clients.All.SendAsync("getallgame", games);
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
                if (game.PlayerTwo.Id!=0)
                {
                    var opponentId = id == game.PlayerOne.Id ? game.PlayerTwo.Id : game.PlayerOne.Id;

                    await Clients.User(opponentId.ToString()).SendAsync("ongamecreate",-1, "opponent disconnected!");

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

        
        public async Task CreateGame(int boardSize, int scoreTarget)
        {
           //int boardSize = request.BoardSize;
           
           //int scoreTarget = request.ScoreTarget;
            if (boardSize<3)
            {
                await Clients.Caller.SendAsync("ongamecreate",-1, "boardsize cannot be less than 3");
                return;
            }
            else if (scoreTarget<1)
            {
                await Clients.Caller.SendAsync("ongamecreate",-1, "target score cannot be less than 1");
                return;
            }
            else
            {
                await Clients.Caller.SendAsync("ongamecreate", 1, "success");
            }
            int playerOneId= int.Parse(Context.User.Claims.First(x => x.Type == ClaimTypes.Name).Value);
            mainGame = new Game();
            mainGame.CreatedAt = DateTime.Now;
            mainGame.BoardSize = boardSize;
            mainGame.TargetScore = scoreTarget;
            mainGame.StateId = (int)StateType.Created;
            mainGame.PlayerOne = new Player { Id=playerOneId};
            mainGame.PlayerOne.UserName = _connection.GetUsername(playerOneId).Username;
            mainGame.Id=_connection.GameCreate(mainGame).GameId;
            var games = _connection.GetGames();
            await Clients.All.SendAsync("getallgame", games);
            //var waitingForOponent = new WaitingForOponent(mainGame.Id, _connection.GetActiveMatch);
            //mainMatch = waitingForOponent.Waiting();

        }

        public async Task JoinToGame(int gameId)
        {
            //int gameId = request.GameId;
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

                List<string>userids=new List<string> { mainGame.PlayerOne.Id.ToString(),mainGame.PlayerTwo.Id.ToString() };
                await Clients.User(mainGame.PlayerOne.Id.ToString()).SendAsync("ongamejoin","your turn turn");
                await Clients.User(mainGame.PlayerTwo.Id.ToString()).SendAsync("ongamejoin",mainGame.PlayerTwo.UserName +"`s turn");
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
        
        public async Task MakeMove(int gameId,int r, int c)
        {
            //int gameId=request.GameId;
            //int r = request.Row;
            //int c = request.Column;
            Match.MoveMade += Match_MoveMade;
            Match.MatchEnded += Match_MatchEnded;
            mainGame = _connection.GetGameByID(gameId);
            mainMatch=_connection.GetActiveMatch(gameId);
            if (mainMatch==null)
            {
               await Clients.Caller.SendAsync("onmovemade", "wait for oponent",-1);
               return;
            }
            //mainMatch.PlayerOne = new Player { Id = mainGame.PlayerOne.Id };
            //mainMatch.PlayerTwo = new Player { Id = mainGame.PlayerTwo.Id };
            mainMatch.GameGrid = _connection.FillGrid(mainMatch);
            if (mainMatch.CurrentPlayerId!= int.Parse(Context.User.Claims.First(x => x.Type == ClaimTypes.Name).Value))
            {
                await Clients.Caller.SendAsync("onmovemade", "wait for your turn",-1);
               
            }
            else
            {
                mainMatch.CurrentPlayer = mainMatch.CurrentPlayerId == mainMatch.PlayerOne.Id ? Mark.X : Mark.O;
                var makeMove=mainMatch.MakeMove(r, c);
                if (makeMove.ErrorCode!=1)
                {
                    await Clients.Caller.SendAsync("onmovemade", makeMove.ErrorMessage, makeMove.ErrorCode);
                }                             
            }

            Match.MoveMade -= Match_MoveMade;
            Match.MatchEnded -= Match_MatchEnded;
        }

        private async void Match_MatchEnded(MatchResult result)
        {
            List<string> ids = new List<string> { mainGame.PlayerOne.Id.ToString(), mainGame.PlayerTwo.Id.ToString() };
            if (result.Winner==Mark.X)
            {
                mainMatch.WinnerId = mainGame.PlayerOne.Id;
                mainGame.PlayerOneScore++;
                mainMatch.PlayerOneScore = mainGame.PlayerOneScore;
                _connection.MatchEnd(mainMatch);
               
            }
            else if (result.Winner==Mark.O)
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
            
            var winnerId=mainMatch.WinnerId;
            var loserId = winnerId == mainGame.PlayerOne.Id ? mainGame.PlayerOne.Id : winnerId == mainGame.PlayerTwo.Id ? mainGame.PlayerTwo.Id : -1;
            if (mainGame.TargetScore!=mainGame.PlayerOneScore && mainGame.TargetScore!=mainGame.PlayerTwoScore)
            {
                if (winnerId!=-1)
                {
                    await Clients.User(winnerId.ToString()).SendAsync("matchend", mainGame.PlayerOneScore, mainGame.PlayerTwoScore, "you Win the match");
                    await Clients.User(loserId.ToString()).SendAsync("matchend", mainGame.PlayerOneScore, mainGame.PlayerTwoScore, "you lose the match!");
                }
                else
                {
                    await Clients.Users(ids).SendAsync("matchend", mainGame.PlayerOneScore, mainGame.PlayerTwoScore, "it's a tie");
                }
                await MatchStart(mainGame.Id);
            }
            else
            {
                mainGame.StateId = 3;
                mainGame.Winner_Player_id = mainMatch.WinnerId;
                _connection.GameEnd(mainGame);
                await Clients.User(winnerId.ToString()).SendAsync("gameend", mainGame.PlayerOneScore, mainGame.PlayerTwoScore, "you Win the game");
                await Clients.User(loserId.ToString()).SendAsync("gameend", mainGame.PlayerOneScore, mainGame.PlayerTwoScore, "you lose the game!");
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
