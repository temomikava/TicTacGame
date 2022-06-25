namespace WebAPI.Models
{
    public class Matchup
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime FinishedAt { get; set; }
        public DateTime FinishidAt { get; set; }
        public int StateId { get; set; }
        public int Winner_Player_id { get; set; }
        public int Points { get; set; }



    }
}
