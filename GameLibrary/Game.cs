using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameLibrary
{
    public class Game
    {
        public Game(int boardSize)
        {
            BoardSize = boardSize;
        }
        public int BoardSize { get; private set; }
        public List<Match> Matches { get; set; }
        Match match = new Match(3);
    }
}
