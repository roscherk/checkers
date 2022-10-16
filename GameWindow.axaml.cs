using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;


using Bitmap = Avalonia.Media.Imaging.Bitmap;
using Image = Avalonia.Controls.Image;

namespace checkers;



public partial class GameWindow : Window
{
    private Grid _fieldImage = null!;
    private Canvas _canvas = null!;
    private readonly Game _game;
    private const int FieldHeight = 8;
    private const int FieldWidth = 8;

    public GameWindow()
    {
        _game = new Game(FieldHeight, FieldWidth);  //TODO: добавить возможность настраивать размеры поля
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        _canvas = this.Find<Canvas>("CanvasDrag");
        _fieldImage = this.Find<Grid>("Field");
        _fieldImage.AddHandler(Avalonia.Input.DragDrop.DropEvent, DragDrop);
        _fieldImage.AddHandler(Avalonia.Input.DragDrop.DragOverEvent, DragOver);

        // render the field image
        for (var i = 0; i < FieldHeight; ++i)
        {
            for (var j = 0; j < FieldWidth; ++j)
            {
                var border = new Border
                {
                    Name = $"S{i}{j}",
                    Background = (i + j) % 2 == 0 ? SolidColorBrush.Parse("White") : SolidColorBrush.Parse("Brown")
                };
                Grid.SetRow(border, i);
                Grid.SetColumn(border, j);
                _fieldImage.Children.Add(border);
            }
        }

        // fill the field with the pieces
        foreach (var piece in _game.GetPieces())
        {
            var pieceImage = new Image
            {
                Source = new Bitmap(
                    $"/home/heapof/CLionProjects/checkers/Assets/sprite_{(piece.PieceColor == Game.Color.White ? "white" : "black")}.png"),
            };
            pieceImage.PointerPressed += DragStart;
            Grid.SetRow(pieceImage, piece.OccupiedCell.X);
            Grid.SetColumn(pieceImage, piece.OccupiedCell.Y);
            _fieldImage.Children.Add(pieceImage);
        }

        Avalonia.Input.DragDrop.SetAllowDrop(_fieldImage, true);
    }

    private async void DragStart(object? sender, PointerPressedEventArgs e)
    {
        var gameStatus = _game.GetStatus();
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed ||
            (gameStatus != Game.GameStatus.BlackMove &&
             gameStatus != Game.GameStatus.WhiteMove)) return;
        var piece = (Image)sender!;
        if (piece.Name!.StartsWith("W") && gameStatus != Game.GameStatus.WhiteMove
            || piece.Name.StartsWith("B") && gameStatus != Game.GameStatus.BlackMove) return;  // player can move their pieces only

        _game.UpdateMoves();
        
        var row = (int)e.GetPosition(_fieldImage).Y / 100;
        var column = (int)e.GetPosition(_fieldImage).X / 100;
        var linkedPiece = _game.GetCell(row, column).LinkedPiece ?? new Game.Piece(new Game.Cell(0, 0), Game.Color.Black);
        if (!_game.LegalMoves.ContainsKey(linkedPiece))
        {
            //TODO: добавить анимацию, показывающую невозможность хода
            return;
        }
        
