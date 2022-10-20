using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using NetCoreAudio;

namespace checkers
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // ReSharper disable once UnusedParameter.Local
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
                    new StroikiWindow { Path_ = new ImageBrush(new Bitmap("../../../Assets/stroiki.jpg")) { Stretch = Stretch.Fill} }.Show();
                    break;
            }
        }

        // ReSharper disable once UnusedParameter.Local
        private void MainMenuImage_OnPointerEnter(object? sender, PointerEventArgs e)
        {
            var player = new Player();
            player.Play("../../../Assets/rotating_sound.mp3");
        }

        // ReSharper disable once UnusedParameter.Local
        private void SettingsButton_OnPointerEnter(object? sender, PointerEventArgs e)
        {
            var button = (Button)sender!;
            button.Content = "На стройки";
        }

        // ReSharper disable once UnusedParameter.Local
        private void SettingsButton_OnPointerLeave(object? sender, PointerEventArgs e)
        {
            var button = (Button)sender!;
            button.Content = "Настройки";
        }
    }
}