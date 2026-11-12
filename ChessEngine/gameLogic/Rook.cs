using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessEngine.gameLogic
{
   public class Rook : Piece
    {
        public bool canCastle = true;
        public Rook(bool colorWhite, int row, int col, int index, bool canCastle) : base(colorWhite, row, col, index)
        {
            this.canCastle = canCastle;
        }
        public override void PossibleMoves(Board board, List<(int, int)> moves)
        {

            moves.AddRange(GetMovesInDirection(board, 0, 1));
            moves.AddRange(GetMovesInDirection(board, 1, 0));
            moves.AddRange(GetMovesInDirection(board, 0, -1));
            moves.AddRange(GetMovesInDirection(board, -1, 0));

            /*List<(int row, int col)> moves =new List<(int row, int col)> ();
            int j = 1;
            while (row + j < 8)
            {
                var target = board.Squares[row+j, col];
                if(target == null)
                {
                    moves.Add((row + j, col));
                }
                else
                {
                    if (target.colorWhite != this.colorWhite)
                        moves.Add((row + j, col));

                    break;
                }

                j++;

            }

            j = 1;
            while (row - j >= 0)
            {
                var target = board.Squares[row - j, col];

                if (target == null)
                {
                    moves.Add((row - j, col));
                }
                else
                {
                    if (target.colorWhite != this.colorWhite)
                        moves.Add((row - j, col));

                    break;
                }

                j++;
            }

            j = 1;
            while (col + j < 8)
            {
                var target = board.Squares[row, col + j];

                if (target == null)
                {
                    moves.Add((row, col + j));
                }
                else
                {
                    if (target.colorWhite != this.colorWhite)
                        moves.Add((row, col + j));

                    break;
                }

                j++;
            }

            j = 1;
            while (col - j >= 0)
            {
                var target = board.Squares[row, col - j];

                if (target == null)
                {
                    moves.Add((row, col - j));
                }
                else
                {
                    if (target.colorWhite != this.colorWhite)
                        moves.Add((row, col - j));

                    break;
                }

                j++;
            }

            return moves;*/

        }
    }
}