        // start dragging
        var draggable = new Image
        {
            Name = gameStatus == Game.GameStatus.WhiteMove ? "Wdrag" : "Bdrag",
            Source = new Bitmap($"/home/heapof/CLionProjects/checkers/Assets/sprite_{(gameStatus == Game.GameStatus.WhiteMove ? "white" : "black")}.png"),
            Height = 100,
            Width = 100,
            IsEnabled = false,
            IsVisible = false
        };
        _canvas.Children.Add(draggable);
        var obj = new DataObject();
        obj.Set("Pair<Image image, Game.Piece piece>", new Pair<Image, Game.Piece>(piece, linkedPiece));
        Console.WriteLine($"\nDragStart: {piece.Name}");
        await Avalonia.Input.DragDrop.DoDragDrop(e, obj, DragDropEffects.Move);
    }

    private void DragOver(object? sender, DragEventArgs e)
    {
        var currentPosition = e.GetPosition(_canvas);
        var (piece, _) = (Pair<Image, Game.Piece>)e.Data.Get("Pair<Image image, Game.Piece piece>")!;
        piece.IsVisible = false;
        var draggable = (Image)_canvas.Children[1];
        draggable.IsVisible = true;
        Canvas.SetLeft(draggable, currentPosition.X - 50);
        Canvas.SetTop(draggable, currentPosition.Y - 50);
    }

    private void DragDrop(object? sender, DragEventArgs e)
    {
        _canvas.Children.RemoveRange(1, _canvas.Children.Count - 1); // remove draggable
        var (piece, linkedPiece) =
            (Pair<Image, Game.Piece>)e.Data.Get("Pair<Image image, Game.Piece piece>")!;
        var finishRow = (int)e.GetPosition(_fieldImage).Y / 100;
        var finishColumn = (int)e.GetPosition(_fieldImage).X / 100;
        piece.IsVisible = true;
        // checking if the move is legal
        if (!e.Source!.ToString()!.EndsWith("Border")
            || _game.LegalMoves[linkedPiece].Contains(new Game.Cell(finishRow, finishColumn))) return;
        // Console.WriteLine($"startRow = {startRow}, finishRow = {finishRow}");
        // Console.WriteLine($"startColumn = {startColumn}, finishColumn = {finishColumn}");
        
        // handle capturing
        Image? toCapture = null;
        
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
        // if there is a follow-up, continue the move
        // generate possible moves
        Console.WriteLine($"DragEnd, droped: Row {finishRow} Column {finishColumn}");
    }

    // private void UpdateMoves(Pair<int, int>? followUp=null)
    // {
    //     _possibleMoves.Clear();
    //     if (followUp != null)
    //     {
    //         return;
    //     }
    //
    //     for (var i = 1; i < 9; ++i)
    //     {
    //         for (var j = 1; j < 9; ++j)
    //         {
    //             if (_field[i][j] != _toMove)
    //             {
    //                 continue;
    //             }
    //
    //             var coordinates = new Pair<int, int>(i, j);
    //             _possibleMoves.Add(coordinates, new List<Pair<int, int>>());
    //             if (_field[i - 1][j - 1] != _toMove)
    //             {
    //                 _possibleMoves[coordinates].Add(new Pair<int, int>(i - 2, j - 2));
    //             }
    //             if (_field[i - 1][j + 1] != _toMove)
    //             {
    //                 _possibleMoves[coordinates].Add(new Pair<int, int>(i - 2, j + 2));
    //             }
    //             if (_field[i + 1][j - 1] != _toMove)
    //             {
    //                 _possibleMoves[coordinates].Add(new Pair<int, int>(i + 2, j - 2));
    //             }
    //             if (_field[i + 1][j + 1] != _toMove)
    //             {
    //                 _possibleMoves[coordinates].Add(new Pair<int, int>(i + 2, j + 2));
    //             }
    //             
    //             foreach (var move in new List<Pair<int, int>>(_possibleMoves[coordinates]))
    //             {
    //                 if (move.First is < 1 or > 8 || move.Second is < 1 or > 8)
    //                 {
    //                     _possibleMoves[coordinates].Remove(move);
    //                 }
    //             }
    //
    //             if (_possibleMoves[coordinates].Count == 0)
    //             {
    //                 if (_toMove == "W")
    //                 {
    //                     _possibleMoves[coordinates].Add(new Pair<int, int>(i - 1, j - 1));
    //                     _possibleMoves[coordinates].Add(new Pair<int, int>(i - 1, j + 1));
    //                 }
    //                 else
    //                 {
    //                     _possibleMoves[coordinates].Add(new Pair<int, int>(i + 1, j - 1));
    //                     _possibleMoves[coordinates].Add(new Pair<int, int>(i + 1, j + 1));
    //                 }
    //             }
    //
    //             foreach (var move in new List<Pair<int, int>>(_possibleMoves[coordinates]))
    //             {
    //                 if (move.First is < 1 or > 8 || move.Second is < 1 or > 8)
    //                 {
    //                     _possibleMoves[coordinates].Remove(move);
    //                 }
    //             }
    //         }
    //     }
    // }
}
