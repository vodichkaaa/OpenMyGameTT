using System;
using UnityEngine;

public class Grid<TGridObject> 
{

    public event EventHandler<OnGridObjectChangedEventArgs> OnGridObjectChanged;
    public class OnGridObjectChangedEventArgs : EventArgs { }

    private readonly int _width;
    private readonly int _height;
    private readonly float _cellSize;
    private readonly Vector3 _originPosition;
    private readonly TGridObject[,] _gridArray;

    public Grid(int width, int height, float cellSize, Vector3 originPosition, Func<Grid<TGridObject>, int, int, TGridObject> createGridObject) 
    {
        _width = width;
        _height = height;
        _cellSize = cellSize;
        _originPosition = originPosition;

        _gridArray = new TGridObject[_width, _height];

        for (var x = 0; x < _gridArray.GetLength(0); x++) 
        {
            for (var y = 0; y < _gridArray.GetLength(1); y++) 
            {
                _gridArray[x, y] = createGridObject(this, x, y);
            }
        }
    }

    public int GetWidth() => _width;

    public int GetHeight() => _height;

    public Vector3 GetWorldPosition(int x, int y) 
    {
        return new Vector3(x, y) * _cellSize + _originPosition;
    }

    public void GetXY(Vector3 worldPosition, out int x, out int y) 
    {
        x = Mathf.FloorToInt((worldPosition - _originPosition).x / _cellSize);
        y = Mathf.FloorToInt((worldPosition - _originPosition).y / _cellSize);
    }

    public void TriggerGridObjectChanged() 
    {
        OnGridObjectChanged?.Invoke(this, new OnGridObjectChangedEventArgs());
    }

    public TGridObject GetGridObject(int x, int y)
    {
        if (x >= 0 && y >= 0 && x < _width && y < _height) 
        {
            return _gridArray[x, y];
        }
        return default;
    }
}
