using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using WebAPI.Models;

namespace WebAPI.SignalR
{
    public class GameHub : Hub
    {
        public Task<bool> IsConnected { get; set; }
        public Task<bool> IsLoggedIn { get; set; }
        
        public List<(int boarsize, int scoretarget)> AvailableBoards { get; set; }
        public async Task CreateBoard(int boardSize, int scoreTarget)
        {
            AvailableBoards.Add((boardSize, scoreTarget));
            await Clients.All.SendAsync("getAllGames", AvailableBoards);
        }
    }
}
