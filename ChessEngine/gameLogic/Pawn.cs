using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChessEngine.views;

namespace ChessEngine.gameLogic
{
    public class Pawn : Piece
    {
        public bool enPassantEligible = false;
        public Pawn(bool colorWhite, int row, int col, int index, bool enPassantEligible) : base(colorWhite, row, col, index)
        {
            this.enPassantEligible= enPassantEligible;
        }

        public override void PossibleMoves(Board board, List<(int, int)> moves)
        {
            //bool IsInBounds = Board.CheckBoundaries(row, col);
            if (board.CheckBoundaries(row, col))
            {
                if (colorWhite)
                {
                    if (board.CheckBoundaries(row - 1, col))
                    {
                        var target = board.Squares[row - 1, col];
                        if (target == null)
                            moves.Add((row - 1, col));
                    }
                    if (row == 6)
                    {
                        var target = board.Squares[row - 1, col];
                        if (target == null)
                            moves.Add((row - 1, col));
                        var target2 = board.Squares[row - 2, col];
                        if (target2 == null && target == null)
                            moves.Add((row - 2, col));
                    }
                    if (row == 3)
                    {
                        if (board.CheckBoundaries(row, col + 1))
                        {
                            var target = board.Squares[row, col + 1];
                            if (target is Pawn pawn && pawn.enPassantEligible == true)
                                moves.Add((row - 1, col + 1));
                        }
                        if (board.CheckBoundaries(row, col - 1))
                        {
                            var target2 = board.Squares[row, col - 1];
                            if (target2 is Pawn pawn2 && pawn2.enPassantEligible == true)
                                moves.Add((row - 1, col - 1));
                        }
                    }
                    if (board.CheckBoundaries(row - 1, col + 1))
                    {
                        var target = board.Squares[row - 1, col + 1];
                        if (target != null && target.colorWhite != this.colorWhite)
                            moves.Add((row - 1, col + 1));
                    }
                    if (board.CheckBoundaries(row - 1, col - 1))
                    {
                        var target = board.Squares[row - 1, col - 1];
                        if (target != null && target.colorWhite != this.colorWhite)
                            moves.Add((row - 1, col - 1));
                    }
                    
                }
                else
                {
                    if (board.CheckBoundaries(row + 1, col))
                    {
                        var target = board.Squares[row + 1, col];
                        if (target == null)
                            moves.Add((row + 1, col));
                    }
                    if (row == 1)
                    {
                        var target = board.Squares[row + 1, col];
                        if(target == null)
                            moves.Add((row + 1, col));
                        var target2 = board.Squares[row + 2, col];
                        if (target2 == null && target == null)
                            moves.Add((row + 2, col));
                    }
                    if (row == 4)
                    {
                        if (board.CheckBoundaries(row, col + 1))
                        {
                            var target = board.Squares[row, col + 1];
                            if (target is Pawn pawn && pawn.enPassantEligible == true)
                                moves.Add((row + 1, col + 1));
                        }
                        if (board.CheckBoundaries(row, col - 1))
                        {
                            var target = board.Squares[row, col - 1];
                            if (target is Pawn pawn && pawn.enPassantEligible == true)
                                moves.Add((row + 1, col - 1));
                        }
                    }
                    if (board.CheckBoundaries(row + 1, col + 1))
                    {
                        var target = board.Squares[row + 1, col + 1];
                        if (target != null && target.colorWhite != this.colorWhite)
                            moves.Add((row + 1, col + 1));
                    }
                    if (board.CheckBoundaries(row + 1, col - 1))
                    {
                        var target = board.Squares[row + 1, col - 1];
                        if (target != null && target.colorWhite != this.colorWhite)
                            moves.Add((row + 1, col - 1));
                    }
                    
                }
            }
        }
        
    }
}
