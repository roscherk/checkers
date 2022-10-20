using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace checkers;

public partial class StroikiWindow : Window
{
    private ImageBrush _path;
    public ImageBrush Path_
    {
        get => _path;
        init
        {
            _path = value;
            Background = _path;
        }
    }

    public StroikiWindow()
    {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
    }

    private void InitializeComponent()
    {
        Background = _path;
        AvaloniaXamlLoader.Load(this);
    }
}