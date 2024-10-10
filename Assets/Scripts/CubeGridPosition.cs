using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CubeGridPosition 
{ 
    [SerializeField] private CubeGrid _cubeGrid;
    [SerializeField] private Grid<CubeGridPosition> _grid;
        
    [SerializeField] private int _x; 
    [SerializeField] private int _y;
        
    public CubeGridPosition(Grid<CubeGridPosition> grid, int x, int y) 
    {
        _grid = grid;
        _x = x;
        _y = y;
    }

    public void SetCubeGrid(CubeGrid cubeGrid) 
    {
        _cubeGrid = cubeGrid;
    }

    public int GetX() => _x;

    public int GetY() => _y;

    public Vector3 GetWorldPosition() => _grid.GetWorldPosition(_x, _y);

    public CubeGrid GetCubeGrid() => _cubeGrid;

    public void ClearCubeGrid() 
    {
        _cubeGrid = null;
    }

    public void DestroyCube(bool immediately) 
    {
        _cubeGrid?.Destroy(immediately);
    }

    public bool HasCubeGrid() 
    {
        return _cubeGrid != null;
    }

    public bool IsEmpty() 
    {
        return _cubeGrid == null;
    }

    public override string ToString() => _cubeGrid?.ToString();
}

[Serializable]
public class GridPositions<TCubeGridPosition>
{
    public List<TCubeGridPosition> positionsList;
}
