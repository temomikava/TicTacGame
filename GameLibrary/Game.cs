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
        public DateTime CreatedAt { get; set; }= DateTime.Now;
        public DateTime FineshedAt { get; set; }

        public static int BoardSize { get; private set; }
        public Game(int boardSize)
        {
            BoardSize = boardSize;
        }
        
        public List<Match> Matches { get; set; }
        

    }
}
