using System;
using System.Collections.Generic;
using System.Drawing;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Animators;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Styling;
using Bitmap = Avalonia.Media.Imaging.Bitmap;
using Image = Avalonia.Controls.Image;

namespace checkers;

public partial class GameWindow : Window
{
    private Grid _field = null!;
    private string _toMove = "W";  // W is for white, B is for black
    public GameWindow()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        _field = this.Find<Grid>("Field");
        _field.AddHandler(Avalonia.Input.DragDrop.DropEvent, DragDrop);
        _field.AddHandler(Avalonia.Input.DragDrop.DragOverEvent, DragOver);
        var b = 0;
        var w = 0;
        for (var i = 0; i < 8; ++i)  // row
        {
            for (var j = 0; j < 8; ++j)  // column
            {
                var border = new Border
                {
                    Name = $"S{i}{j}",  // stands for Square[i][j]
                    Background = (i + j) % 2 == 0 ? SolidColorBrush.Parse("White") : SolidColorBrush.Parse("Brown")
                };
                Grid.SetRow(border, i);
                Grid.SetColumn(border, j);
                _field.Children.Add(border);
                
                // add pieces to the board
                if ((i + j) % 2 != 0)
                {
                    switch (i)
                    {
                        case < 3:
                        {
                            var blackPiece = new Image
                            {
                                Name = $"B{b++}",
                                Source = new Bitmap("/home/heapof/CLionProjects/checkers/Assets/sprite_black.png")
                            };
                            blackPiece.PointerPressed += DragStart;
                            Grid.SetRow(blackPiece, i);
                            Grid.SetColumn(blackPiece, j);
                            _field.Children.Add(blackPiece);
                            break;
                        }
                        case > 4:
                            var whitePiece = new Image
                            {
                                Name = $"W{w++}",
                                Source = new Bitmap("/home/heapof/CLionProjects/checkers/Assets/sprite_white.png")
                            };
                            whitePiece.PointerPressed += DragStart;
                            Grid.SetRow(whitePiece, i);
                            Grid.SetColumn(whitePiece, j);
                            _field.Children.Add(whitePiece);
                            break;
                    }
                }
                
            }
        }
        
        Avalonia.Input.DragDrop.SetAllowDrop(_field, true);
    }

    private async void DragStart(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;  // only left button moves the pieces
        var piece = (Image)sender!;
        var obj = new DataObject();
        var row = (int)e.GetPosition(_field).X / 100;
        var column = (int)e.GetPosition(_field).Y / 100;
        obj.Set("KeyValuePair<Image, KeyValuePair<int, int>>", new KeyValuePair<Image, KeyValuePair<int, int>>(piece, new KeyValuePair<int, int>(row, column)));
        Console.WriteLine($"\nDragStart: {piece.Name}");
        var result = await Avalonia.Input.DragDrop.DoDragDrop(e, obj, DragDropEffects.Move);
    }

    private void DragDrop(object? sender, DragEventArgs e)
    {
        try
        {
            var square = (Border)e.Source!;
            Console.WriteLine($"Drop: {square.Name}");

            var row = square.Name![1] - '0';
            var column = square.Name[2] - '0';
            
            if ((row + column) % 2 == 0) return;  // can't go on white squares  

            var data = (KeyValuePair<Image, KeyValuePair<int, int>>)e.Data.Get("KeyValuePair<Image, KeyValuePair<int, int>>")!;
            var piece = data.Key;
            _field.Children.Remove(piece);
            Grid.SetRow(piece, row);
            Grid.SetColumn(piece, column);
            _field.Children.Add(piece);
        }
        catch (InvalidCastException exception)
        {
            Console.WriteLine("Dropped onto another piece");
        }
    }

    private void DragOver(object? sender, DragEventArgs e)
    {
        
    }
}