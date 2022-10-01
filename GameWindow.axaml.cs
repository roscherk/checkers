using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace checkers;

public partial class GameWindow : Window
{
    private RelativePanel _field = null!;
    public GameWindow()
    {
        InitializeComponent();
        AddHandler(DragDrop.DropEvent, Drop);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        _field = this.Find<RelativePanel>("Field");
        DragDrop.SetAllowDrop(_field, true);
        Console.WriteLine(_field.Bounds.Size);
    }

    private void InputElement_OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void InputElement_OnPointerMoved(object? sender, PointerEventArgs e)
    {
        var image = (Image)sender!;
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        var obj = new DataObject();
        obj.Set("", image);
        DragDrop.DoDragDrop(e, obj, DragDropEffects.Move);
    }

    private void Drop(object? sender, DragEventArgs e)
    {
        Console.WriteLine("Drop");
        // Point dropPosition = e.GetPosition(Field);
        // Console.WriteLine($"{dropPosition.X}, {dropPosition.Y}");
    }
}