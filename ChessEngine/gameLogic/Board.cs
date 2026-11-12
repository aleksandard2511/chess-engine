using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using ChessEngine.gameLogic;
using ChessEngine.views;

namespace ChessEngine.gameLogic
{ 
    public class Board
    {
        Piece?[,] squares = new Piece[8, 8];
        public Piece?[,] Squares => squares;
        public bool WhiteTurn = true;
        public Piece? enPassantPawn;
        int legalMoveCounter = 0;
        public int whiteKingRow = 7;
        public int whiteKingCol = 4;
        public int blackKingRow = 0;
        public int blackKingCol = 4;
        public int fiftyMoveRuleCounter = 0;
        public string gameOverMessage = "Game over";
        public bool gameOver = false;
        public List<Move> MoveHistory = new();
        private Game game;
        public int moveCounter = 1;
        private Dictionary<ulong, TableEntry> transpositionTable = new();
        public long nodes = 0;
        public int highlightRow = 0;
        public int highlightCol = 0;
        const int DELTA = 300;
        private Move?[,] bestSilentMoves = new Move?[128, 2];
        private int whiteCastleBonus = 0;
        private int blackCastleBonus = 0;
        private bool isWhiteQueenCaptured = false;
        private bool isBlackQueenCaptured = false;
        private ulong currentHash;
        public ulong CurrentHash => currentHash;
        Dictionary<ulong, int> bitBoardStates = new();
        public long scoreMoveCalls = 0;
        private readonly List<(int row, int col)> attackBuffer = new(32);
        private readonly List<(int row, int col)> moveBuffer = new(32);
        public int whiteMaterial;
        public int blackMaterial;
        private const int MATE_THRESHOLD = 90000;
        private static readonly (int dr, int dc)[] KnightOffsets = { (-2, -1), (-2, 1), (-1, -2), (-1, 2), (1, -2), (1, 2), (2, -1), (2, 1) };
        private static readonly (int dr, int dc)[] DiagonalDirections = { (-1, -1), (-1, 1), (1, -1), (1, 1) };
        private static readonly (int dr, int dc)[] StraightDirections = { (-1, 0), (1, 0), (0, -1), (0, 1) };
        private static readonly (int dr, int dc)[] KingOffsets ={(-1,-1), (-1,0), (-1,1), (0,-1), (0,1),( 1,-1), ( 1,0), (1,1)};
        private const int MAX_VALUE = 1000000;
        private const int MIN_VALUE = -1000000;
        public Board(Game game)
        {
            transpositionTable.Clear();
            bitBoardStates.Clear();
            MoveHistory.Clear();
            nodes = 0;
            GeneratePieces();
            currentHash = ComputeHash();
            whiteMaterial=GenerateWhitePieceValue();
            blackMaterial=GenerateBlackPieceValue();
            bitBoardStates[currentHash] = 1;
            this.game = game;
        }
        public void GeneratePieces()
        {
            for (int i = 0; i < 8; i++)
            {
                squares[1, i] = new Pawn(false, 1, i, 6, false);
                squares[6, i] = new Pawn(true, 6, i, 0, false);
            }
            squares[0, 0] = new Rook(false, 0, 0, 9, true);
            squares[0, 7] = new Rook(false, 0, 7, 9, true);
            squares[7, 0] = new Rook(true, 7, 0, 3, true);
            squares[7, 7] = new Rook(true, 7, 7, 3, true);

            squares[0, 1] = new Knight(false, 0, 1, 7);
            squares[0, 6] = new Knight(false, 0, 6, 7);
            squares[7, 1] = new Knight(true, 7, 1, 1);
            squares[7, 6] = new Knight(true, 7, 6, 1);

            squares[0, 2] = new Bishop(false, 0, 2, 8);
            squares[0, 5] = new Bishop(false, 0, 5, 8);
            squares[7, 2] = new Bishop(true, 7, 2, 2);
            squares[7, 5] = new Bishop(true, 7, 5, 2);

            squares[0, 3] = new Queen(false, 0, 3, 10);
            squares[7, 3] = new Queen(true, 7, 3, 4);

            squares[0, 4] = new King(false, 0, 4, 11, true);
            squares[7, 4] = new King(true, 7, 4, 5, true);
        }
        //bool proverava da li se izvrsava promocija pesaka
        public bool MakeMove(Piece piece, int targetRow, int targetCol, bool searchMove = false)
        {
            Pawn? oldEnPassant = enPassantPawn as Pawn;
            if (oldEnPassant != null)
            {
                oldEnPassant.enPassantEligible = false;
            }
            enPassantPawn = null;
            if (Squares[targetRow, targetCol] == null)
            {
                fiftyMoveRuleCounter++;
            }
            else
            {
                fiftyMoveRuleCounter = 0;
            }
            int oldRow = piece.row;
            int oldCol = piece.col;
            TogglePiece(piece, oldRow, oldCol);
            if (Squares[targetRow, targetCol] != null)
            {
                TogglePiece(Squares[targetRow, targetCol]!, targetRow, targetCol);
            }
            Squares[piece.row, piece.col] = null;
            Squares[targetRow, targetCol] = piece;
            piece.row = targetRow;
            piece.col = targetCol;
            TogglePiece(piece, targetRow, targetCol);
            WhiteTurn = !WhiteTurn;
            ToggleSideToMove();

            if ((piece.index == 0 || piece.index == 6) && oldEnPassant != null && Math.Abs(oldEnPassant.col - oldCol) == 1 && targetCol == oldEnPassant.col && Math.Abs(oldEnPassant.row - targetRow) == 1 && oldEnPassant.row == oldRow)
            {
                Squares[oldRow, targetCol] = null;
                fiftyMoveRuleCounter = 0;
                TogglePiece(oldEnPassant, oldRow, targetCol);
            }
            else if (piece.index == 6 && targetRow == 7)
            {
                fiftyMoveRuleCounter = 0;
                if (!game.playerColorWhite && !searchMove)
                {
                    return true;
                }
                else
                    Squares[targetRow, targetCol] = new Queen(false, targetRow, targetCol, 10);
            }
            else if (piece.index == 0 && targetRow == 0)
            {
                fiftyMoveRuleCounter = 0;
                Squares[targetRow, targetCol] = piece;
                if (game.playerColorWhite && !searchMove)
                {
                    return true;
                }
                else
                    Squares[targetRow, targetCol] = new Queen(true, targetRow, targetCol, 4);

            }
            else if (piece is Pawn pawn && Math.Abs(targetRow - oldRow) == 2)
            {
                pawn.enPassantEligible = true;
                enPassantPawn = pawn;
                fiftyMoveRuleCounter = 0;
            }
            else if (piece is King king)
            {
                king.canCastle = false;
                if (king.colorWhite)
                {
                    whiteKingCol = piece.col;
                    whiteKingRow = piece.row;
                    if (targetCol - oldCol == 2)
                    {
                        whiteCastleBonus = 15;
                        if (Squares[7, 7] is Rook rook)
                        {
                            Squares[7, 7] = null;
                            Squares[7, 5] = rook;
                            rook.col = 5;
                            rook.canCastle = false;
                            TogglePiece(rook, 7, 7);
                            TogglePiece(rook, 7, 5);
                        }
                        
                    }
                    else if (targetCol - oldCol == -2)
                    {
                        whiteCastleBonus = 10;
                        if (Squares[7, 0] is Rook rook)
                        {
                            Squares[7, 0] = null;
                            Squares[7, 3] = rook;
                            rook.col = 3;
                            rook.canCastle = false;
                            TogglePiece(rook, 7, 0);
                            TogglePiece(rook, 7, 3);
                        }
                       
                    }
                }
                else
                {
                    blackKingCol = piece.col;
                    blackKingRow = piece.row;
                    if (targetCol - oldCol == -2)
                    {
                        blackCastleBonus = 10;
                        if (Squares[0, 0] is Rook rook)
                        {
                            Squares[0, 0] = null;
                            Squares[0, 3] = rook;
                            rook.col = 3;
                            rook.canCastle = false;
                            TogglePiece(rook, 0, 0);
                            TogglePiece(rook, 0, 3);
                        }
                        //Squares[0, 0] = null;
                        //Squares[0, 3] = new Rook(false, 0, 3, false);
                    }
                    else if (targetCol - oldCol == 2)
                    {
                        blackCastleBonus = 15;
                        if (Squares[0, 7] is Rook rook)
                        {
                            Squares[0, 7] = null;
                            Squares[0, 5] = rook;
                            rook.col = 5;
                            rook.canCastle = false;
                            TogglePiece(rook, 0, 7);
                            TogglePiece(rook, 0, 5);
                        }
                        //Squares[0, 7] = null;
                        //Squares[0, 5] = new Rook(false, 0, 5, false);
                    }
                }
            }
            else if (piece is Rook rook)
            {
                rook.canCastle = false;
            }
            if (!searchMove)
            {
                moveCounter++;
                if (bitBoardStates.ContainsKey(currentHash))
                    bitBoardStates[currentHash]++;
                else
                    bitBoardStates[currentHash] = 1;
                IsGameOver();
                ThreeFoldRepetition();
                FiftyMoveRule();
                //if(enPassantPawn is Pawn pawn1)
                //MessageBox.Show("Red:" + enPassantPawn?.row.ToString() + "Kolona:" + enPassantPawn?.col.ToString()+"ENPE:"+ pawn1.enPassantEligible.ToString());
            }
            return false;
        }
        public bool CheckBoundaries(int row, int col)
        {
            if (row >= 0 && col >= 0 && row < 8 && col < 8)
                return true;
            else return false;
        }
        public bool CanCastle(King king, Rook rook)
        {
            if (king.canCastle == true && rook.canCastle == true)
                return true;
            else return false;
        }
        public bool IsCheck(bool whiteKing)
        {
            int kingRow = whiteKing ? whiteKingRow : blackKingRow;
            int kingCol = whiteKing ? whiteKingCol : blackKingCol;
            if (PawnAttacks(kingRow, kingCol, whiteKing))
                return true;
            if (KnightAttacks(kingRow, kingCol, whiteKing))
                return true;
            if (KingAttacks(kingRow, kingCol, whiteKing))
                return true;
            if (DiagonalAttacks(kingRow, kingCol, whiteKing))
                return true;
            if (StraightAttacks(kingRow, kingCol, whiteKing))
                return true;
            return false;
        }

