using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

using Bitmap = Avalonia.Media.Imaging.Bitmap;
using Piece = Avalonia.Controls.Image;

namespace checkers;



public partial class GameWindow : Window
{
    private List<List<string>> _field = new List<List<string>>(10);
    private Grid _fieldImage = null!;
    private Canvas _canvas = null!;
    private string _toMove = "W";  // W is for white, B is for black
    private Dictionary<Pair<int, int>, List<Pair<int, int>>> _possibleMoves = new Dictionary<Pair<int, int>, List<Pair<int, int>>>();
    public GameWindow()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        _canvas = this.Find<Canvas>("CanvasDrag");
        _fieldImage = this.Find<Grid>("Field");
        _fieldImage.AddHandler(Avalonia.Input.DragDrop.DropEvent, DragDrop);
        _fieldImage.AddHandler(Avalonia.Input.DragDrop.DragOverEvent, DragOver);
        
        // make a field
        for (var i = 0; i < 10; ++i)
        {
            _field.Add(new List<string>(10));
            for (var j = 0; j < 10; ++j)
            {
                _field[i].Add(".");
            }
        }
        
        // fill the field with the pieces
        var b = 0;
        var w = 0;
        for (var i = 0; i < 8; ++i)  // row
        {
            for (var j = 0; j < 8; ++j)  // column
            {
                
                // add border to the field
                var border = new Border
                {
                    Name = $"S{i}{j}",  // stands for Square[i][j]
                    Background = (i + j) % 2 == 0 ? SolidColorBrush.Parse("White") : SolidColorBrush.Parse("Brown")
                };
                Grid.SetRow(border, i);
                Grid.SetColumn(border, j);
                _fieldImage.Children.Add(border);
                
                // add a piece
                if ((i + j) % 2 != 0)
                {
                    switch (i)
                    {
                        case < 3:
                        {
                            _field[i + 1][j + 1] = "B";
                            var blackPiece = new Piece
                            {
                                Name = $"B{b++}",
                                Source = new Bitmap("/home/heapof/CLionProjects/checkers/Assets/sprite_black.png")
                            };
                            blackPiece.PointerPressed += DragStart;
                            Grid.SetRow(blackPiece, i);
                            Grid.SetColumn(blackPiece, j);
                            _fieldImage.Children.Add(blackPiece);
                            break;
                        }
                        case > 4:
                            _field[i + 1][j + 1] = "W";
                            var whitePiece = new Piece
                            {
                                Name = $"W{w++}",
                                Source = new Bitmap("/home/heapof/CLionProjects/checkers/Assets/sprite_white.png")
                            };
                            whitePiece.PointerPressed += DragStart;
                            Grid.SetRow(whitePiece, i);
                            Grid.SetColumn(whitePiece, j);
                            _fieldImage.Children.Add(whitePiece);
                            break;
                    }
                }
                
            }
        }
        
