using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessEngine.gameLogic
{
    public class Knight : Piece
    {
        public Knight(bool colorWhite, int row, int col, int index) : base(colorWhite, row, col, index)
        {
        }
        public override void PossibleMoves(Board board, List<(int, int)> moves)
        {
            List<(int row, int col)> possibleMoves =
                [
                    (row+1,col+2),
                    (row+1,col-2),
                    (row+2,col-1),
                    (row+2,col+1),
                    (row-1,col+2),
                    (row-1,col-2),
                    (row-2,col+1),
                    (row-2,col-1),
                ];
           // List<(int row,int col)> validMoves=new List<(int row,int col)> ();
            foreach (var move in possibleMoves)
            {
                if (board.CheckBoundaries(move.row, move.col))
                {
                    var target = board.Squares[move.row, move.col];
                    if (target==null || target.colorWhite != this.colorWhite)
                    {
                        moves.Add(move);
                    }
                } 
                   
            }
        }
    }
}
