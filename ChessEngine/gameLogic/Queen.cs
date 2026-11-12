using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessEngine.gameLogic
{
    public class Queen : Piece
    {
        public Queen(bool colorWhite, int row, int col, int index) : base(colorWhite, row, col, index)
        {
        }
        public override void PossibleMoves(Board board, List<(int, int)> moves)
        {
            moves.AddRange(GetMovesInDirection(board, 1, 1));
            moves.AddRange(GetMovesInDirection(board, 1, -1));
            moves.AddRange(GetMovesInDirection(board, -1, 1));
            moves.AddRange(GetMovesInDirection(board, -1, -1));

            moves.AddRange(GetMovesInDirection(board, 0, 1));
            moves.AddRange(GetMovesInDirection(board, 1, 0));
            moves.AddRange(GetMovesInDirection(board, 0, -1));
            moves.AddRange(GetMovesInDirection(board, -1, 0));

        }
    }
}
