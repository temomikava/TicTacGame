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

namespace WebAPI.SignalR
{
    
    public class GameHub : Hub
    {
        IDatabaseConnection _connection;
        public GameHub(IDatabaseConnection connection)
        {
            _connection=connection;
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


             
        public static List<Matchup> CreatedMatches = new List<Matchup>();
        
        

        public async Task CreateMatch(int boardSize, int scoreTarget)
        {
            string message = "success";
            if (boardSize < 3)
            {
                message = "board size cannot be less than 3";
                await Clients.Caller.SendAsync("onmatchcreate", message,-1);
            }
            else if (scoreTarget < 1)
            {
                message = "scoretarget cannot be less than 1";
                await Clients.Caller.SendAsync("onmatchcreate", message,-1);
            }
            else
            {
                Matchup match = new Matchup();
                match.CreatedAt=DateTime.Now;
                match.StateId = (int)StateType.Created;
                match.Points = scoreTarget;
                match.BoardSize = boardSize;
                _connection.CreateMatch(match);
                await Clients.Caller.SendAsync("onmatchcreate", message,1);

            }
        }
        public async Task SendAllMatches()
        {
            List<Matchup> matches = _connection.GetAllMatches();
            await Clients.All.SendAsync("getallmatches", matches);

        }
       
        public async Task JoinToGame()
        {

        }
        public async Task MakeMove(int r, int c)
        {

        }
        public async Task StartMatch()
        {

        }
        public async Task Notify(string messagge)
        {
            await Clients.All.SendAsync("ReceiveMessage","from server " +messagge);
        }


        //public string GetConnectionId() => Context.ConnectionId;
        //public async Task AddToGroup(string groupName)
        // =>    await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        

    }
}
