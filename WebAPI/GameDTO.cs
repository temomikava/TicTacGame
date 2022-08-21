using GameLibrary;

namespace WebAPI
{
    public class GameDTO
    {
        public int Id { get; set; }
        public int StateId { get; set; }
        public string StateIdString
        {
            set
            {
                value = StateId == 1 ? "created" : StateId == 2 ? "started" : "";
            }
        }
        public Player PlayerOne { get; set; }
        public Player PlayerTwo { get; set; }
        public int PlayerOneScore { get; set; }
        public int PlayerTwoScore { get; set; }
        public int Winner_Player_id { get; set; }
        public int TargetScore { get; set; }
        public int BoardSize { get; set; }
        public string BoardSizeString
        {
            set
            {
                value = BoardSize.ToString();
            }
        }
    }
}

