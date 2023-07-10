using System.Collections.Generic;
using System.Linq;

namespace checkers;

public class Game
{
    public enum Color
    {
        White,
        Black
    }

    public class Cell
    {
        public int X { get; private set; }
        public int Y { get; private set; }
        public Color CellColor { get; private set; }
        public Piece? LinkedPiece { get; set; }

        public Cell(int x, int y)
        {
            X = x;
            Y = y;
            CellColor = (x + y) % 2 == 0 ? Color.White : Color.Black;
            LinkedPiece = null;
        }
    }

    public class Piece
    {
        public Piece(int x, int y, Color pieceColor, bool king = false)
        {
            X = x;
            Y = y;
            PieceColor = pieceColor;
            King = king;
            MovingUp = pieceColor == Color.White;
        }

        public int X { get; set; }
        public int Y { get; set; }
        public Color PieceColor { get; private set; }
        public bool King { get; set; }
        
        public bool MovingUp { get; set; }
    }

    public enum GameStatus
    {
        WhiteMove,
        BlackMove,
        WhiteVictory,
        BlackVictory,
        Draw
    }

    public readonly Dictionary<Piece, Dictionary<Cell, Piece?>> LegalMoves = new ();
    private readonly List<List<Cell>> _field = new ();
    public bool BoardFlipped = false;
    private readonly List<Piece> _pieces = new ();
    private readonly int _height;
    private readonly int _width;
    private GameStatus _gameStatus = GameStatus.WhiteMove;

    public Game(int height, int width)
    {
        _height = height;
        _width = width;
        for (var i = 0; i < _height; ++i)
        {
            _field.Add(new List<Cell>(_width));
            for (var j = 0; j < _width; ++j)
            {
                _field[i].Add(new Cell(i, j));
                if (_field[i][j].CellColor != Color.Black || i is > 2 and < 5) continue;
                _pieces.Add(new Piece(i, j, i <= 2 ? Color.Black : Color.White));
                _field[i][j].LinkedPiece = _pieces[^1];
            }
        }
    }

    public List<Piece> GetPieces()
    {
        return _pieces;
    }

    public Cell GetCell(int x, int y)
    {
        return _field[x][y];
    }

    public GameStatus GetStatus()
    {
        return _gameStatus;
    }

    public void MakeDraw()
    {
        _gameStatus = GameStatus.Draw;
    }

    public void Concede()
    {
        _gameStatus = _gameStatus == GameStatus.WhiteMove ? GameStatus.BlackVictory : GameStatus.WhiteVictory;
    }
    public void PassTheMove()
    {
        // передаём ход сопернику
        _gameStatus = _gameStatus == GameStatus.WhiteMove ? GameStatus.BlackMove : GameStatus.WhiteMove;
        UpdateMoves();
        if (LegalMoves.Count > 0) return;  // если у него есть ходы, продолжаем игру
        // если же нет, смотрим, есть ли ходы у нас
        _gameStatus = _gameStatus == GameStatus.WhiteMove ? GameStatus.BlackMove : GameStatus.WhiteMove;
        UpdateMoves();
        if (LegalMoves.Count > 0)
        {
            // если есть, то мы выиграли
            _gameStatus = _gameStatus == GameStatus.WhiteMove ? GameStatus.WhiteVictory : GameStatus.BlackVictory;
        }
        else
        {
            // если и у нас нет ходов, то на доске ничья
            _gameStatus = GameStatus.Draw;
        }
        LegalMoves.Clear();
    }

    private Dictionary<Cell, Piece?> GetAvailableMoves(Piece piece)
    {
        var result = new Dictionary<Cell, Piece?>();
        var x = piece.X;
        var y = piece.Y;
        
        foreach (var i in new List<int> { -1, 1 })  // задаёт направление хода: -1 -- вверх, 1 -- вниз
        {
            foreach (var j in new List<int> { -1, 1 })  // задаёт направление хода: -1 -- влево, 1 -- вправо
            {
                var k = 1;  // перебираем количество клеток, на которое можем пойти в выбранном направлении
                while (x + k * i >= 0 && x + k * i < _height && y + k * j >= 0 && y + k * j < _width)
                {
                    if (_field[x + k*i][y + k*j].LinkedPiece == null)
                    {
                        // если клетка пустая, мы можем на неё сходить
                        if (piece.King || (piece.MovingUp && i == -1) || (!piece.MovingUp && i == 1))
                        {
                            result.Add(_field[x + k*i][y + k*j], null);
                        }
                    } else if (_field[x + k*i][y + k*j].LinkedPiece!.PieceColor != piece.PieceColor)
                    {
                        var t = 1;  // перебираем возможные варианты взятия
                        while (x + (k + t) * i >= 0 
                               && x + (k + t) * i < _height
                               && y + (k + t) * j >= 0
                               && y + (k + t) * j < _width 
                               && _field[x + (k + t) * i][y + (k + t) * j].LinkedPiece == null)
                        {
                            t += 1;
                        }
                        t -= 1;  // последняя свободная клетка, в которую ещё можем попасть после взятия
                        for (var m = 1; m <= t; ++m)
                        {
                            result.Add(_field[x + (k + m)*i][y + (k + m)*j], _field[x + k*i][y + k*j].LinkedPiece);
                            if (!piece.King)
                            {
                                break;
                            }
                        }
                        break;
                    }
                    else
                    {
                        break;
                    }

                    if (!piece.King)
                    {
                        break;
                    }

                    k++;
                }
            }
        }
        return result;
    }

    public void UpdateMoves()
    {
        LegalMoves.Clear();
        foreach (var piece in _pieces.Where(piece => (int)piece.PieceColor == (int)_gameStatus))
        {
            var moves = GetAvailableMoves(piece);
            if (moves.Count > 0)
            {
                LegalMoves.Add(piece, moves);
            }
        }
        if (LegalMoves.Values.All(move => move.Values.All(capture => capture == null))) return;
        // убираем все ходы не-взятия
        foreach (var (piece, value)
                 in LegalMoves.Where(moves => moves.Value.Any(move => move.Value == null)))
        {
            foreach (var move in value.Where(move => move.Value == null))
            {
                value.Remove(move.Key);
            }

            if (value.Count == 0)
            {
                LegalMoves.Remove(piece);
            }
        }
    }
}