        Avalonia.Input.DragDrop.SetAllowDrop(_fieldImage, true);
    }

    private async void DragStart(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;  // only left button moves the pieces
        var piece = (Piece)sender!;
        var draggable = new Piece
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
        var row = (int)e.GetPosition(_fieldImage).Y / 100;
        var column = (int)e.GetPosition(_fieldImage).X / 100;
        obj.Set("Pair<Piece, Pair<int, int>>", new Pair<Piece, Pair<int, int>>(piece, new Pair<int, int>(row, column)));
        Console.WriteLine($"\nDragStart: {piece.Name}");
        await Avalonia.Input.DragDrop.DoDragDrop(e, obj, DragDropEffects.Move);
    }
    
    private void DragOver(object? sender, DragEventArgs e)
    {
        var currentPosition = e.GetPosition(_canvas);
        var (piece, _) = (Pair<Piece, Pair<int, int>>)e.Data.Get("Pair<Piece, Pair<int, int>>")!;
        piece.IsVisible = false;
        var draggable = (Piece)_canvas.Children[1];
        draggable.IsVisible = true;
        Canvas.SetLeft(draggable, currentPosition.X - 50);
        Canvas.SetTop(draggable, currentPosition.Y - 50);
    }

    private void DragDrop(object? sender, DragEventArgs e)
    {
        _canvas.Children.RemoveRange(1, _canvas.Children.Count - 1); // remove draggable
        var (piece, (startRow, startColumn)) =
            (Pair<Piece, Pair<int, int>>)e.Data.Get("Pair<Piece, Pair<int, int>>")!;
        var finishRow = (int)e.GetPosition(_fieldImage).Y / 100;
        var finishColumn = (int)e.GetPosition(_fieldImage).X / 100;
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
        
        // handle capturing
        Piece? toCapture = null;
        if (finishColumn == startColumn - 2)
        {
            toCapture = _toMove == "W" ? GetPiece(finishRow + 1, finishColumn + 1)
                : GetPiece(finishRow - 1, finishColumn + 1);
        }
        else if (finishColumn == startColumn + 2)
        {
            toCapture = _toMove == "W" ? GetPiece(finishRow + 1, finishColumn - 1)
                : GetPiece(finishRow - 1, finishColumn - 1);
        }

        if (toCapture != null)
        {
            _fieldImage.Children.Remove(toCapture);
        }
        
        // change the piece position
        _fieldImage.Children.Remove(piece);
        Grid.SetRow(piece, finishRow);
        Grid.SetColumn(piece, finishColumn);
        _fieldImage.Children.Add(piece);
        
        // if no follow-up is available, pass the move
        _toMove = _toMove == "W" ? "B" : "W";
        UpdateMoves();
        // if there is a follow-up, continue the move
        // generate possible moves
        Console.WriteLine($"DragEnd, droped: Row {finishRow} Column {finishColumn}");
    }

    private void UpdateMoves(Pair<int, int>? followUp=null)
    {
        _possibleMoves.Clear();
        if (followUp != null)
        {
            return;
        }

        for (var i = 1; i < 9; ++i)
        {
            for (var j = 1; j < 9; ++j)
            {
                if (_field[i][j] != _toMove)
                {
                    continue;
                }

                var coordinates = new Pair<int, int>(i, j);
                _possibleMoves.Add(coordinates, new List<Pair<int, int>>());
                if (_field[i - 1][j - 1] != _toMove)
                {
                    _possibleMoves[coordinates].Add(new Pair<int, int>(i - 2, j - 2));
                }
                if (_field[i - 1][j + 1] != _toMove)
                {
                    _possibleMoves[coordinates].Add(new Pair<int, int>(i - 2, j + 2));
                }
                if (_field[i + 1][j - 1] != _toMove)
                {
                    _possibleMoves[coordinates].Add(new Pair<int, int>(i + 2, j - 2));
                }
                if (_field[i + 1][j + 1] != _toMove)
                {
                    _possibleMoves[coordinates].Add(new Pair<int, int>(i + 2, j + 2));
                }
                
                foreach (var move in new List<Pair<int, int>>(_possibleMoves[coordinates]))
                {
                    if (move.First is < 1 or > 8 || move.Second is < 1 or > 8)
                    {
                        _possibleMoves[coordinates].Remove(move);
                    }
                }

                if (_possibleMoves[coordinates].Count == 0)
                {
                    if (_toMove == "W")
                    {
                        _possibleMoves[coordinates].Add(new Pair<int, int>(i - 1, j - 1));
                        _possibleMoves[coordinates].Add(new Pair<int, int>(i - 1, j + 1));
                    }
                    else
                    {
                        _possibleMoves[coordinates].Add(new Pair<int, int>(i + 1, j - 1));
                        _possibleMoves[coordinates].Add(new Pair<int, int>(i + 1, j + 1));
                    }
                }

                foreach (var move in new List<Pair<int, int>>(_possibleMoves[coordinates]))
                {
                    if (move.First is < 1 or > 8 || move.Second is < 1 or > 8)
                    {
                        _possibleMoves[coordinates].Remove(move);
                    }
                }
            }
        }
    }
    private Piece? GetPiece(int row, int column)
    {
        return (from element in _fieldImage.Children where !element.ToString()!.EndsWith("Border") select (Piece)element).FirstOrDefault(piece => Grid.GetRow(piece) == row && Grid.GetColumn(piece) == column);
    }
}
