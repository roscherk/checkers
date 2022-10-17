using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

using Bitmap = Avalonia.Media.Imaging.Bitmap;
using Image = Avalonia.Controls.Image;

namespace checkers;

public partial class GameWindow : Window
{
    private Grid _fieldImage = null!;
    private Canvas _canvas = null!;
    private RelativePanel _screen = null!;
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
        _screen = this.Find<RelativePanel>("Screen");
        _canvas = this.Find<Canvas>("CanvasDrag");
        _fieldImage = this.Find<Grid>("Field");
        _fieldImage.AddHandler(Avalonia.Input.DragDrop.DropEvent, DragDrop);
        _fieldImage.AddHandler(Avalonia.Input.DragDrop.DragOverEvent, DragOver);

        // отрисовываем поле
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

        // заполняем поле фигурами
        foreach (var piece in _game.GetPieces())
        {
            var pieceImage = new Image
            {
                Source = new Bitmap(
                    $"../../../Assets/sprite_{(piece.PieceColor == Game.Color.White ? "white" : "black")}.png"),
            };
            pieceImage.PointerPressed += DragStart;
            Grid.SetRow(pieceImage, piece.X);
            Grid.SetColumn(pieceImage, piece.Y);
            _fieldImage.Children.Add(pieceImage);
        }
        _game.UpdateMoves();  // сразу задаём возможные ходы для белых

        Avalonia.Input.DragDrop.SetAllowDrop(_fieldImage, true);
    }

    private async void DragStart(object? sender, PointerPressedEventArgs e)
    {
        var gameStatus = _game.GetStatus();
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed ||
            (gameStatus != Game.GameStatus.BlackMove &&
             gameStatus != Game.GameStatus.WhiteMove)) return;
        var piece = (Image)sender!;

        var row = (int)e.GetPosition(_fieldImage).Y / 100;
        var column = (int)e.GetPosition(_fieldImage).X / 100;
        var linkedPiece = _game.GetCell(row, column).LinkedPiece ??
                          new Game.Piece(0, 0, Game.Color.Black);
        if ((int)linkedPiece.PieceColor != (int)_game.GetStatus()) return;
        
        if (!_game.LegalMoves.ContainsKey(linkedPiece))
        {
            //TODO: добавить анимацию, показывающую невозможность хода
            return;
        }
        
        // начинаем перетаскивание
        var draggable = new Image
        {
            Name = gameStatus == Game.GameStatus.WhiteMove ? "Wdrag" : "Bdrag",
            Source = new Bitmap(
                $"../../../Assets/sprite_{(gameStatus == Game.GameStatus.WhiteMove ? "white" : "black")}{(linkedPiece.King ? "_king" : "")}.png"),
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
        var (piece, linkedPiece) = (Pair<Image, Game.Piece>)e.Data.Get("Pair<Image image, Game.Piece piece>")!;
        var finishX = (int)e.GetPosition(_fieldImage).Y / 100;
        var finishY = (int)e.GetPosition(_fieldImage).X / 100;
        piece.IsVisible = true;
        // проверяем, что ход возможен
        if (!_game.LegalMoves[linkedPiece].Keys.Contains(_game.GetCell(finishX, finishY))) return;

        // обрабатываем взятие
        var toCapture = _game.LegalMoves[linkedPiece][_game.GetCell(finishX, finishY)];
        if (toCapture != null)
        {
            _fieldImage.Children.Remove(GetPieceImage(toCapture));
            _game.GetCell(toCapture.X, toCapture.Y).LinkedPiece = null;
            _game.GetPieces().Remove(toCapture);
        }
        
        // меняем координаты шашки в массиве
        _game.GetCell(linkedPiece.X, linkedPiece.Y).LinkedPiece = null;
        (linkedPiece.X, linkedPiece.Y) = (finishX, finishY);
        _game.GetCell(finishX, finishY).LinkedPiece = linkedPiece;

        // меняем положение шашки на экране
        _fieldImage.Children.Remove(piece);
        Grid.SetRow(piece, finishX);
        Grid.SetColumn(piece, finishY);
        _fieldImage.Children.Add(piece);
        // если дошли до последней горизонтали, становимся дамкой
        if ((linkedPiece.X == 0 && linkedPiece.PieceColor == Game.Color.White)
            || (linkedPiece.X == FieldHeight - 1 && linkedPiece.PieceColor == Game.Color.Black))
        {
            linkedPiece.King = true;
            GetPieceImage(linkedPiece)!.Source =
                new Bitmap($"../../../Assets/sprite_{(linkedPiece.PieceColor == Game.Color.White ? "white" : "black")}_king.png");
        }

        //TODO: проверить, есть ли продолжение взятия
        if (toCapture != null)  // если взяли на этом ходу, нужно проверить, есть ли продолжение взятия
        {
            _game.UpdateMoves();
            if (_game.LegalMoves.ContainsKey(linkedPiece)
                && _game.LegalMoves[linkedPiece].Any(move => move.Value != null)) return;
        }
        // если продолжения нету, передаём ход
        _game.PassTheMove();
        if (_game.GetStatus() is Game.GameStatus.WhiteMove or Game.GameStatus.BlackMove) return;
        //TODO: сделать статус-бар и написать сообщение о победе в него
        var gameResultBorder = new Border
        {
            BorderBrush = SolidColorBrush.Parse("Black"),
            BorderThickness = Avalonia.Thickness.Parse("1"),
            Background = SolidColorBrush.Parse("AntiqueWhite"),
            CornerRadius = Avalonia.CornerRadius.Parse("3")
        };
        var gameResult = new TextBlock
        {
            Height = 40,
            Width = 200,
            // Background = SolidColorBrush.Parse("#4DA6FF"),
            TextAlignment = TextAlignment.Center,
            FontSize = 24,
            Text = "Победа белых!"
        };
        gameResultBorder.Child = gameResult;
        _screen.Children.Add(gameResultBorder);
        RelativePanel.SetAlignHorizontalCenterWithPanel(gameResultBorder, true);
        RelativePanel.SetAlignVerticalCenterWithPanel(gameResultBorder, true);
        Console.WriteLine($"DragEnd, droped: Row {finishX} Column {finishY}");
    }

    private Image? GetPieceImage(Game.Piece piece)
    {
        return (from element in _fieldImage.Children
            where !element.ToString()!.EndsWith("Border")
            select (Image)element).FirstOrDefault(pieceImage =>
            Grid.GetRow(pieceImage) == piece.X && Grid.GetColumn(pieceImage) == piece.Y);
    }
}