        public List<(int row, int col)> GetLegalMoves(Piece piece)
        {
            var legalMoves = new List<(int, int)>();
            moveBuffer.Clear();
            piece.PossibleMoves(this, moveBuffer);
            foreach (var move in moveBuffer)
            {
                int oldRow = piece.row;
                int oldCol = piece.col;
                Piece? capturedPiece = Squares[move.row, move.col];
                Squares[oldRow, oldCol] = null;
                Squares[move.row, move.col] = piece;
                piece.row = move.row;
                piece.col = move.col;
                int oldKingRow = 0;
                int oldKingCol = 0;
                if (piece.index == 5)
                {
                    oldKingRow = whiteKingRow;
                    oldKingCol = whiteKingCol;
                    whiteKingRow = move.row;
                    whiteKingCol = move.col;
                }
                else if (piece.index == 11)
                {
                    oldKingRow = blackKingRow;
                    oldKingCol = blackKingCol;
                    blackKingRow = move.row;
                    blackKingCol = move.col;
                }
                bool kingInCheck = IsCheck(piece.colorWhite);
                piece.row = oldRow;
                piece.col = oldCol;
                /*if (piece is King)
                {
                    if (piece.colorWhite)
                    {
                        whiteKingRow = oldKingRow;
                        whiteKingCol = oldKingCol;
                    }
                    else
                    {
                        blackKingRow = oldKingRow;
                        blackKingCol = oldKingCol;
                    }
                }*/
                if (piece.index == 5)
                {
                    whiteKingRow = oldKingRow;
                    whiteKingCol = oldKingCol;
                }
                else if (piece.index == 11)
                {
                    blackKingRow = oldKingRow;
                    blackKingCol = oldKingCol;
                }
                Squares[oldRow, oldCol] = piece;
                Squares[move.row, move.col] = capturedPiece;
                if (!kingInCheck)
                {
                    legalMoves.Add(move);
                    legalMoveCounter++;
                }
            }
            return legalMoves;
        }

