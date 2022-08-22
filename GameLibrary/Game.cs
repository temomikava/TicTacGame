using GameLibrary.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameLibrary
{
    public class Game
    {
        public int Id { get; set; }
        public Mark[,] GameGrid { get; set; }
        public List<Match> MatchList { get; set; }=new ();
        public DateTime? CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; } = null;
        public DateTime? FinishedAt { get; set; } = null;
        public int StateId { get; set; }
        public Player PlayerOne { get; set; }
        public Player PlayerTwo { get; set; }
        public int PlayerOneScore { get; set; } 
        public int PlayerTwoScore { get; set; } 
        public int Winner_Player_id { get; set; }
        public int TargetScore { get; set; }
        public int BoardSize { get; set; }
    }
}
