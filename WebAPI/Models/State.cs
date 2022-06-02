using WebAPI.Enums;

namespace WebAPI.Models
{
    public class State
    {
        public int Id { get; set; }
        public StateType GameState { get; set; }
    }
}
