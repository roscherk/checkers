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

        // ReSharper disable UnusedParameter.Local
        private void button_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            switch (button.Name)
            {
                case "NewGameButton":
                {
                    var gameWindow = new GameWindow();
                    gameWindow.Show();
                    break;
                }
                case "SettingsButton":
                    break;
            }
        }
    }
}