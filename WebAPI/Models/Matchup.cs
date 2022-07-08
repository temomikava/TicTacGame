using System;
using GameLibrary;

namespace WebAPI.Models
{
    public class Matchup
    {
        public int Id { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; } = null;
        public DateTime? FinishedAt { get; set; } = null;
        public int StateId { get; set; }
        public int Winner_Player_id { get; set; }
        public int Points { get; set; }
        public int BoardSize { get; set; }




    }
}
