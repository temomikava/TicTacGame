using GameLibrary.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameLibrary
{
    public interface IMatch
    {
        public Mark[,] GameGrid { get; set; }
        public Mark CurrentPlayer { get; set; } 
        public int TurnsPassed { get; set; } 
        public bool MatchOver { get; set; } 
        public WinInfo WinInfo { get; set; }
        public MatchResult MatchResult { get; set; }
        public (short ErrorCode, string ErrorMessage, bool matchend) MakeMove(int r, int c);
        public int Id { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; } 
        public DateTime? FinishedAt { get; set; }
        public int StateId { get; set; }
        public int PlayerOneID { get; set; }
        public int PlayerTwoID { get; set; }
        public int PlayerOneScore { get; set; }
        public int PlayerTwoScore { get; set; }
        public int Winner_Player_id { get; set; }
        public int TargetScore { get; set; }
        public int BoardSize { get; set; }


    }
}
