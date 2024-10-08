using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public class Grid<TGridObject> 
{
    public class OnGridObjectChangedEventArgs : EventArgs { }

    [SerializeField] private int _width;
    [SerializeField] private int _height;
    [SerializeField] private float _cellSize;
    [SerializeField] private Vector3 _originPosition;
    [SerializeField] private TGridObject[,] _gridArray;
    
    public Dictionary<int2, int> _gridObjectId = new Dictionary<int2, int>(); 

    public Grid(int width, int height, float cellSize, Vector3 originPosition, Func<Grid<TGridObject>, int, int, TGridObject> createGridObject) 
    {
        _width = width;
        _height = height;
        _cellSize = cellSize;
        _originPosition = originPosition;

        _gridArray = new TGridObject[_width, _height];

        var t = 0;
        for (var x = 0; x < _gridArray.GetLength(0); x++) 
        {
            for (var y = 0; y < _gridArray.GetLength(1); y++) 
            {
                _gridArray[x, y] = createGridObject(this, x, y);
                
                var gridPos = new int2(x, y);
                
                _gridObjectId.Add(gridPos, t);
                t++;
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

    public int GetGridID(int x, int y)
    {
        var gridPos = new int2(x, y);

        _gridObjectId.TryGetValue(gridPos, out var id);
        return id;
    }

    public TGridObject GetGridObject(int x, int y)
    {
        if (x >= 0 && y >= 0 && x < _width && y < _height) 
        {
            return _gridArray[x, y];
        }
        return default;
    }
    
    public void SetGridObject(int x, int y, TGridObject obj)
    {
        _gridArray[x, y] = obj;
    }
}
