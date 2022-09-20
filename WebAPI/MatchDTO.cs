namespace WebAPI
{
    public class MatchDTO
    {
        public int Id { get; set; }
        public int WinnerId { get; set; }
        //public DateTime StartedAt { get; set; }
        //public DateTime FinishedAt { get; set; }
        public int GameId { get; set; }
        //public int StateId { get; set; }
        //public int TurnsPassed { get; set; }
        public int[] BoardState { get; set; }
        public int YourScore { get; set; }
        public int OpponentScore { get; set; }
        public int TargetScore { get; set; }
        public int PlayerOneScore { get; set; }
        public int PlayerTwoScore { get; set; }
        public int BoardSize { get; set; }
        public string YourName { get; set; }
        public string OpponentName { get; set; }
        public string WinnerName { get; set; }


    }
}
