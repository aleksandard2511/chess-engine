using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessEngine.gameLogic
{
    public class King : Piece
    {
        public bool canCastle=true;
        public King(bool colorWhite,int row,int col,int index, bool canCastle) : base(colorWhite, row, col, index)
        {
            this.canCastle=canCastle; 
        }
        public override void PossibleMoves(Board board, List<(int, int)> moves)
        {
            List<(int row, int col)> possibleMoves= [
                   (row + 1, col + 1),
                   (row + 1, col - 1),
                   (row - 1, col + 1),
                   (row - 1, col - 1),
                   (row + 1, col),
                   (row , col - 1),
                   (row , col + 1),
                   (row - 1, col),
                ];
            foreach (var move in possibleMoves)
            {
                if (board.CheckBoundaries(move.row, move.col)) 
                {
                    var target=board.Squares[move.row,move.col];
                    if (target == null || target.colorWhite!=this.colorWhite)
                    {
                        moves.Add(move);
                    }
                }
            }
            if (board.Squares[7,7] is Rook rook && board.Squares[7,5]==null && board.Squares[7,6]==null)
            {
                if (colorWhite == true && board.CanCastle(this, rook) && !board.IsSquareAttacked(7,5,false) && !board.IsSquareAttacked(7,4,false))
                    moves.Add((row, col + 2));
            }
            if (board.Squares[7, 0] is Rook rook2 && board.Squares[7,1]==null && board.Squares[7, 2] == null && board.Squares[7, 3] == null)
            {
                if (colorWhite == true && board.CanCastle(this, rook2) && !board.IsSquareAttacked(7, 3, false) && !board.IsSquareAttacked(7, 4, false))
                    moves.Add((row, col - 2));
            }
            if (board.Squares[0, 0] is Rook rook3 && board.Squares[0,1]==null && board.Squares[0,2]==null && board.Squares[0,3]==null)
            {
                if (colorWhite == false && board.CanCastle(this, rook3) && !board.IsSquareAttacked(0, 3, true) && !board.IsSquareAttacked(0, 4, true))
                    moves.Add((row, col - 2));
            }
            if (board.Squares[0, 7] is Rook rook4 && board.Squares[0,5]==null && board.Squares[0,6]==null)
            {
                if (colorWhite == false && board.CanCastle(this, rook4) && !board.IsSquareAttacked(0, 5, true) && !board.IsSquareAttacked(0, 4, true))
                    moves.Add((row, col + 2));
            }

        }
    }
}
