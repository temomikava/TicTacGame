namespace WebAPI.Requests
{
    public class CreateGameRequest
    {
        public int BoardSize { get; set; }
        public int ScoreTarget { get; set; }
    }
}
