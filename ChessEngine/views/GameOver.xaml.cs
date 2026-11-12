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
using ChessEngine.gameLogic;

namespace ChessEngine.views
{
    /// <summary>
    /// Interaction logic for GameOver.xaml
    /// </summary>
    public partial class GameOver : Window
    {
        private Game game;
        public GameOver(Game game)
        {
            InitializeComponent();
            this.game = game;
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
            game.Close();
        }
        private void PlayAgainButton_Click(object sender, RoutedEventArgs e)
        {
            game.RestartGame();
            Close();
        }

    }
}
