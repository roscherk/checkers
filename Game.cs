using System.Collections.Generic;

namespace checkers;

public class Game
{
    public enum Color
    {
        White,
        Black
    }

    public struct Cell
    {
        public int X { get; private set; }
        public int Y { get; private set; }
        public Color CellColor { get; private set; }

        public Cell(int x, int y, Color cellColor)
        {
            X = x;
            Y = y;
            CellColor = cellColor;
        }
    }
    
    public struct Piece
    {
        public Cell OccupiedCell { get; set; }
        public Color PieceColor { get; private set; }
        public bool King { get; private set; }

        public Piece(Cell occupiedCell, Color pieceColor, bool king=false)
        {
            OccupiedCell = Field[occupiedCell.X][occupiedCell.Y];
            PieceColor = pieceColor;
            King = king;
        }
    }
    public enum GameStatus
    {
        WhiteMove,
        BlackMove,
        WhiteWin,
        BlackWin,
        Draw
    }

    private static readonly List<List<Cell>> Field = new List<List<Cell>>();
    private readonly List<Piece> _pieces = new List<Piece>();
    private Dictionary<Piece, List<Pair<int, int>>> _legalMoves = new Dictionary<Piece, List<Pair<int, int>>>();
    private readonly int _height;
    private readonly int _width;

    public Game(int height, int width)
    {
        _height = height;
        _width = width;
        for (var i = 0; i < _height; ++i)
        {
            Field.Add(new List<Cell>(_width));
            for (var j = 0; j < _width; ++j)
            {
                Field[i].Add(new Cell(i, j, (i + j) % 2 == 0 ? Color.White : Color.Black));
                switch (i)
                {
                    case <= 2 when Field[i][j].CellColor == Color.Black:
                        _pieces.Add(new Piece(Field[i][j], Color.Black));
                        break;
                    case >= 5 when Field[i][j].CellColor == Color.Black:
                        _pieces.Add(new Piece(Field[i][j], Color.White));
                        break;
                }
            }
        }
    }

    public void UpdateMoves()
    {
        return;
    }
    
    // private List<Pair<int, int>> 
}