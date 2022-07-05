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

namespace WebAPI.SignalR
{
    
    public class GameHub : Hub
    {
        private static ConcurrentDictionary<string, HashSet<string>> _users = new ConcurrentDictionary<string, HashSet<string>>();

        public override async Task OnConnectedAsync()
        {
            await Clients.All.SendAsync("receivemessage", "hello from server");
            //var id = Context.User.Claims.First(x => x.Type == ClaimTypes.Name).Value;
            //var connId = Context.ConnectionId;
            ////databaseConnection.GetUserId(connId);
            //if(!_users.ContainsKey(id))
            //{
            //    _users.TryAdd(id, new HashSet<string>() { connId });
            //}
            //else
            //{
            //    _users[id].Add(connId);
            //}

            //await base.OnConnectedAsync();
        }


        //public override async Task OnDisconnectedAsync(Exception? exception)
        //{
        //    var id = Context.User.Claims.First(x => x.Type == ClaimTypes.Name).Value;
        //    var connId = Context.ConnectionId;
        //    if (_users[id].Count>1)
        //    {
        //        _users[id].Remove(connId);
        //    }
        //    else
        //    {
        //        _users.Remove(id, out var _);
        //    }
        //    await base.OnDisconnectedAsync(exception);
        //}


        //public Task<bool> IsConnected { get; set; } 
        //public Task<bool> IsLoggedIn { get; set; }       
        public List<(uint boarsize, uint scoretarget)> AvailableBoards = new List<(uint boarsize, uint scoretarget)>();

        public async Task CreateBoard(uint boardSize, uint scoreTarget)
        {
            string message = "success";
            if (boardSize < 3)
            {
                message = "board size cannot be less than 3";
                await Clients.Caller.SendAsync("createboard", message);
            }
            else if (scoreTarget < 1)
            {
                message = "scoretarget cannot be less than 1";
                await Clients.Caller.SendAsync("getresponse", message);
            }
            else
            {
                AvailableBoards.Add((boardSize, scoreTarget));
                await Clients.Caller.SendAsync("boardcreate", message);
            }
        }
        public async Task GetAllBoards()
        {
            await Clients.All.SendAsync("getallboards", AvailableBoards);
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
