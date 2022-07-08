using GameLibrary.Enums;

namespace GameLibrary
{
    public class Match
    {
        public int Id { get; set; }
        public DateTime StartedAt { get; set; } = DateTime.Now;
        public DateTime FinishedAt { get; set; }
        public Mark[,] GameGrid { get; private set; }
        public Mark CurrentPlayer { get; private set; } = Mark.X;
        public int TurnsPassed { get; private set; } = 0;
        public bool MatchOver { get; private set; } = false;
        public WinInfo WinInfo { get; private set; }
        public MatchResult MatchResult { get; private set; }

        //public event Action<int, int> MoveMade;
        //public event Action<MatchResult> GameEnded;
        //public event Action GameRestarted;
        private int boardSize = Game.BoardSize;
        public Match()
        {
            GameGrid = new Mark[boardSize, boardSize];
        }


        private bool IsGridFull()
        {
            return TurnsPassed == GameGrid.Length;
        }

        private void SwitchPlayer()
        {
            CurrentPlayer = CurrentPlayer == Mark.X ? Mark.O : Mark.X;
        }

        private bool AreSquaresMarked((int, int)[] squares, Mark player)
        {
            foreach ((int r, int c) in squares)
            {
                if (GameGrid[r, c] != player)
                {
                    return false;
                }

            }
            return true;
        }

        private bool DidMoveWin(int r, int c)
        {
            int length = GameGrid.GetLength(0);
            int lastIndex = length - 1;
            (int, int)[] row = new (int, int)[length].Select((x, i) => (r, i)).ToArray();
            (int, int)[] column = new (int, int)[length].Select((x, i) => (i, c)).ToArray();
            (int, int)[] mainDiagonal = new (int, int)[length].Select((x, i) => (i, i)).ToArray();
            (int, int)[] antiDiagonal = new (int, int)[length].Select((x, i) => (i, lastIndex--)).ToArray();

            if (AreSquaresMarked(row, CurrentPlayer))
            {
                WinInfo = new WinInfo { Type = WinType.Row, Number = r };
                return true;
            }
            if (AreSquaresMarked(column, CurrentPlayer))
            {
                WinInfo = new WinInfo { Type = WinType.Column, Number = c };
                return true;
            }
            if (AreSquaresMarked(antiDiagonal, CurrentPlayer))
            {
                WinInfo = new WinInfo { Type = WinType.AntiDiagonal };
                return true;
            }
            if (AreSquaresMarked(mainDiagonal, CurrentPlayer))
            {
                WinInfo = new WinInfo { Type = WinType.MainDiagonal };
                return true;
            }

            return false;
        }

        private bool DidMoveEndMatch(int r, int c)
        {
            if (DidMoveWin(r, c))
            {
                MatchResult = new MatchResult { Winner = CurrentPlayer, WinInfo = WinInfo };

                return true;
            }
            if (IsGridFull())
            {
                MatchResult = new MatchResult { Winner = Mark.None };
                return true;
            }

            return false;
        }
        private (short ErrorCode, string ErrorMessage) CanMakeMove(int r, int c)
        {
            if (r > GameGrid.GetLength(0) - 1 ||
                r < 0 ||
                c > GameGrid.GetLength(0) - 1 ||
                c < 0)

            {
                return ((short)ErrorCode.Denied, $"Index is out of range. Row:{r}, Column: {c}");
            }
            else if (MatchOver)
            {
                return ((short)ErrorCode.MatchIsOver, "Match is over");
            }
            else if (GameGrid[r, c] != Mark.None)
            {
                return ((short)ErrorCode.TrayIstaken, "tray is taken");
            }
            return ((short)ErrorCode.Success, "Success");
        }
        public (short ErrorCode, string ErrorMessage) MakeMove(int r, int c)
        {

            var canMakeMove = CanMakeMove(r, c);
            if (canMakeMove.ErrorCode != (short)ErrorCode.Success)
            {
                return canMakeMove;
            }

            GameGrid[r, c] = CurrentPlayer;
            TurnsPassed++;
            if (DidMoveEndMatch(r, c))
            {
                MatchOver = true;
                FinishedAt = DateTime.Now;
                //MoveMade?.Invoke(r, c);
                //GameEnded?.Invoke(MatchResult);
            }
            else
            {
                SwitchPlayer();
                //MoveMade?.Invoke(r, c);
            }
            return (canMakeMove);
        }

        public void Reset(int boardSize)
        {
            GameGrid = new Mark[boardSize, boardSize];
            CurrentPlayer = Mark.X;
            TurnsPassed = 0;
            MatchOver = false;
            //GameRestarted?.Invoke();
        }


    }
}
