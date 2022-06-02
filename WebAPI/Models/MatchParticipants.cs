namespace WebAPI.Models
{
    public class MatchParticipants
    {
        public int Id { get; set; }
        public int MatchupId { get; set; }
        public HashSet<int>UserId { get; set; }
    }
}