        public bool IsSquareAttacked(int row, int col, bool colorWhite)
        {
            foreach (Piece? piece in Squares)
            {
                if (piece != null && piece.colorWhite == colorWhite)
                {
                    if (piece.index == 5 || piece.index == 11)
                    {
                        if (Math.Abs(piece.row - row) <= 1 && Math.Abs(piece.col - col) <= 1)
                        {
                            return true;
                        }
                    }
                    /*else if (piece is Pawn pawn)
                    {
                        if (pawn.colorWhite && pawn.row == row + 1 && (pawn.col == col + 1 || pawn.col == col - 1))
                            return true;
                        else if (!pawn.colorWhite && pawn.row == row - 1 && (pawn.col == col + 1 || pawn.col == col - 1))
                            return true;
                    }*/
                    else if (piece.index == 0 || piece.index == 6)
                    {
                        if (piece.colorWhite && piece.row == row + 1 && (piece.col == col + 1 || piece.col == col - 1))
                            return true;
                        else if (!piece.colorWhite && piece.row == row - 1 && (piece.col == col + 1 || piece.col == col - 1))
                            return true;
                    }
                    else
                    {
                        attackBuffer.Clear();
                        piece.PossibleMoves(this, attackBuffer);
                        if (attackBuffer.Contains((row, col)))
                            return true;
                    }
                }
            }
            return false;
        }
        public void IsGameOver()
        {
            legalMoveCounter = 0;
            if(whiteMaterial < 700 && whiteMaterial < 700)
            {
                int knightCounter = 0;
                int bishopCounter = 0;
                int pawnCounter = 0;
                int rookCounter = 0;
                foreach(Piece? piece in Squares)
                {
                    if (pawnCounter > 0 || rookCounter > 0)
                        break;
                    if (piece != null)
                    {
                        if (piece.index == 0 || piece.index == 6)
                        {
                            pawnCounter++;
                        }
                        if (piece.index == 3 || piece.index == 9)
                        {
                            rookCounter++;
                        }
                        if(piece.index==1 || piece.index == 7)
                        {
                            knightCounter++;
                        }
                        if(piece.index==2  || piece.index == 8)
                        {
                            bishopCounter++;
                        }
                    }
                }
                if (pawnCounter == 0 && rookCounter == 0 && bishopCounter == 0 && knightCounter == 0)
                {
                    gameOverMessage = "Game over! Draw by insufficient material!";
                    gameOver = true;
                    return;
                }
                else if (pawnCounter == 0 && rookCounter == 0 && bishopCounter == 1 && knightCounter == 0)
                {
                    gameOverMessage = "Game over! Draw by insufficient material!";
                    gameOver = true;
                    return;
                }
                else if (pawnCounter == 0 && rookCounter == 0 && bishopCounter == 0 && knightCounter == 1)
                {
                    gameOverMessage = "Game over! Draw by insufficient material!";
                    gameOver = true;
                    return;
                }
            }
            foreach (Piece? piece in Squares)
            {
                if (piece != null && piece.colorWhite == WhiteTurn)
                {
                    GetLegalMoves(piece);
                }
            }
            if (legalMoveCounter > 0)
            {
                return;
            }
            else
            {
                if (WhiteTurn)
                {
                    if (IsCheck(true))
                    {
                        gameOverMessage = "Game over! Black wins by checkmate!";
                        gameOver = true;
                    }
                    else
                    {
                        gameOverMessage = "Game over! Draw by stalemate!";
                        gameOver = true;
                    }
                }
                else
                {
                    if (IsCheck(false))
                    {
                        gameOverMessage = "Game over! White wins by checkmate!";
                        gameOver = true;
                    }
                    else
                    {
                        gameOverMessage = "Game over! Draw by stalemate!";
                        gameOver = true;
                    }
                }
            }
        }
        public void FiftyMoveRule()
        {
            if (fiftyMoveRuleCounter > 99)
            {
                gameOverMessage = "Game over! Draw by 50 move rule!";
                gameOver = true;
            }
        }
        public void ThreeFoldRepetition()
        {
            if (bitBoardStates[currentHash] >= 3)
            {
                gameOverMessage = "Game over! Draw by threefold repetition!";
                gameOver = true;
            }
        }
        public void AddLegalMovesAsMoveObjects(Piece piece, List<Move> moves)
        {
            foreach (var move in GetLegalMoves(piece))
            {
                moves.Add(new Move(piece.row, piece.col, move.row, move.col, piece, Squares[move.row, move.col]));
            }
        }
        public bool MakeMove(Move move, bool searchMove = false)
        {
            move.oldEnPassantPawn = enPassantPawn;
            move.oldFiftyMoveCounter = fiftyMoveRuleCounter;
            move.oldEnPassantPawnReference = enPassantPawn as Pawn;
            move.oldIsWhiteQueenCaptured = isWhiteQueenCaptured;
            move.oldIsBlackQueenCaptured = isBlackQueenCaptured;
            if(move.capturedPiece != null)
            {
                if (move.capturedPiece.index == 0)
                    whiteMaterial -= 95;
                else if (move.capturedPiece.index == 1)
                    whiteMaterial -= 300;
                else if (move.capturedPiece.index == 2)
                    whiteMaterial -= 310;
                else if (move.capturedPiece.index == 3)
                    whiteMaterial -= 500;
                else if (move.capturedPiece.index == 4)
                {
                    whiteMaterial -= 900;
                    isWhiteQueenCaptured = true;
                }
                else if (move.capturedPiece.index == 6)
                    blackMaterial -= 95;
                else if (move.capturedPiece.index == 7)
                    blackMaterial -= 300;
                else if (move.capturedPiece.index == 8)
                    blackMaterial -= 310;
                else if (move.capturedPiece.index == 9)
                    blackMaterial -= 500;
                else if (move.capturedPiece.index == 10)
                {
                    blackMaterial -= 900;
                    isBlackQueenCaptured = true;
                }
            }
            if (move.movedPiece!.index == 0 && move.moveRow == 0)
                whiteMaterial += 805;
            if (move.movedPiece.index == 6 && move.moveRow == 7)
                blackMaterial += 805;
            if (enPassantPawn is Pawn ep)
            {
                move.oldEnPassantPawnEligible = ep.enPassantEligible;
            }
            /*if (move.movedPiece is King && Math.Abs(move.moveCol - move.startingCol)==2)
            {
                move.oldWhiteCastleBonus = whiteCastleBonus;
                move.oldBlackCastleBonus = blackCastleBonus;
                move.wasCastle = true;
            }*/
            if ((move.movedPiece!.index == 5 || move.movedPiece.index == 11) && Math.Abs(move.moveCol - move.startingCol) == 2)
            {
                move.oldWhiteCastleBonus = whiteCastleBonus;
                move.oldBlackCastleBonus = blackCastleBonus;
                move.wasCastle = true;
            }
            if (move.movedPiece is King king)
            {
                if (king.colorWhite)
                {
                    whiteKingRow = move.moveRow;
                    whiteKingCol = move.moveCol;
                }
                else
                {
                    blackKingRow = move.moveRow;
                    blackKingCol = move.moveCol;
                }
                move.oldCanCastle = king.canCastle;
            }
            else if (move.movedPiece is Rook rook)
            {
                move.oldCanCastle = rook.canCastle;
            }
            else if (move.movedPiece is Pawn pawn && move.capturedPiece == null && move.startingCol != move.moveCol && enPassantPawn != null)
            {
                move.wasEnPassant = true;
                Pawn? capturedPawn = Squares[move.startingRow, move.moveCol] as Pawn;
                if (capturedPawn != null && capturedPawn == enPassantPawn)
                {
                    move.enPassantCapturedPawn = capturedPawn;
                    move.enPassantCapturedRow = capturedPawn.row;
                    move.enPassantCapturedCol = capturedPawn.col;
                    if (move.movedPiece.colorWhite)
                        blackMaterial -= 95;
                    else
                        whiteMaterial -= 95;
                }
            }
            else if (move.movedPiece is Pawn pawn2 && Math.Abs(move.moveRow - move.startingRow) == 2)
            {
                move.newEnPassantPawn = pawn2;
            }
            if (!searchMove)
                MoveHistory.Add(move);
            move.pieceIndex = PieceIndex(move.movedPiece!);

            move.fromTo =
                (move.startingRow << 9) |
                (move.startingCol << 6) |
                (move.moveRow << 3) |
                move.moveCol;
            return MakeMove(move.movedPiece!, move.moveRow, move.moveCol, searchMove);
        }
        public void UndoMove(Move move)
        {
            Piece piece = move.movedPiece!;
            ToggleSideToMove();
            TogglePiece(piece, move.moveRow, move.moveCol);
            if (move.capturedPiece != null)
                TogglePiece(move.capturedPiece, move.moveRow, move.moveCol);
            TogglePiece(piece, move.startingRow, move.startingCol);
            Squares[move.moveRow, move.moveCol] = move.capturedPiece;
            Squares[move.startingRow, move.startingCol] = piece;
            piece.row = move.startingRow;
            piece.col = move.startingCol;
            WhiteTurn = !WhiteTurn;
            enPassantPawn = move.oldEnPassantPawn;
            isWhiteQueenCaptured = move.oldIsWhiteQueenCaptured;
            isBlackQueenCaptured = move.oldIsBlackQueenCaptured;
            fiftyMoveRuleCounter = move.oldFiftyMoveCounter;
            if (move.capturedPiece != null)
            {
                {
                    if (move.capturedPiece.index == 0)
                        whiteMaterial += 95;
                    else if (move.capturedPiece.index == 1)
                        whiteMaterial += 300;
                    else if (move.capturedPiece.index == 2)
                        whiteMaterial += 310;
                    else if (move.capturedPiece.index == 3)
                        whiteMaterial += 500;
                    else if (move.capturedPiece.index == 4)
                        whiteMaterial += 900;
                    else if (move.capturedPiece.index == 6)
                        blackMaterial += 95;
                    else if (move.capturedPiece.index == 7)
                        blackMaterial += 300;
                    else if (move.capturedPiece.index == 8)
                        blackMaterial += 310;
                    else if (move.capturedPiece.index == 9)
                        blackMaterial += 500;
                    else if (move.capturedPiece.index == 10)
                        blackMaterial += 900;
                    
                }
            }
            if(move.movedPiece!.index==0 && move.moveRow == 0)
            {
                whiteMaterial -= 805;
            }
            else if (move.movedPiece!.index == 6 && move.moveRow == 7)
            {
                blackMaterial -= 805;
            }
            else if (move.wasEnPassant)
            {
                if (move.movedPiece.colorWhite)
                    blackMaterial += 95;
                else
                    whiteMaterial += 95;
                Squares[move.enPassantCapturedRow, move.enPassantCapturedCol] = move.enPassantCapturedPawn;
                TogglePiece(move.enPassantCapturedPawn!, move.enPassantCapturedRow, move.enPassantCapturedCol);
            }
            if (move.oldEnPassantPawnReference != null)
            {
                move.oldEnPassantPawnReference.enPassantEligible =
                    move.oldEnPassantPawnEligible;
            }
            if (move.newEnPassantPawn != null)
            {
                move.newEnPassantPawn.enPassantEligible = false;
            }
            if (piece is King king)
            {
                if (king.colorWhite)
                {
                    whiteKingRow = move.startingRow;
                    whiteKingCol = move.startingCol;
                }
                else
                {
                    blackKingRow = move.startingRow;
                    blackKingCol = move.startingCol;
                }
                king.canCastle = move.oldCanCastle;
            }
            else if (piece is Rook rook)
            {
                rook.canCastle = move.oldCanCastle;
            }
            if (move.wasCastle)
            {
                if (WhiteTurn && move.moveCol == 6)
                {
                    whiteCastleBonus = move.oldWhiteCastleBonus;
                    if (Squares[7, 5] is Rook rook2)
                    {
                        Squares[7, 5] = null;
                        Squares[7, 7] = rook2;
                        rook2.col = 7;
                        rook2.canCastle = true;
                        TogglePiece(rook2, 7, 5);
                        TogglePiece(rook2, 7, 7);
                    }
                }
                if (WhiteTurn && move.moveCol == 2)
                {
                    whiteCastleBonus = move.oldWhiteCastleBonus;
                    if (Squares[7, 3] is Rook rook2)
                    {
                        Squares[7, 3] = null;
                        Squares[7, 0] = rook2;
                        rook2.col = 0;
                        rook2.canCastle = true;
                        TogglePiece(rook2, 7, 3);
                        TogglePiece(rook2, 7, 0);
                    }
                }
                if (!WhiteTurn && move.moveCol == 6)
                {
                    blackCastleBonus = move.oldBlackCastleBonus;
                    if (Squares[0, 5] is Rook rook2)
                    {
                        Squares[0, 5] = null;
                        Squares[0, 7] = rook2;
                        rook2.col = 7;
                        rook2.canCastle = true;
                        TogglePiece(rook2, 0, 5);
                        TogglePiece(rook2, 0, 7);
                    }
                }
                if (!WhiteTurn && move.moveCol == 2)
                {
                    blackCastleBonus = move.oldBlackCastleBonus;
                    if (Squares[0, 3] is Rook rook2)
                    {
                        Squares[0, 3] = null;
                        Squares[0, 0] = rook2;
                        rook2.col = 0;
                        rook2.canCastle = true;
                        TogglePiece(rook2, 0, 3);
                        TogglePiece(rook2, 0, 0);
                    }
                }
            }

        }

