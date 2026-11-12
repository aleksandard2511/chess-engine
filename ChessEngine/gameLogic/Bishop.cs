using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessEngine.gameLogic
{
    public class Bishop : Piece
    {
        public Bishop(bool colorWhite,int row,int col, int index) : base(colorWhite,row,col, index)
        {
        }
        public override void PossibleMoves(Board board, List<(int, int)> moves)
        {

            moves.AddRange(GetMovesInDirection(board, 1, 1));
            moves.AddRange(GetMovesInDirection(board, 1, -1));
            moves.AddRange(GetMovesInDirection(board, -1, 1));
            moves.AddRange(GetMovesInDirection(board, -1, -1));

            /*List<(int row,int col)> validMoves=new List<(int row,int col)> ();
            int j = 1;
            while(row+j<8 && col+j<8)
            {
                var target = board.Squares[row + j, col + j];
                if (target == null)
                {
                    validMoves.Add((row + j, col + j));
                }
                else
                {
                    if (target.colorWhite != this.colorWhite)
                    {
                        validMoves.Add((row + j, col + j));
                    }
                        break;
                }

                j++;
            }
            j = 1;

            while (row + j < 8 && col - j >= 0)
            {
                var target = board.Squares[row + j, col - j];
                if (target == null)
                {
                    validMoves.Add((row + j, col - j));
                }
                else
                {
                    if (target.colorWhite != this.colorWhite)
                    {
                        validMoves.Add((row + j, col - j));
                    }
                    break;
                }

                j++;
            }
            j = 1;

            while (row - j >= 0 && col + j < 8)
            {
                var target = board.Squares[row - j, col + j];
                if (target == null)
                {
                    validMoves.Add((row - j, col + j));
                }
                else
                {
                    if (target.colorWhite != this.colorWhite)
                    {
                        validMoves.Add((row - j, col + j));
                    }
                    break;
                }

                j++;
            }
            j = 1;

            while (row - j >= 0 && col - j >= 0)
            {
                var target = board.Squares[row - j, col - j];
                if (target == null)
                {
                    validMoves.Add((row - j, col - j));
                }
                else
                {
                    if (target.colorWhite != this.colorWhite)
                    {
                        validMoves.Add((row - j, col - j));
                    }
                    break;
                }

                j++;
            }

            return validMoves;*/

        }
    }
}
