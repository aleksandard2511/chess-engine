using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using ChessEngine.gameLogic;

namespace ChessEngine.views
{
    /// <summary>
    /// Interaction logic for Game.xaml
    /// </summary>
    public partial class Game : Window
    {
        public Board board;
        private ChessBoard chessBoard;
        public DispatcherTimer timer;
        private Settings settings;
        public int whiteSeconds = 60;
        public int blackSeconds = 60;
        public int increment = 0;
        public bool playerColorWhite = true;
        public Game(Settings settings)
        {
            InitializeComponent();
            this.settings = settings;
            GameSetup();
            board =new Board(this);
            chessBoard = new ChessBoard(board, this, settings);
            chessBoard.HorizontalAlignment = HorizontalAlignment.Center;
            chessBoard.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(chessBoard, 0);
            MainGrid.Children.Add(chessBoard);
            timer=new DispatcherTimer();
            timer.Interval=TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();
            UpdateTimers();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (playerColorWhite)
            {
                if (!board.WhiteTurn || chessBoard.aiThinking)
                {
                    blackSeconds--;
                }
                else
                {
                    whiteSeconds--;
                }
            }
            else
            {
                if (board.WhiteTurn || chessBoard.aiThinking)
                {
                    whiteSeconds--;
                }
                else
                {
                    blackSeconds--;
                }
            }

                UpdateTimers();
            if (whiteSeconds <= 0)
            {
                StopTimer();
                GameOver gameOverWindow = new GameOver(this);
                gameOverWindow.tbGameOver.Text = "Game Over! Black wins on time!";
                gameOverWindow.ShowDialog();
            }
            if (blackSeconds <= 0)
            {
                StopTimer();
                GameOver gameOverWindow = new GameOver(this);
                gameOverWindow.tbGameOver.Text = "Game Over! White wins on time!";
                gameOverWindow.ShowDialog();
            }
        }

        private void UpdateTimers()
        {
            tbWhiteTime.Text = TimeSpan.FromSeconds(whiteSeconds).ToString(@"mm\:ss");
            tbBlackTime.Text = TimeSpan.FromSeconds(blackSeconds).ToString(@"mm\:ss");
        }

        public void ResignButton_Click(object sender, EventArgs e)
        {
            board.gameOver = true;
            if (settings.isPlayerWhite)
            {
                StopTimer();
                GameOver gameOverWindow = new GameOver(this);
                gameOverWindow.tbGameOver.Text = "Game Over! Black wins by resignation!";
                gameOverWindow.ShowDialog();
            }
            else
            {
                StopTimer();
                GameOver gameOverWindow=new GameOver(this);
                gameOverWindow.tbGameOver.Text = "Game Over! White wins by resignation!";
                gameOverWindow.ShowDialog();
            }
            board.gameOver = false;
        }
        public void RestartGame()
        {
            /*board=new Board();
            MainGrid.Children.Remove(chessBoard);
            chessBoard=new ChessBoard(board, this);
            Grid.SetColumn(chessBoard, 0);
            MainGrid.Children.Add(chessBoard);*/
            Close();
            Settings settings = new Settings();
            settings.Show();
        }
        public void StopTimer()
        {
            timer.Stop();
        }
        public void StartTimer()
        {
            timer.Start();
        }
        public void IncrementTime()
        {
            if (!board.gameOver)
            {
                if (board.WhiteTurn)
                {
                    blackSeconds += increment;
                }
                else
                {
                    whiteSeconds += increment;
                }
                UpdateTimers();
            }
        }
        public void GameSetup()
        {
            //levele kasnije rasporediti!!!
            //if(settings.level==1)
            //if (settings.level == 1)
            //if (settings.level == 1)
            if (settings.isPlayerWhite)
            {
                playerColorWhite = true;
            }
            else
            {
                playerColorWhite = false;
            }  
            if (settings.timeControl == 1)
            {
                whiteSeconds = 180;
                blackSeconds = 180;
                increment = 2;
            }
            else if (settings.timeControl == 2)
            {
                whiteSeconds = 300;
                blackSeconds = 300;
                increment = 3;
            }
            else if(settings.timeControl == 3)
            {
                whiteSeconds = 600;
                blackSeconds = 600;
                increment = 5;
            }
            if (!playerColorWhite)
            {
                tbWhiteTime.VerticalAlignment = VerticalAlignment.Top;
                tbBlackTime.VerticalAlignment = VerticalAlignment.Bottom;
            }
        }
    }
}