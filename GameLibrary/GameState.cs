using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameLibrary
{
    public class GameState
    {
        public Player[,] GameGrid { get; private set; }
        public Player CurrentPlayer { get; private set; }
        public int TurnsPassed { get; private set; }
        public bool GameOver { get; private set; }

        public event Action<int, int> MoveMade;
        public event Action<GameResult> GameEnded;
        public event Action GameRestarted;

        public GameState(int boardSize)
        {
            GameGrid = new Player[boardSize, boardSize];
            CurrentPlayer = Player.X;
            TurnsPassed = 0;
            GameOver = false;
        }

        private bool CanMakeMove(int r, int c)
        {
            return !GameOver && GameGrid[r, c] == Player.None;
        }

        private bool IsGridFull()
        {
            return TurnsPassed == GameGrid.Length;
        }

        private void SwitchPlayer()
        {
            CurrentPlayer = CurrentPlayer == Player.X ? Player.O : Player.X;
        }

        private bool AreSquaresMarked((int, int)[] squares, Player player)
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
       
        private bool DidMoveWin( int r, int c, out WinInfo winInfo)
        {
            int length = GameGrid.GetLength(0);
            int lastIndex=length-1;
            (int, int)[] row = new (int,int)[length].Select((x,i)=>(r,i)).ToArray() ;
            (int, int)[] column = new (int,int)[length].Select((x,i)=>(i,c)).ToArray() ;
            (int, int)[] mainDiagonal = new (int, int)[length].Select((x,i)=>(i,i)).ToArray();         
            (int, int)[] antiDiagonal = new (int,int)[length].Select((x,i)=>(i,lastIndex--)).ToArray() ;        
            
            if (AreSquaresMarked(row, CurrentPlayer))
            {               
                winInfo = new WinInfo { Type = WinType.Row, Number = r };
                return true;
            }
            if (AreSquaresMarked(column, CurrentPlayer))
            {
                winInfo = new WinInfo { Type = WinType.Column, Number = c };
                return true;
            }
            if (AreSquaresMarked(antiDiagonal, CurrentPlayer))
            {
                winInfo = new WinInfo { Type = WinType.AntiDiagonal };
                return true;
            }
            if (AreSquaresMarked(mainDiagonal, CurrentPlayer))
            {
                winInfo = new WinInfo { Type = WinType.MainDiagonal };
                return true;
            }

            winInfo = null;
            return false;
        }

        private bool DidMoveEndGame(int r, int c, out GameResult gameResult)
        {
            if (DidMoveWin( r, c, out WinInfo winInfo))
            {
                gameResult = new GameResult { Winner = CurrentPlayer, WinInfo = winInfo };
                return true;
            }
            if (IsGridFull())
            {
                gameResult = new GameResult { Winner = Player.None };
                return true;
            }
            gameResult = null;
            return false;
        }

        public void MakeMove(int r,int c)
        {
            if (!CanMakeMove(r, c))
            {
                return;
            }
            GameGrid[r, c] = CurrentPlayer;
            TurnsPassed++;
            if (DidMoveEndGame(r, c, out GameResult gameResult))
            {
                GameOver = true;
                MoveMade?.Invoke(r, c);
                GameEnded?.Invoke(gameResult);
            }
            else
            {
                SwitchPlayer();
                MoveMade?.Invoke(r, c);
            }
        }

        public void Reset(int boardSize)
        {
            GameGrid = new Player[boardSize, boardSize];
            CurrentPlayer = Player.X;
            TurnsPassed = 0;
            GameOver = false;
            GameRestarted?.Invoke();
        }
    }
}
