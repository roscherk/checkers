using System;
using System.Linq;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Bitmap = Avalonia.Media.Imaging.Bitmap;
using Image = Avalonia.Controls.Image;

namespace checkers;

public partial class GameWindow : Window
{
    // private RelativePanel _screen = null!;
    private Canvas _canvas = null!;
    private Grid _fieldImage = null!;
    private TextBlock _statusBar = null!;
    private Pair<Canvas, Canvas> _capturedBox = null!;
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
        // _screen = this.Find<RelativePanel>("Screen");
        _canvas = this.Find<Canvas>("CanvasDrag");
        _fieldImage = this.Find<Grid>("Field");
        _statusBar = this.Find<TextBlock>("GameStatus");
        _capturedBox = new Pair<Canvas, Canvas>(this.Find<Canvas>("CapturedByWhite"),
            this.Find<Canvas>("CapturedByBlack"));
        
        _fieldImage.AddHandler(Avalonia.Input.DragDrop.DropEvent, DragDrop);
        _fieldImage.AddHandler(Avalonia.Input.DragDrop.DragOverEvent, DragOver);
        // _canvas.AddHandler(Avalonia.Input.DragDrop.DragLeaveEvent, DragLeave);

        // отрисовываем поле
        for (var i = 0; i < FieldHeight; ++i)
        {
            for (var j = 0; j < FieldWidth; ++j)
            {
                var border = new Border
                {
                    Name = $"S{i}{j}",
                    Background = (i + j) % 2 == 0 ? SolidColorBrush.Parse("White") : SolidColorBrush.Parse("Brown"),
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
                Classes = Classes.Parse("Piece")
            };
            pieceImage.PointerPressed += DragStart;
            pieceImage.PointerMoved += PieceMouseOver;
            pieceImage.PointerLeave += PieceMouseLeave;
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
        var dragResult = await Avalonia.Input.DragDrop.DoDragDrop(e, obj, DragDropEffects.Move);
        
        // если что-то пошло не так во время перетаскивания, откатываемся до первоначального состояния
        if (dragResult != DragDropEffects.None) return;
        _canvas.Children.RemoveRange(1, _canvas.Children.Count - 1); // уничтожаем draggable
        piece.IsVisible = true;
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
        var (piece, linkedPiece) = (Pair<Image, Game.Piece>)e.Data.Get("Pair<Image image, Game.Piece piece>")!;
        var finishX = (int)e.GetPosition(_fieldImage).Y / 100;
        var finishY = (int)e.GetPosition(_fieldImage).X / 100;
        _canvas.Children.RemoveRange(1, _canvas.Children.Count - 1); // уничтожаем draggable
        piece.IsVisible = true;
        // проверяем, что ход возможен
        if (!_game.LegalMoves[linkedPiece].Keys.Contains(_game.GetCell(finishX, finishY))) return;

        // обрабатываем взятие
        var capturedPiece = _game.LegalMoves[linkedPiece][_game.GetCell(finishX, finishY)];
        if (capturedPiece != null)
        {
            _fieldImage.Children.Remove(GetPieceImage(capturedPiece));
            _game.GetCell(capturedPiece.X, capturedPiece.Y).LinkedPiece = null;
            _game.GetPieces().Remove(capturedPiece);
            
            // анимируем взятие
            var capturedPieceImage = new Image
            {
                Source = new Bitmap($"../../../Assets/sprite_" +
                                    $"{(capturedPiece.PieceColor == Game.Color.White ? "white" : "black")}" +
                                    $"{(capturedPiece.King ? "_king" : "")}.png"),
                Height = 100,
                Width = 100,
                Opacity = 1,
                IsEnabled = false
            };

            var targetBox = capturedPiece.PieceColor == Game.Color.White ? _capturedBox.Second : _capturedBox.First;
            targetBox.Children.Add(capturedPieceImage);
            if (targetBox.Children.Count > 1)
            {
                Console.WriteLine((int)targetBox.Children[^2].Bounds.Center.X);
                var targetX = (int)targetBox.Children[^2].Bounds.Center.X < 150
                    ? (int)targetBox.Children[^2].Bounds.Center.X
                    : 0;
                Console.WriteLine(targetX);
                var targetY = (int)targetBox.Children[^2].Bounds.Center.X == 150 ?
                    (int)targetBox.Children[^2].Bounds.Center.Y
                    : (int)targetBox.Children[^2].Bounds.Center.Y - 50;
                Canvas.SetLeft(capturedPieceImage, targetX);
                Canvas.SetTop(capturedPieceImage, targetY);
            }

            var animation = new Animation
            {
                Duration = TimeSpan.FromSeconds(0.55),
                Children =
                {
                    new KeyFrame
                    {
                        Setters =
                        {
                            new Setter(OpacityProperty, 0.01)
                        },
                        Cue = new Cue(0)
                    }
                }
            };
            animation.RunAsync(capturedPieceImage, new Clock());
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

        if (capturedPiece != null)  // если взяли на этом ходу, нужно проверить, есть ли продолжение взятия
        {
            _game.UpdateMoves();
            if (_game.LegalMoves.ContainsKey(linkedPiece)
                && _game.LegalMoves[linkedPiece].Any(move => move.Value != null)) return;
        }
        // если продолжения нету, передаём ход
        _game.PassTheMove();
        DisplayGameStatus();
        // if (_game.GetStatus() is Game.GameStatus.WhiteMove or Game.GameStatus.BlackMove) return;
        Console.WriteLine($"DragEnd, droped: Row {finishX} Column {finishY}");
    }
    
    private void PieceMouseOver(object? sender, PointerEventArgs e)
    {
        var pieceImage = (Image)sender!;
        var x = (int)pieceImage.Bounds.Y / 100;
        var y = (int)pieceImage.Bounds.X / 100;
        var linkedPiece = _game.GetCell(x, y).LinkedPiece;
        if (linkedPiece == null) return;
        if (!_game.LegalMoves.ContainsKey(linkedPiece)) return;
        foreach (var border in from move in _game.LegalMoves[linkedPiece] from element in _fieldImage.Children where element.ToString()!.EndsWith("Border") where Grid.GetRow((Border)element) == move.Key.X && Grid.GetColumn((Border)element) == move.Key.Y select (Border)element)
        {
            border.BorderThickness = Thickness.Parse("4");
            border.BorderBrush = SolidColorBrush.Parse("Green");
        }
        pieceImage.RenderTransform = new TranslateTransform { Y = -1 };
    }

    private void PieceMouseLeave(object? sender, PointerEventArgs e)
    {
        var pieceImage = (Image)sender!;
        var x = (int)pieceImage.Bounds.Y / 100;
        var y = (int)pieceImage.Bounds.X / 100;
        var linkedPiece = _game.GetCell(x, y).LinkedPiece;
        if (linkedPiece == null) return;
        if (!_game.LegalMoves.ContainsKey(linkedPiece)) return;
        foreach (var border in from move in _game.LegalMoves[linkedPiece] from element in _fieldImage.Children where element.ToString()!.EndsWith("Border") where Grid.GetRow((Border)element) == move.Key.X && Grid.GetColumn((Border)element) == move.Key.Y select (Border)element)
        {
            border.BorderThickness = Thickness.Parse("0");
        }
        pieceImage.RenderTransform = new TranslateTransform { X = 0, Y = 0 };
    }

    private Image? GetPieceImage(Game.Piece piece)
    {
        return (from element in _fieldImage.Children
            where !element.ToString()!.EndsWith("Border")
            select (Image)element).FirstOrDefault(pieceImage =>
            Grid.GetRow(pieceImage) == piece.X && Grid.GetColumn(pieceImage) == piece.Y);
    }

    private void DisplayGameStatus()
    {
        _statusBar.Text = _game.GetStatus() switch
        {
            Game.GameStatus.WhiteMove => "Ход белых",
            Game.GameStatus.BlackMove => "Ход чёрных",
            Game.GameStatus.WhiteVictory => "Победа белых!",
            Game.GameStatus.BlackVictory => "Победа чёрных!",
            Game.GameStatus.Draw => "Ничья!",
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}
