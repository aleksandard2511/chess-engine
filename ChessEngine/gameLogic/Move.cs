using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessEngine.gameLogic
{
    public class Move
    {
        public int startingRow;
        public int startingCol;
        public int moveRow;
        public int moveCol;
        public Piece? movedPiece;
        public Piece? capturedPiece;
        public bool wasEnPassant;
        public bool wasCastle;
        public Piece? oldEnPassantPawn;
        public int oldFiftyMoveCounter;
        public bool oldCanCastle;
        public Pawn? enPassantCapturedPawn;
        public int enPassantCapturedRow;
        public int enPassantCapturedCol;
        public Pawn? oldEnPassantPawnReference;
        public bool oldEnPassantPawnEligible;
        public Pawn? newEnPassantPawn;
        public int oldWhiteCastleBonus;
        public int oldBlackCastleBonus;
        public int score;
        public int pieceIndex;
        public int fromTo;
        public bool oldIsWhiteQueenCaptured;
        public bool oldIsBlackQueenCaptured;
        public Move(int startingRow, int startingCol, int moveRow, int moveCol, Piece? movedPiece, Piece? capturedPiece)
        {
            this.startingRow = startingRow;
            this.startingCol = startingCol;
            this.moveRow = moveRow;
            this.moveCol = moveCol;
            this.movedPiece = movedPiece;
            this.capturedPiece = capturedPiece;
        }
    }


}
