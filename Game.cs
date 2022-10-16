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
        public Piece(Cell occupiedCell, Color pieceColor, bool king = false)
        {
            OccupiedCell = occupiedCell;
            PieceColor = pieceColor;
            King = king;
        }

        public Cell OccupiedCell { get; set; }
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

    public Dictionary<Piece, List<Cell>> LegalMoves = new Dictionary<Piece, List<Cell>>();
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
                if (_field[i][j].CellColor != Color.Black) continue;
                switch (i)
                {
                    case <= 2:
                        _pieces.Add(new Piece(_field[i][j], Color.Black));
                        break;
                    case >= 5:
                        _pieces.Add(new Piece(_field[i][j], Color.White));
                        break;
                }
                _field[i][j].LinkedPiece = _pieces[^1];
            }
        }
    }

    public List<Piece> GetPieces()
    {
        return _pieces.ToList();
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

    private List<Cell> GetAvailableMoves(Piece piece)
    {
        var result = new List<Cell>();
        var x = piece.OccupiedCell.X;
        var y = piece.OccupiedCell.Y;
        var k = 1;
        
        foreach (var i in new List<int> { -1, 1 })
        {
            if (!piece.King && piece.PieceColor == Color.Black)
            {
                continue;
            }
            
            foreach (var j in new List<int> { -1, 1 })
            {
                while (x + k * i >= 0 && x + k * i < _height && y + k * j >= 0 && y + k * j < _width)
                {
                    if (_field[x + k*i][y + k*j].LinkedPiece == null)
                    {
                        result.Add(_field[x + k*i][y + k*j]);
                    } else if (x + (k + 1)*i >= 0 && x + (k + 1)*i < _height &&
                               y + (k + 1)*j >= 0 && y + (k + 1)*j < _width)
                    {
                        result.Add(_field[x + (k + 1)*i][y + (k + 1)*j]);
                    }
                    k++;
                    if (!piece.King)
                    {
                        break;
                    }
                }
            }
            if (!piece.King)
            {
                break;
            }
        }
        // if (piece.King)
        // {
        //     
        // }
        // else
        // {
        //     foreach (var j in new List<int> { -1, 1 })
        //     {
        //         if ((y == 0 && j == -1) || (y == _width - 1 && j == 1))
        //         {
        //             // проверяем, что не вышли за границы поля
        //             continue;
        //         }
        //
        //         var i = piece.PieceColor == Color.White ? -1 : 1;
        //         if (_field[x + i][y + j].LinkedPiece == null)
        //         {
        //             result.Add(_field[x + i][y + j]);
        //         } else if (x + 2*i >= 0 && x + 2*i < _height && y + 2*j >= 0 && y + 2*j < _width)
        //         {
        //             result.Add(_field[x + 2*i][y + 2*j]);
        //         }
        //     }
        // }
        return result;
    }

    public void UpdateMoves()
    {
        foreach (var piece in _pieces.Where(piece => (int)piece.PieceColor == (int)_gameStatus))
        {
            LegalMoves.Add(piece, GetAvailableMoves(piece));
        }
    }

    // private List<Pair<int, int>> 
}