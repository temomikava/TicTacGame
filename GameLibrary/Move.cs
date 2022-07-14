using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameLibrary
{
    public class Move
    {
        public int PlayerId { get; set; }
        public int RowCoordinate { get; set; }
        public int ColumnCoordinate { get; set; }
        public int MatchId { get; set; }
    }
}
