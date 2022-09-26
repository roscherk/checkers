using Avalonia.Controls;
using Avalonia.Interactivity;

namespace checkers
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            var gameWindow = new GameWindow();
            gameWindow.Show();
            this.Close();
        }
    }
}