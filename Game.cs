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
        }

        public int X { get; set; }
        public int Y { get; set; }
        public Color PieceColor { get; private set; }
        public bool King { get; set; }
    }

    public enum GameStatus
    {
        WhiteMove,
        BlackMove,
        WhiteWin,
        BlackWin,
        Draw
    }

    public Dictionary<Piece, Dictionary<Cell, Piece?>> LegalMoves = new Dictionary<Piece, Dictionary<Cell, Piece?>>();
    private readonly List<List<Cell>> _field = new List<List<Cell>>();
    private readonly List<Piece> _pieces = new List<Piece>();
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
    public void PassTheMove()
    {
        //TODO: проверить, не закончилась ли игра
        _gameStatus = _gameStatus == GameStatus.WhiteMove ? GameStatus.BlackMove : GameStatus.WhiteMove;
    }

    private Dictionary<Cell, Piece?> GetAvailableMoves(Piece piece)
    {
        var result = new Dictionary<Cell, Piece?>();
        var x = piece.X;
        var y = piece.Y;
        
        foreach (var i in new List<int> { -1, 1 })  // задаёт направление хода: -1 -- вверх, 1 -- вниз
        {
            // if (piece.PieceColor == Color.Black && i == -1 && !piece.King)
            // {
            //     continue;
            // }
            
            foreach (var j in new List<int> { -1, 1 })  // задаёт направление хода: -1 -- влево, 1 -- вправо
            {
                var k = 1;  // перебираем количество клеток, на которое можем пойти в выбранном направлении
                while (x + k * i >= 0 && x + k * i < _height && y + k * j >= 0 && y + k * j < _width)
                {
                    if (_field[x + k*i][y + k*j].LinkedPiece == null)
                    {
                        // если клетка пустая, мы можем на неё сходить
                        if (piece.King
                            || (piece.PieceColor == Color.White && i == -1)
                            || (piece.PieceColor == Color.Black && i == 1))
                        {
                            result.Add(_field[x + k*i][y + k*j], null);
                        }
                    } else if (_field[x + k*i][y + k*j].LinkedPiece!.PieceColor != piece.PieceColor)
                    {
                        var t = 1;  // перебираем возможные варианты взятия
                        //TODO: пофиксить баг со взятием только в одну сторону
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
        foreach (var moves in LegalMoves)
        {
            if (moves.Value.Any(move => move.Value == null))
            {
                foreach (var move in moves.Value)
                {
                    if (move.Value == null)
                    {
                        moves.Value.Remove(move.Key);
                    }
                }

                if (moves.Value.Count == 0)
                {
                    LegalMoves.Remove(moves.Key);
                }
            }
        }
    }

    // private List<Pair<int, int>> 
}