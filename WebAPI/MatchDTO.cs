namespace WebAPI
{
    public class MatchDTO
    {
        public int Id { get; set; }
        public int WinnerId { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime FinishedAt { get; set; }
        public int GameId { get; set; }
        public int StateId { get; set; }
        public int TurnsPassed { get; set; }
        public int[] BoardState { get; set; }

    }
}
