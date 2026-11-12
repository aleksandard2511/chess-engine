using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Xml;

namespace ChessEngine.gameLogic
{
    public abstract class Piece
    {
        public bool colorWhite=true;
        public int row;
        public int col;
        public int index;
        public Piece(bool colorWhite, int row, int col, int index)
        {
            this.colorWhite = colorWhite;
            this.row = row;
            this.col = col;
            this.index = index;
        }
        public abstract void PossibleMoves(Board board, List<(int, int)> moves);

        //pomocna metoda za kretanje kraljice topa i lovca
        protected List<(int row, int col)> GetMovesInDirection(Board board, int dRow, int dCol)
        {
            List<(int, int)> validMoves = new List<(int, int)>();

            int r = row + dRow;
            int c = col + dCol;

            while (board.CheckBoundaries(r, c))
            {
                var target = board.Squares[r, c];

                if (target == null)
                {
                    validMoves.Add((r, c));
                }
                else
                {
                    if (target.colorWhite != this.colorWhite)
                        validMoves.Add((r, c));

                    break;
                }

                r += dRow;
                c += dCol;
            }

            return validMoves;
        }
    }
}