        public List<Move> GetAllLegalMoves(bool white)
        {
            List<Move> moves = new(64);
            foreach (Piece? piece in Squares)
            {
                if (piece != null && piece.colorWhite == white)
                {
                    AddLegalMovesAsMoveObjects(piece, moves);
                }
            }
            return moves;
        }
        public int EvaluatePosition()
        {
            int score = 0;
            foreach (Piece? piece in Squares)
            {
                if (piece == null)
                    continue;
                int pieceType = piece.index % 6;
                int value = pieceType switch
                {
                    0 => 95 + PawnEvaluation(piece),
                    1 => 300 + KnightEvaluation(piece),
                    2 => 310 + BishopEvaluation(piece),
                    3 => 500 + RookEvaluation(piece),
                    4 => 980 + QueenEvaluation(piece),
                    5 => 10000 + KingEvaluation(piece),
                    _ => 0
                };

                score += piece.colorWhite ? value : -value;
            }
            score += whiteCastleBonus + blackCastleBonus;
            return score;
        }
        public int Minimax(int depth, int ply, bool maximizingPlayer, int alpha, int beta, bool allowNull = true)
        {
            bool inCheck = IsCheck(maximizingPlayer);
            nodes++;
            if (ply > 0 && bitBoardStates.TryGetValue(currentHash, out int count) && count >= 2)
            {
                return 0;
            }
            int alphaOriginal = alpha;
            int betaOriginal = beta;
            ulong hash = currentHash;
            Move? bestMove = null;
            TableEntry? entry = null;
            bitBoardStates.TryGetValue(hash, out int currentHistoryCount);
            bool isRepetitionPath = currentHistoryCount > 0;
            if (transpositionTable.TryGetValue(hash, out TableEntry ttEntry))
            {
                entry = ttEntry;
                int ttScore = ScoreFromTT(entry.Value.Score, ply);
                if (entry.Value.Depth >= depth && !isRepetitionPath)
                {
                    switch (entry.Value.Flag)
                    {
                        case TableFlag.Exact:
                            return ttScore;
                        case TableFlag.LowerBound:
                            alpha = Math.Max(alpha, ttScore);
                            break;
                        case TableFlag.UpperBound:
                            beta = Math.Min(beta, ttScore);
                            break;
                    }
                    if (alpha >= beta)
                        return ttScore;
                }
            }

            if (depth == 0)
                return Quiescence(alpha, beta, maximizingPlayer, ply);

            if (allowNull && depth >= 3 && !inCheck && whiteMaterial > 500 && blackMaterial > 500)
            {
                int reduction = 2;
                MakeNullMove();
                int score = Minimax(depth - 1 - reduction, ply + 1, !maximizingPlayer, alpha, beta, false);
                UndoNullMove();
                if (maximizingPlayer)
                {
                    if (score >= beta)
                        return beta;
                }
                else
                {
                    if (score <= alpha)
                        return alpha;
                }
            }
            List<Move> moves = GetAllLegalMoves(maximizingPlayer);
            foreach (Move move in moves)
            {
                move.score = ScoreMove(move, ply);
            }
            moves.Sort((a, b) => b.score.CompareTo(a.score));
            if (entry?.BestMove != null && !isRepetitionPath)
            {
                Move hashMove = entry.Value.BestMove;
                int index = moves.FindIndex(m =>
                    m.startingRow == hashMove.startingRow &&
                    m.startingCol == hashMove.startingCol &&
                    m.moveRow == hashMove.moveRow &&
                    m.moveCol == hashMove.moveCol);
                if (index > 0)
                {
                    Move temp = moves[index];
                    moves.RemoveAt(index);
                    moves.Insert(0, temp);
                }
            }
            if (moves.Count == 0)
            {
                if (IsCheck(maximizingPlayer))
                {
                    return maximizingPlayer ? -1000000 + ply : 1000000 - ply;
                }
                return 0;
            }
            if (maximizingPlayer)
            {
                int best = MIN_VALUE;
                for (int moveIndex = 0; moveIndex < moves.Count; moveIndex++)
                {
                    Move move = moves[moveIndex];
                    bool reduce = depth >= 4 && moveIndex >= 4 && move.capturedPiece == null && !inCheck && whiteMaterial > 1000 && blackMaterial > 1000;
                    MakeMove(move, true);int score;
                    bool firstMove = (moveIndex == 0);
                    if (firstMove || whiteMaterial < 500 || blackMaterial < 500)
                    {
                        if (reduce)
                        {
                            score = Minimax(depth - 2, ply + 1, false, alpha, beta);
                            if (score > alpha)
                                score = Minimax(depth - 1, ply + 1, false, alpha, beta);
                        }
                        else
                        {
                            score = Minimax(depth - 1, ply + 1, false, alpha, beta);
                        }
                    }
                    else
                    {
                        if (reduce)
                        {
                            score = Minimax(depth - 2, ply + 1, false, alpha, alpha + 1);
                            if (score > alpha)
                            {
                                score = Minimax(depth - 1, ply + 1, false, alpha, alpha + 1);
                                if (score > alpha && score < beta)
                                {
                                    score = Minimax(depth - 1, ply + 1, false, alpha, beta);
                                }
                            }
                        }
                        else
                        {
                            score = Minimax(depth - 1, ply + 1, false, alpha, alpha + 1);
                            if (score > alpha && score < beta)
                            {
                                score = Minimax(depth - 1, ply + 1, false, alpha, beta);
                            }
                        }
                    }
                    UndoMove(move);
                    if (score > best)
                    {
                        best = score;
                        bestMove = move;
                    }
                    alpha = Math.Max(alpha, best);
                    if (beta <= alpha)
                    {
                        if (move.capturedPiece == null)
                        {
                            if (!SameMove(move, bestSilentMoves[ply, 0]))
                            {
                                bestSilentMoves[ply, 1] = bestSilentMoves[ply, 0];
                                bestSilentMoves[ply, 0] = move;
                            }
                        }
                        break;
                    }
                }
                TableFlag flag;
                if (best <= alphaOriginal)
                    flag = TableFlag.UpperBound;
                else if (best >= betaOriginal)
                    flag = TableFlag.LowerBound;
                else
                    flag = TableFlag.Exact;
                transpositionTable[hash] = new TableEntry
                {
                    Depth = depth,
                    Score = ScoreToTT(best, ply),
                    Flag = flag,
                    BestMove = bestMove
                };
                return best;
            }
            else
            {
                int best = MAX_VALUE;
                for (int moveIndex = 0; moveIndex < moves.Count; moveIndex++)
                {
                    Move move = moves[moveIndex];
                    bool reduce = depth >= 4 && moveIndex >= 4 && move.capturedPiece == null && !inCheck && whiteMaterial > 1000 && blackMaterial > 1000;
                    MakeMove(move, true);
                    int score;
                    bool firstMove = (moveIndex == 0);
                    if (firstMove || whiteMaterial < 500 || blackMaterial < 500)
                    {
                        if (reduce)
                        {
                            score = Minimax(depth - 2, ply + 1, true, alpha, beta);
                            if (score < beta)
                                score = Minimax(depth - 1, ply + 1, true, alpha, beta);
                        }
                        else
                        {
                            score = Minimax(depth - 1, ply + 1, true, alpha, beta);
                        }
                    }
                    else
                    {
                        if (reduce)
                        {
                            score = Minimax(depth - 2, ply + 1, true, beta - 1, beta);
                            if (score < beta)
                            {
                                score = Minimax(depth - 1, ply + 1, true, beta - 1, beta);
                                if (score > alpha && score < beta)
                                {
                                    score = Minimax(depth - 1, ply + 1, true, alpha, beta);
                                }
                            }
                        }
                        else
                        {
                            score = Minimax(depth - 1, ply + 1, true, beta - 1, beta);
                            if (score > alpha && score < beta)
                            {
                                score = Minimax(depth - 1, ply + 1, true, alpha, beta);
                            }
                        }
                    }
                    UndoMove(move);
                    if (score < best)
                    {
                        best = score;
                        bestMove = move;
                    }
                    beta = Math.Min(beta, best);
                    if (beta <= alpha)
                    {
                        if (move.capturedPiece == null)
                        {
                            if (!SameMove(move, bestSilentMoves[ply, 0]))
                            {
                                bestSilentMoves[ply, 1] = bestSilentMoves[ply, 0];
                                bestSilentMoves[ply, 0] = move;
                            }
                        }
                        break;
                    }
                }
                TableFlag flag;
                if (best <= alphaOriginal)
                    flag = TableFlag.UpperBound;
                else if (best >= betaOriginal)
                    flag = TableFlag.LowerBound;
                else
                    flag = TableFlag.Exact;
                if (!isRepetitionPath)
                {
                    transpositionTable[hash] = new TableEntry
                    {
                        Depth = depth,
                        Score = ScoreToTT(best, ply),
                        Flag = flag,
                        BestMove = bestMove
                    };
                }
                return best;
            }
        }
        public Move? FindBestMove(int depth)
        {
            TimeSpan totalPauseTime = GC.GetTotalPauseDuration();
            nodes = 0;
            Move? bestMove = null;
            int alpha = MIN_VALUE;
            int beta = MAX_VALUE;
            int bestScore = WhiteTurn ? MIN_VALUE : MAX_VALUE;
            List<Move> moves = GetAllLegalMoves(WhiteTurn);
            foreach (Move move in moves)
                move.score = ScoreMove(move, 0);
            moves.Sort((a, b) => b.score.CompareTo(a.score));
            ulong hash = currentHash;
            if (transpositionTable.TryGetValue(hash, out TableEntry entry) &&
                entry.BestMove != null)
            {
                int index = moves.FindIndex(m =>
                    m.startingRow == entry.BestMove.startingRow &&
                    m.startingCol == entry.BestMove.startingCol &&
                    m.moveRow == entry.BestMove.moveRow &&
                    m.moveCol == entry.BestMove.moveCol);

                if (index > 0)
                {
                    Move temp = moves[index];
                    moves.RemoveAt(index);
                    moves.Insert(0, temp);
                }
            }
            foreach (Move move in moves)
            {
                MakeMove(move, true);
                int score = Minimax(depth - 1, 1, !move.movedPiece!.colorWhite, alpha, beta);
                UndoMove(move);
                if (WhiteTurn)
                {
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestMove = move;
                    }
                    alpha = Math.Max(alpha, score);
                }
                else
                {
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestMove = move;
                    }
                    beta = Math.Min(beta, score);
                }
            }
            Debug.WriteLine($"Half move: {moveCounter:N0}");
            Debug.WriteLine($"Nodes searched: {nodes:N0}");
            Debug.WriteLine($"TT entries: {transpositionTable.Count:N0}");
            Debug.WriteLine($"White queen captured:{isWhiteQueenCaptured.ToString()}");
            Debug.WriteLine($"Black queen captured:{isBlackQueenCaptured.ToString()}");
            //Debug.WriteLine($"Score move call counter: {scoreMoveCalls:N0}");
            Debug.WriteLine($"GC Collection Count0: {GC.CollectionCount(0):N0}");
            Debug.WriteLine($"GC Collection Count1: {GC.CollectionCount(1):N0}");
            Debug.WriteLine($"GC Collection Count2: {GC.CollectionCount(2):N0}");
            Debug.WriteLine($"Total GC Pause Time: {totalPauseTime.TotalMilliseconds} ms");
            //Debug.WriteLine($"Current Hash: {currentHash:N0}");
            Debug.WriteLine($"White Material Value:{whiteMaterial:N0}");
            Debug.WriteLine($"Black Material Value:{blackMaterial:N0}");
            Debug.WriteLine($"White King Row :{whiteKingRow:N0}");
            Debug.WriteLine($"White King Col :{whiteKingCol:N0}");
            Debug.WriteLine($"Black King Row :{blackKingRow:N0}");
            Debug.WriteLine($"Baclk King Col :{blackKingCol:N0}");
            Debug.WriteLine($"Best move value:{bestScore:N0}");
            if (bestMove != null)
            {
                highlightRow = bestMove.moveRow;
                highlightCol = bestMove.moveCol;
            }
            return bestMove;
        }
        public int KnightEvaluation(Piece knight)
        {
            if (knight.colorWhite)
            {
                int score = PieceSquareTables.GetTableValue(PieceSquareTables.whiteKnightTable, knight.row, knight.col);
                return score;
            }
            else
            {
                int score = PieceSquareTables.GetTableValue(PieceSquareTables.blackKnightTable, knight.row, knight.col);
                return score;
            }
        }
        public int BishopEvaluation(Piece bishop)
        {
            int score = 0;
            if (bishop.colorWhite)
            {
                if (bishop.row != 7)
                {
                    score += 25;
                    if (moveCounter < 16)
                    {
                        if (Squares[bishop.row + 1, bishop.col]?.index == 0)
                            score -= 15;
                    }
                }    
            }
            else
            {
                if (bishop.row != 0)
                {
                    score += 25;
                    if(moveCounter < 16)
                    {
                        if (Squares[bishop.row - 1, bishop.col]?.index == 6)
                            score -= 15;
                    }
                } 
            }
            return score;
        }
        public int KingEvaluation(Piece king)
        {
            int score = 0;
            if (king.colorWhite)
            {
                if (whiteCastleBonus == 15 && blackMaterial > 1500)
                    score += PieceSquareTables.GetTableValue(PieceSquareTables.whiteKingTableKingsideCastle, whiteKingRow, whiteKingCol);
                else if (whiteCastleBonus == 10 && blackMaterial > 1500)
                    score += PieceSquareTables.GetTableValue(PieceSquareTables.whiteKingTableQueensideCaslte, whiteKingRow, whiteKingCol);
            }
            else
            {
                if (blackCastleBonus == 15 && whiteMaterial > 1500)
                    score += PieceSquareTables.GetTableValue(PieceSquareTables.blackKingTableKingsideCastle, blackKingRow, blackKingCol);
                else if (blackCastleBonus == 10 && whiteMaterial > 1500)
                    score += PieceSquareTables.GetTableValue(PieceSquareTables.blackKingTableQueensideCastle, blackKingRow, blackKingCol);
            }

            if (blackMaterial == 0)
            {
                if (!king.colorWhite)
                {
                    if (king.row == 0 || king.row == 7 || king.col == 0 || king.col == 7)
                        score -= 100;
                    //if (king.col == 0 || king.col == 7)
                        //score -= 100;
                }
                else
                {
                    int edge = Math.Abs(blackKingRow - 3) + Math.Abs(blackKingCol - 3);
                    score += edge * 40;
                    int kingDistance = Math.Abs(whiteKingRow - blackKingRow) + Math.Abs(whiteKingCol - blackKingCol);
                    score += (14 - kingDistance) * 20;
                }
            }
            else if (blackMaterial > 0 && blackMaterial < 1500)
            {
                if (king.colorWhite)
                    score += PieceSquareTables.GetTableValue(PieceSquareTables.whiteKingTableEndgame, whiteKingRow, whiteKingCol);
            }
            if (whiteMaterial == 0)
            {
                if (king.colorWhite)
                {
                    if (king.row == 0 || king.row == 7 || king.col == 0 || king.col == 7)
                        score -= 100;
                    //if (king.col == 0 || king.col == 7)
                        //score -= 100;
                }
                else
                {
                    int edge = Math.Abs(whiteKingRow - 3) + Math.Abs(whiteKingCol - 3);
                    score += edge * 40;
                    int kingDistance = Math.Abs(whiteKingRow - blackKingRow) + Math.Abs(whiteKingCol - blackKingCol);
                    score += (14 - kingDistance) * 20;
                }
            }
            else if(whiteMaterial>0 && whiteMaterial < 1500)
            {
                if (!king.colorWhite)
                {
                    score += PieceSquareTables.GetTableValue(PieceSquareTables.blackKingTableEndgame, blackKingRow, blackKingCol);
                }
            }
                return score;
        }
        public int QueenEvaluation(Piece queen)
        {
            int score = 0;
            if (queen.colorWhite)
            {
                if (queen.row != 7)
                    score += 2;
            }
            else
            {
                if (queen.row != 0)
                    score += 2;
            }
            return score;
        }
        public int RookEvaluation(Piece rook)
        {
            int score = 0;
            int openFileBonus = 30;
            if (rook.colorWhite)
            {
                if (rook.row == 0)
                    score += 15;
                else if (rook.row == 1)
                    score += 20;
            }
            else
            {
                if (rook.row == 7)
                    score += 15;
                else if (rook.row == 6)
                    score += 20;
            }
            for (int i = 0; i < 8; i++)
            {
                if (Squares[i, rook.col]?.index == 0 || Squares[i, rook.col]?.index == 6)
                    openFileBonus -= 10;
            }
            return score+openFileBonus;
        }
        public int PawnEvaluation(Piece pawn)
        {
            int score = 0;
            if (pawn.colorWhite)
            {
                if (blackMaterial > 1500)
                {
                    if (whiteCastleBonus == 0)
                        score += PieceSquareTables.GetTableValue(PieceSquareTables.whitePawnTableOpening, pawn.row, pawn.col);
                    else if (whiteCastleBonus == 15)
                        score += PieceSquareTables.GetTableValue(PieceSquareTables.whitePawnTableKingsideCastle, pawn.row, pawn.col);
                    else if (whiteCastleBonus == 10)
                        score += PieceSquareTables.GetTableValue(PieceSquareTables.whitePawnTableQueensideCastle, pawn.row, pawn.col);
                }
                else 
                    score += PieceSquareTables.GetTableValue(PieceSquareTables.whitePawnTableEndgame, pawn.row, pawn.col);
                
                if (pawn.row > 3)
                {
                    var square1 = Squares[pawn.row - 1, pawn.col];
                    if (square1?.index == 0)
                        score -= 10;
                    else
                    {
                        var square2 = Squares[pawn.row - 2, pawn.col];
                        if (square2?.index == 0)
                            score -= 10;
                    }
                }
            }
            else
            {
                if (whiteMaterial > 1500)
                {
                    if (blackCastleBonus == 0)
                        score += PieceSquareTables.GetTableValue(PieceSquareTables.blackPawnTableOpening, pawn.row, pawn.col);
                    else if (blackCastleBonus == 15)
                        score += PieceSquareTables.GetTableValue(PieceSquareTables.blackPawnTableKingsideCastle, pawn.row, pawn.col);
                    else if (blackCastleBonus == 10)
                        score += PieceSquareTables.GetTableValue(PieceSquareTables.blackPawnTableQueensideCastle, pawn.row, pawn.col);
                }
                else
                    score += PieceSquareTables.GetTableValue(PieceSquareTables.blackPawnTableEndgame, pawn.row, pawn.col);

                var square1 = Squares[pawn.row + 1, pawn.col];
                if (pawn.row < 4)
                {
                    var square3 = Squares[pawn.row + 1, pawn.col];
                    if (square3?.index == 6)
                        score -= 10;
                    else
                    {
                        var square4 = Squares[pawn.row + 2, pawn.col];
                        if (square4?.index == 6)
                            score -= 10;
                    }
                }
            }
            return score;
        }

        private int ScoreMove(Move move, int ply)
        {
            int score = 0;
            if (bestSilentMoves[ply, 0] != null && SameMove(move, bestSilentMoves[ply, 0]!))
            {
                score += 7000;
            }
            if (bestSilentMoves[ply, 1] != null && SameMove(move, bestSilentMoves[ply, 1]!))
            {
                score += 6500;
            }
            if (move.capturedPiece != null)
            {
                score += 7000;
                score += 2 * PieceValue(move.capturedPiece) - PieceValue(move.movedPiece!);
            }
            if (move.movedPiece!.index == 0 && move.movedPiece.index == 6)
            {
                if (move.moveRow == 0 || move.moveRow == 7)
                    score += 10000;
            }
            return score;
        }
        private int PieceIndex(Piece piece)
        {
            if (piece.colorWhite)
            {
                return piece switch
                {
                    Pawn => 0,
                    Knight => 1,
                    Bishop => 2,
                    Rook => 3,
                    Queen => 4,
                    King => 5,
                    _ => 0
                };
            }
            else
            {
                return piece switch
                {
                    Pawn => 6,
                    Knight => 7,
                    Bishop => 8,
                    Rook => 9,
                    Queen => 10,
                    King => 11,
                    _ => 0
                };
            }
        }
        public ulong ComputeHash()
        {
            ulong hash = 0;
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    Piece? piece = Squares[r, c];
                    if (piece == null)
                        continue;
                    hash ^= Zobrist.PieceKeys[PieceIndex(piece), r, c];
                }
            }
            if (WhiteTurn)
                hash ^= Zobrist.SideToMove;
            return hash;
        }
        private int Quiescence(int alpha, int beta, bool player, int ply)
        {
            nodes++;
            bool inCheck = IsCheck(player);
            int standPat = 0;
            if (!inCheck)
            {
                standPat = EvaluatePosition();
                if (player)
                {
                    if (standPat >= beta)
                        return beta;
                    alpha = Math.Max(alpha, standPat);
                }
                else
                {
                    if (standPat <= alpha)
                        return alpha;
                    beta = Math.Min(beta, standPat);
                }
            }
            List<Move> moves = inCheck ? GetAllLegalMoves(player) : GetAllCaptureMoves(player);
            if (moves.Count == 0)
            {
                if (inCheck)
                {
                    return player ? -1000000 + ply : 1000000 - ply;
                }
                return standPat;
            }
            moves.Sort((a, b) => ScoreMove(b, 0).CompareTo(ScoreMove(a, 0)));
            foreach (Move move in moves)
            {
                if (!inCheck && move.capturedPiece != null)
                {
                    int gain = PieceValue(move.capturedPiece);

                    if (player)
                    {
                        if (standPat + gain + DELTA < alpha)
                            continue;
                    }
                    else
                    {
                        if (standPat - gain - DELTA > beta)
                            continue;
                    }
                }
                MakeMove(move, true);
                int score = Quiescence(alpha, beta, !player, ply);
                UndoMove(move);
                if (player)
                {
                    if (score > alpha)
                        alpha = score;
                    if (alpha >= beta)
                        break;
                }
                else
                {
                    if (score < beta)
                        beta = score;
                    if (beta <= alpha)
                        break;
                }
            }
            return player ? alpha : beta;
        }
        public List<Move> GetAllCaptureMoves(bool white)
        {
            List<Move> moves = new List<Move>();
            foreach (Piece? piece in Squares)
            {
                if (piece == null || piece.colorWhite != white)
                    continue;

                foreach (var move in GetLegalMoves(piece))
                {
                    Piece? captured = Squares[move.row, move.col];

                    bool promotion =
                        piece is Pawn &&
                        (move.row == 0 || move.row == 7);

                    if (captured != null || promotion)
                    {
                        moves.Add(new Move(piece.row, piece.col, move.row, move.col, piece, captured));
                    }
                }
            }
            return moves;
        }
        private int PieceValue(Piece piece)
        {
            return piece switch
            {
                Pawn => 95,
                Knight => 300,
                Bishop => 310,
                Rook => 500,
                Queen => 900,
                King => 10000,
                _ => 0
            };
        }
        private bool SameMove(Move? a, Move? b)
        {
            if (a == null || b == null)
                return false;
            return a.startingRow == b.startingRow && a.startingCol == b.startingCol && a.moveRow == b.moveRow && a.moveCol == b.moveCol;
        }
        private void TogglePiece(Piece piece, int row, int col)
        {
            currentHash ^= Zobrist.PieceKeys[PieceIndex(piece), row, col];
        }

        private void ToggleSideToMove()
        {
            currentHash ^= Zobrist.SideToMove;
        }
        private void MakeNullMove()
        {
            WhiteTurn = !WhiteTurn;
            ToggleSideToMove();
        }
        private void UndoNullMove()
        {
            WhiteTurn = !WhiteTurn;
            ToggleSideToMove();
        }
        public Move? IterativeSearch(int maxDepth)
        {
            Move? best = null;

            for (int depth = 1; depth <= maxDepth; depth++)
            {
                best = FindBestMove(depth);

                Debug.WriteLine($"Depth {depth}");
            }

            return best;
        }
        private int GenerateWhitePieceValue()
        {
            int value = 0;
            foreach(Piece piece in squares)
            {
                if(piece != null)
                {
                    if (piece.colorWhite)
                    {
                        value += PieceValue(piece);
                    }
                }
            }
            return value - 10000;
        }
        private int GenerateBlackPieceValue()
        {
            int value = 0;
            foreach (Piece piece in squares)
            {
                if (piece != null)
                {
                    if (!piece.colorWhite)
                    {
                        value += PieceValue(piece);
                    }
                }
            }
            return value - 10000;
        }
        private int ScoreToTT(int score, int ply)
        {
            if (score >= MATE_THRESHOLD) return score + ply;
            if (score <= -MATE_THRESHOLD) return score - ply;
            return score;
        }

        private int ScoreFromTT(int score, int ply)
        {
            if (score >= MATE_THRESHOLD) return score - ply;
            if (score <= -MATE_THRESHOLD) return score + ply;
            return score;
        }
        private bool PawnAttacks(int kingRow, int kingCol, bool whiteKing)
        {
            int pawnRow = whiteKing ? kingRow - 1 : kingRow + 1;

            if (pawnRow < 0 || pawnRow > 7)
                return false;

            if (kingCol > 0)
            {
                Piece? p = Squares[pawnRow, kingCol - 1];
                if (p is Pawn && p.colorWhite != whiteKing)
                    return true;
            }

            if (kingCol < 7)
            {
                Piece? p = Squares[pawnRow, kingCol + 1];
                if (p is Pawn && p.colorWhite != whiteKing)
                    return true;
            }

            return false;
        }
        private bool KnightAttacks(int kingRow, int kingCol, bool whiteKing)
        {
            foreach (var (dr, dc) in KnightOffsets)
            {
                int row = kingRow + dr;
                int col = kingCol + dc;

                if (row < 0 || row > 7 || col < 0 || col > 7)
                    continue;

                Piece? piece = Squares[row, col];

                if (piece is Knight && piece.colorWhite != whiteKing)
                    return true;
            }

            return false;
        }
        private bool DiagonalAttacks(int kingRow, int kingCol, bool whiteKing)
        {
            foreach (var (dr, dc) in DiagonalDirections)
            {
                int row = kingRow + dr;
                int col = kingCol + dc;

                while (row >= 0 && row < 8 && col >= 0 && col < 8)
                {
                    Piece? piece = Squares[row, col];

                    if (piece != null)
                    {
                        if (piece.colorWhite != whiteKing &&
                            (piece is Bishop || piece is Queen))
                        {
                            return true;
                        }
                        break;
                    }

                    row += dr;
                    col += dc;
                }
            }

            return false;
        }
        private bool StraightAttacks(int kingRow, int kingCol, bool whiteKing)
        {
            foreach (var (dr, dc) in StraightDirections)
            {
                int row = kingRow + dr;
                int col = kingCol + dc;

                while (row >= 0 && row < 8 && col >= 0 && col < 8)
                {
                    Piece? piece = Squares[row, col];

                    if (piece != null)
                    {
                        if (piece.colorWhite != whiteKing &&
                            (piece is Rook || piece is Queen))
                        {
                            return true;
                        }

                        break;
                    }

                    row += dr;
                    col += dc;
                }
            }

            return false;
        }
        private bool KingAttacks(int kingRow, int kingCol, bool whiteKing)
        {
            foreach (var (dr, dc) in KingOffsets)
            {
                int row = kingRow + dr;
                int col = kingCol + dc;

                if (row < 0 || row > 7 || col < 0 || col > 7)
                    continue;

                Piece? piece = Squares[row, col];

                if (piece is King && piece.colorWhite != whiteKing)
                    return true;
            }

            return false;
        }
    }
}