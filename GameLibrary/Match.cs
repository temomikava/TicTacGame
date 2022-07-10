﻿using GameLibrary.Enums;

namespace GameLibrary
{
    public class Match:Game,IMatch
    {
        public Mark[,] GameGrid { get;  set; }
        public Mark CurrentPlayer { get;  set; } = Mark.X;
        public int TurnsPassed { get;  set; } = 0;
        public bool MatchOver { get;  set; } = false;
        public WinInfo WinInfo { get;  set; }
        public MatchResult MatchResult { get;  set; }

        //public event Action<int, int> MoveMade;
        //public event Action<MatchResult> GameEnded;
        //public event Action GameRestarted;
       
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
        private (short ErrorCode, string ErrorMessage, bool matchend) CanMakeMove(int r, int c)
        {
            if (r > GameGrid.GetLength(0) - 1 ||
                r < 0 ||
                c > GameGrid.GetLength(0) - 1 ||
                c < 0)

            {
                return ((short)ErrorCode.Denied, $"Index is out of range. Row:{r}, Column: {c}",false);
            }
            else if (MatchOver)
            {
                return ((short)ErrorCode.MatchIsOver, "Match is over",true);
            }
            else if (GameGrid[r, c] != Mark.None)
            {
                return ((short)ErrorCode.TrayIstaken, "tray is taken",false);
            }
            return ((short)ErrorCode.Success, "Success",false);
        }
        public (short ErrorCode, string ErrorMessage,bool matchend) MakeMove(int r, int c)
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
