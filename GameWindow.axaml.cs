using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Bitmap = Avalonia.Media.Imaging.Bitmap;
using Image = Avalonia.Controls.Image;

namespace checkers;

public partial class GameWindow : Window
{
    private Grid _field = null!;
    private Canvas _canvas = null!;
    private string _toMove = "W";  // W is for white, B is for black
    public GameWindow()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        _canvas = this.Find<Canvas>("CanvasDrag");
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
        var draggable = new Image
        {
            Name = _toMove == "W" ? "WDrag" : "BDrag",
            Source = new Bitmap(
                $"/home/heapof/CLionProjects/checkers/Assets/sprite_{(_toMove == "W" ? "white" : "black")}.png"),
            Height = 100,
            Width = 100,
            IsEnabled = false,
            IsVisible = false
        };
        _canvas.Children.Add(draggable);
        if (piece.Name!.StartsWith("W") && _toMove != "W"
            || piece.Name.StartsWith("B") && _toMove != "B") return;  // player can move only pieces with appropriate color
        var obj = new DataObject();
        var row = (int)e.GetPosition(_field).Y / 100;
        var column = (int)e.GetPosition(_field).X / 100;
        obj.Set("KeyValuePair<Image, KeyValuePair<int, int>>", new KeyValuePair<Image, KeyValuePair<int, int>>(piece, new KeyValuePair<int, int>(row, column)));
        Console.WriteLine($"\nDragStart: {piece.Name}");
        await Avalonia.Input.DragDrop.DoDragDrop(e, obj, DragDropEffects.Move);
    }
    
    private void DragOver(object? sender, DragEventArgs e)
    {
        var currentPosition = e.GetPosition(_canvas);
        var (piece, _) = 
            (KeyValuePair<Image, KeyValuePair<int, int>>)e.Data.Get("KeyValuePair<Image, KeyValuePair<int, int>>")!;
        piece.IsVisible = false;
        var draggable = (Image)_canvas.Children[1];
        draggable.IsVisible = true;
        Canvas.SetLeft(draggable, currentPosition.X - 50);
        Canvas.SetTop(draggable, currentPosition.Y - 50);
    }

    private void DragDrop(object? sender, DragEventArgs e)
    {
        _canvas.Children.RemoveRange(1, _canvas.Children.Count - 1);  // remove draggable
        var (piece, (startRow, startColumn)) =
            (KeyValuePair<Image, KeyValuePair<int, int>>)e.Data.Get("KeyValuePair<Image, KeyValuePair<int, int>>")!;
        var finishRow = (int)e.GetPosition(_field).Y / 100;
        var finishColumn = (int)e.GetPosition(_field).X / 100;
        piece.IsVisible = true;
        // checking if the move is legal
        if (!e.Source!.ToString()!.EndsWith("Border")
            || finishRow == startRow
            || finishColumn == startColumn
            || (finishRow + finishColumn) % 2 == 0
            || (_toMove == "W" && (finishRow > startRow || finishRow < startRow - 2))
            || (_toMove == "B" && (finishRow < startRow || finishRow > startRow + 2))) return;
        // Console.WriteLine($"startRow = {startRow}, finishRow = {finishRow}");
        // Console.WriteLine($"startColumn = {startColumn}, finishColumn = {finishColumn}");
        
        Image? toCapture = null; // checking whether we can capture somebody on this move
        if (finishColumn == startColumn - 2)
        {
            toCapture = _toMove == "W"
                ? GetPiece(finishRow + 1, finishColumn + 1)
                : GetPiece(finishRow - 1, finishColumn + 1);
        }
        else if (finishColumn == startColumn + 2)
        {
            toCapture = _toMove == "W"
                ? GetPiece(finishRow + 1, finishColumn - 1)
                : GetPiece(finishRow - 1, finishColumn - 1);
        }
        if (toCapture != null)
        {
            _field.Children.Remove(toCapture);
        }

        _field.Children.Remove(piece);
        Grid.SetRow(piece, finishRow);
        Grid.SetColumn(piece, finishColumn);
        _field.Children.Add(piece);
        _toMove = _toMove == "W" ? "B" : "W";
        Console.WriteLine($"DragEnd, droped: Row {finishRow} Column {finishColumn}");
    }
    private Image? GetPiece(int row, int column)
    {
        return (from element in _field.Children where !element.ToString()!.EndsWith("Border") select (Image)element).FirstOrDefault(piece => Grid.GetRow(piece) == row && Grid.GetColumn(piece) == column);
    }
}