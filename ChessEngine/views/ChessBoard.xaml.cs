using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Linq;
using ChessEngine.gameLogic;

namespace ChessEngine.views
{
    /// <summary>
    /// Interaction logic for ChessBoard.xaml
    /// </summary>
    public partial class ChessBoard : UserControl
    {
        private Board board;
        private Piece? selectedPiece=null;

        private Rectangle[,] squaresUI = new Rectangle[8, 8];

        private Pawn? pawnToPromote = null;
        private Game game;
        private Settings settings;
        private int promoRow;
        private int promoCol;
        private bool promoComplete = true;
        public bool aiThinking = false;
        private TextBlock[] fileTop = new TextBlock[8];
        private TextBlock[] fileBottom = new TextBlock[8];
        private TextBlock[] rankLeft = new TextBlock[8];
        private TextBlock[] rankRight = new TextBlock[8];
        public ChessBoard(Board board, Game game, Settings settings)
        {
            InitializeComponent();
            this.board = board;
            this.game = game;
            this.settings = settings;
            CreateBoardUI();
            CreateCoordinates();
            DrawPieces();
            Loaded += ChessBoard_Loaded;
        }

        private void ChessBoard_Loaded(object sender, RoutedEventArgs e)
        {
            if (!game.playerColorWhite)
            {
                AiMakeMove();
            }
        }
        private void CreateBoardUI()
        {
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    Rectangle square = new Rectangle();
                    if ((i + j) % 2 == 0)
                        square.Fill = Brushes.Beige;
                    else
                        square.Fill = Brushes.BurlyWood;
                    squaresUI[i, j] = square;
                    var (displayRow, displayCol) = BoardToDisplay(i, j);
                    Grid.SetRow(square, displayRow + 1);
                    Grid.SetColumn(square, displayCol + 1);
                    square.MouseLeftButtonDown += Square_Click;
                    BoardGrid.Children.Add(square);
                }
            }
        }
        private void CreateCoordinates()
        {
            string files = game.playerColorWhite
                ? "ABCDEFGH"
                : "HGFEDCBA";

            for (int col = 0; col < 8; col++)
            {
                fileTop[col] = new TextBlock
                {
                    Text = files[col].ToString(),
                    FontSize = 22,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Bottom
                };

                fileBottom[col] = new TextBlock
                {
                    Text = files[col].ToString(),
                    FontSize = 22,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                Grid.SetRow(fileTop[col], 0);
                Grid.SetColumn(fileTop[col], col + 1);

                Grid.SetRow(fileBottom[col], 9);
                Grid.SetColumn(fileBottom[col], col + 1);

                BoardGrid.Children.Add(fileTop[col]);
                BoardGrid.Children.Add(fileBottom[col]);
            }

            for (int row = 0; row < 8; row++)
            {
                string rank = game.playerColorWhite
                    ? (8 - row).ToString()
                    : (row + 1).ToString();

                rankLeft[row] = new TextBlock
                {
                    Text = rank,
                    FontSize = 22,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center
                };

                rankRight[row] = new TextBlock
                {
                    Text = rank,
                    FontSize = 22,
                    VerticalAlignment = VerticalAlignment.Center
                };

                Grid.SetRow(rankLeft[row], row + 1);
                Grid.SetColumn(rankLeft[row], 0);

                Grid.SetRow(rankRight[row], row + 1);
                Grid.SetColumn(rankRight[row], 9);

                BoardGrid.Children.Add(rankLeft[row]);
                BoardGrid.Children.Add(rankRight[row]);
            }
        }

        private void Square_Click(object sender, MouseButtonEventArgs e)
        {
            if (!promoComplete)
                return;
            if (sender is not Rectangle rect)
                return;
            if (aiThinking)
                return;
            int displayRow = Grid.GetRow(rect) - 1;
            int displayCol = Grid.GetColumn(rect) - 1;
            var (row, col) = BoardToDisplay(displayRow, displayCol);
            var piece = board.Squares[row, col];

            if (selectedPiece != null)
                {
                    var moves = board.GetLegalMoves(selectedPiece);
                    if (moves.Contains((row, col)))
                    {
                    Move move = new Move(selectedPiece.row, selectedPiece.col, row, col, selectedPiece, board.Squares[row, col]);
                    bool needsPromotion = board.MakeMove(move);
                    game.IncrementTime();
                    if (board.gameOver)
                    {
                        game.StopTimer();
                        GameOver gameOverWindow=new GameOver(game);
                        gameOverWindow.tbGameOver.Text = board.gameOverMessage;
                        gameOverWindow.ShowDialog();
                    }

                    if (needsPromotion)
                        {
                            game.StopTimer();
                            promoComplete = false;
                            pawnToPromote = (Pawn)selectedPiece;
                            promoRow = row;
                            promoCol = col;
                            selectedPiece = null;

                            ShowPromotionPanel(row, col);
                            return;
                        }
                        selectedPiece = null;
                        UpdateBoard();
                    AiMakeMove();
                    return;
                    }
                if (selectedPiece.row == row && selectedPiece.col == col)
                    {
                        selectedPiece = null;
                        ClearBoardColors();
                        return;
                    }
                    if (piece != null && piece.colorWhite == board.WhiteTurn && piece.colorWhite == selectedPiece.colorWhite)
                    {
                        selectedPiece = piece;
                        ClearBoardColors();
                        rect.Fill = Brushes.Green;
                        var newMoves = board.GetLegalMoves(selectedPiece);
                        foreach (var move in newMoves)
                        {
                            var target = board.Squares[move.row, move.col];
                            if (target != null)
                            {
                                squaresUI[move.row, move.col].Fill = Brushes.Red;
                            }

                            else
                            {
                                squaresUI[move.row, move.col].Fill = Brushes.Yellow;
                            }
                        }
                        return;
                    }
                    selectedPiece = null;
                    ClearBoardColors();
                    return;
                }

            if (piece != null && piece.colorWhite == board.WhiteTurn)
            {
                    selectedPiece = piece;
                    ClearBoardColors();
                    rect.Fill = Brushes.Green;
                    var moves = board.GetLegalMoves(selectedPiece);
                    foreach (var move in moves)
                    {
                        var target = board.Squares[move.row, move.col];
                        if (target != null)
                        {
                                squaresUI[move.row, move.col].Fill = Brushes.Red;
                        }

                        else
                        {
                            squaresUI[move.row, move.col].Fill = Brushes.Yellow;
                        }  
                    }
            }
        }
        private void DrawPieces()
        {
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    var piece = board.Squares[row, col];

                    if (piece != null)
                    {
                        TextBlock text = new TextBlock();
                        text.Tag = "Piece";
                        text.FontSize = 42;
                        text.HorizontalAlignment = HorizontalAlignment.Center;
                        text.VerticalAlignment = VerticalAlignment.Center;
                        text.IsHitTestVisible = false;
                        if (piece is Pawn && piece.colorWhite == true)
                            text.Text = "♙";
                        else if (piece is Pawn && piece.colorWhite == false)
                            text.Text = "♟";
                        else if (piece is Knight && piece.colorWhite == true)
                            text.Text = "♘";
                        else if (piece is Knight && piece.colorWhite == false)
                            text.Text = "♞";
                        else if (piece is Bishop && piece.colorWhite == true)
                            text.Text = "♗";
                        else if (piece is Bishop && piece.colorWhite == false)
                            text.Text = "♝";
                        else if (piece is Rook && piece.colorWhite == true)
                            text.Text = "♖";
                        else if (piece is Rook && piece.colorWhite == false)
                            text.Text = "♜";
                        else if (piece is Queen && piece.colorWhite == true)
                            text.Text = "♕";
                        else if (piece is Queen && piece.colorWhite == false)
                            text.Text = "♛";
                        else if (piece is King && piece.colorWhite == true)
                            text.Text = "♔";
                        else if (piece is King && piece.colorWhite == false)
                            text.Text = "♚";
                        var (displayRow, displayCol) = BoardToDisplay(row, col);
                        Grid.SetRow(text, displayRow + 1);
                        Grid.SetColumn(text, displayCol + 1);
                        BoardGrid.Children.Add(text);
                    }
                }
            }

        }
        private void ClearBoardColors()
        {
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if ((i + j) % 2 == 0)
                        squaresUI[i,j].Fill = Brushes.Beige;
                    else
                        squaresUI[i,j].Fill = Brushes.BurlyWood;
                }
            }
        }

        private void UpdateBoard()
        {
            ClearPieces();
            ClearBoardColors();
            DrawPieces();
        }

        private void PromoteToQueen_Click(object sender, RoutedEventArgs e)
        {
            board.Squares[promoRow, promoCol]=new Queen(pawnToPromote!.colorWhite, promoRow, promoCol, pawnToPromote.colorWhite? 4:10);
            FinishPromotion();
        }
        private void PromoteToRook_Click(object sender, RoutedEventArgs e)
        {
            board.Squares[promoRow, promoCol] = new Rook(pawnToPromote!.colorWhite, promoRow, promoCol, pawnToPromote.colorWhite ? 3 : 9, false);
            FinishPromotion();
        }
        private void PromoteToBishop_Click(object sender, RoutedEventArgs e)
        {
            board.Squares[promoRow, promoCol] = new Bishop(pawnToPromote!.colorWhite, promoRow, promoCol, pawnToPromote.colorWhite ? 2 : 8);
            FinishPromotion();
        }
        private void PromoteToKnight_Click(object sender, RoutedEventArgs e)
        {
            board.Squares[promoRow, promoCol] = new Knight(pawnToPromote!.colorWhite, promoRow, promoCol, pawnToPromote.colorWhite ? 1 : 7);
            FinishPromotion();
        }

        private void FinishPromotion()
        {
            promoComplete = true;
            pawnToPromote = null;
            PromotionPanel.Visibility = Visibility.Collapsed;
            UpdateBoard();
            board.IsGameOver();
            if (board.gameOver)
            {
                game.StopTimer();
                GameOver gameOverWindow = new GameOver(game);
                gameOverWindow.tbGameOver.Text = board.gameOverMessage;
                gameOverWindow.ShowDialog();
                return;
            }
            game.StartTimer();
            AiMakeMove();
        }
        private void ShowPromotionPanel(int row, int col)
        {
            var square = squaresUI[row, col];
            var position = square.TransformToAncestor(BoardGrid).Transform(new Point(0, 0));
            Canvas.SetLeft(PromotionPanel, position.X);
            Canvas.SetTop(PromotionPanel, position.Y-40);
            PromotionPanel.Visibility = Visibility.Visible;
            OverlayCanvas.IsHitTestVisible = true;
        }
        private void ClearPieces()
        {
            for(int i = BoardGrid.Children.Count - 1; i >= 0; i--)
            {
                if (BoardGrid.Children[i] is TextBlock tb && tb.Tag?.ToString() == "Piece")
                {
                    BoardGrid.Children.RemoveAt(i);
                }
            }
        }
        private void AiMakeMove()
        {
            int depth;
            if (settings.timeControl == 3)
            {
                if (settings.level == 3)
                {
                    if (board.whiteMaterial + board.blackMaterial < 4000 && board.whiteMaterial + board.blackMaterial > 2000 && Math.Abs(board.whiteMaterial - board.blackMaterial) < 1000)
                     depth = 9;
                    else if (board.whiteMaterial + board.blackMaterial <= 2000 && Math.Abs(board.whiteMaterial - board.blackMaterial) < 800)
                     depth = 10;
                    else depth = 8;
                    //if (board.whiteMaterial + board.blackMaterial <= 2000 && Math.Abs(board.whiteMaterial - board.blackMaterial) < 800)
                        //depth = 10;
                    //else depth = 9;
                }
                else if (settings.level == 2)
                {
                    depth = 6;
                }
                else depth = 2;
            }
            else if (settings.timeControl == 2)
            {
                if (settings.level == 3)
                {
                    if (board.whiteMaterial + board.blackMaterial < 4000)
                        depth = 8;
                    else depth = 7;
                }
                else if (settings.level == 2)
                {
                    depth = 6;
                }
                else depth = 2;
            }
            else
            {
                if (settings.level == 3)
                    depth = 7;
                else if (settings.level == 2)
                    depth = 6;
                else depth = 1;
            }
            
            if (game.playerColorWhite)
            {
                if (board.WhiteTurn || aiThinking)
                    return;
            }
            else
            {
                if (!board.WhiteTurn || aiThinking)
                    return;
            }
                aiThinking = true;
            {
                Task.Run(() =>
                {
                    Move? aiMove = board.IterativeSearch(depth);

                    Dispatcher.Invoke(async () =>
                    {
                        if (aiMove != null)
                        {
                            if (board.gameOver)
                            {
                                aiThinking = false;
                                return;
                            }
                            if(depth<3)
                               await Task.Delay(1000);
                            board.MakeMove(aiMove);
                            game.IncrementTime();
                            UpdateBoard();
                            squaresUI[board.highlightRow, board.highlightCol].Fill = Brushes.Aqua;

                            if (board.gameOver)
                            {
                                game.StopTimer();
                                GameOver gameOverWindow = new GameOver(game);
                                gameOverWindow.tbGameOver.Text = board.gameOverMessage;
                                gameOverWindow.ShowDialog();
                            }
                        }
                        aiThinking = false;
                    });
                });
            }
        }
        private (int row, int col) BoardToDisplay(int row, int col)
        {
            if (game.playerColorWhite)
                return (row, col);

            return (7 - row, 7 - col);
        }
    }
}
