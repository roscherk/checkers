using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace checkers;

public partial class GameWindow : Window
{
    public GameWindow()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}