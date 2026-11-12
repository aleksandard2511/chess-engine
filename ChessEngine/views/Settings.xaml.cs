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

namespace ChessEngine.views
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        public int level = 1;
        public bool isPlayerWhite = true;
        public int timeControl = 1;
        public Settings()
        {
            InitializeComponent();
        }
        public void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (Level1.IsChecked == true)
                level = 1;
            else if (Level2.IsChecked == true)
                level = 2;
            else if(Level3.IsChecked == true)
                level = 3;

            if(ColorWhite.IsChecked == true)
                isPlayerWhite = true;
            else if (ColorBlack.IsChecked==true)
                isPlayerWhite = false;
            
            if(Time1.IsChecked == true)
                timeControl = 1;
            else if(Time2.IsChecked == true)
                timeControl = 2;
            else if(Time3.IsChecked == true)
                timeControl = 3;

            Game game = new Game(this);
            game.Show();
            Close();
        }
    }
}
