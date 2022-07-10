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
       private IDatabaseConnection _connection;
       private IMatch _match;
        public GameHub(IDatabaseConnection connection,IMatch match)
        {
            _connection=connection;
            _match=match;
        }
        private static ConcurrentDictionary<string, HashSet<string>> _users = new ConcurrentDictionary<string, HashSet<string>>();

        public override async Task OnConnectedAsync()
        {

            var id = Context.User.Claims.First(x => x.Type == ClaimTypes.Name).Value;
            var connid = Context.ConnectionId;
            if (_users.ContainsKey(id))
            {
                _users[id].Add(connid);
            }
            else
            {
                _users.TryAdd(id, new HashSet<string>() { connid });
            }
            
            

        }


        public override async Task OnDisconnectedAsync(Exception? exception)
        {

            var id = Context.User.Claims.First(x => x.Type == ClaimTypes.Name).Value;
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
            string message = "success";
            if (boardSize < 3)
            {
                message = "board size cannot be less than 3";
                await Clients.Caller.SendAsync("ongamecreate", message,-1);
            }
            else if (scoreTarget < 1)
            {
                message = "scoretarget cannot be less than 1";
                await Clients.Caller.SendAsync("ongamecreate", message,-1);
            }
            else
            {
                //TODO: get  game creator id 
                //game.PlayerOneID=
                _match.CreatedAt=DateTime.Now;
                _match.StateId = (int)StateType.Created;
                _match.TargetScore = scoreTarget;
                _match.BoardSize = boardSize;
                _match.GameGrid = new Mark[boardSize, boardSize];
                var data=_connection.SaveGameToDb(_match);
                if (data.ErrorCode==1)
                {
                   await SendGameToAllClient(data.GameId);

                }
                else
                {
                    message = "failed to create game";
                    await Clients.Caller.SendAsync("ongamecreate", message, -1);

                    return;
                }

                await Clients.Caller.SendAsync("ongamecreate", message, 1);

            }
        }
        private async Task SendGameToAllClient( int gameId)
        {
            List<IMatch> games = _connection.GetGames();
            IMatch? game=games.FirstOrDefault(game => game.Id == gameId);
            await Clients.All.SendAsync("getgame", game);

        }
       
        public async Task JoinToGame(int gameId)
        {
            var games = _connection.GetGames();
            var game=games.FirstOrDefault(game => game.Id == gameId);
            game.StartedAt = DateTime.Now;
            game.StateId = (int)StateType.Started;
            //TODO: get player two id
            //game.PlayerTwoID=
            _connection.JoinToGame(game);

            games.Remove(game);
            await Clients.All.SendAsync("updategames",games);
            await GameStart(game);
        }
        public async Task MakeMove(int r, int c)
        {
            var move=_match.MakeMove(r, c);
            await Clients.Caller.SendAsync("onmovemade", move.ErrorMessage, move.ErrorCode, _match.MatchOver);
            if (_match.MatchOver)
            {
               await MatchEnd();
            }
        }
        private async Task MatchStart(IMatch match)
        {
            _connection.MatchStart(match);
        }
        public async Task MatchEnd()
        {
            _match.FinishedAt = DateTime.Now;
            //TODO:get winner id
            //_match.Winner_Player_id=
            _connection.MatchEnd(_match);
            if (_match.MatchResult.Winner==Mark.X)
            {
                _match.PlayerOneScore += 1;
            }
            if (_match.MatchResult.Winner == Mark.O)
            {
                _match.PlayerTwoScore += 1;
            }
            if (_match.PlayerOneScore == _match.TargetScore || _match.PlayerTwoScore == _match.TargetScore)
            {
                await GameEnd();
            }
            else
            {

            }
            await Clients.Caller.SendAsync("getmatchresult",_match);
        }
        public async Task GameStart(IMatch game)
        {
           await MatchStart(_match);
        }
        public async Task GameEnd()
        {

        }
        public async Task<int> Notify(string messagge)
        {
            await Clients.All.SendAsync("ReceiveMessage","from server " +messagge);
            return 888;
        }


        //public string GetConnectionId() => Context.ConnectionId;
        //public async Task AddToGroup(string groupName)
        // =>    await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        

    }
}
