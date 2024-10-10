using System;
using UnityEngine;

[Serializable]
public class CubeGrid 
{
    public event Match3.BoolEventHandler OnDestroyed;

    [SerializeField] private Cube _cube;
    [SerializeField] private int _x;
    [SerializeField] private int _y;
    [SerializeField] private bool _isDestroyed;

    public CubeGrid(Cube cube, int x, int y) 
    {
        _cube = cube;
        _x = x;
        _y = y;

        _isDestroyed = false;
    }
        
    public Cube GetCube() => _cube;

    public Vector3 GetWorldPosition() 
    {
        return new Vector3(_x, _y);
    }

    public void SetCubeXY(int x, int y) 
    {
        _x = x;
        _y = y;
    }

    public void Destroy(bool immediately) 
    {
        _isDestroyed = true;
        OnDestroyed?.Invoke(this, EventArgs.Empty, immediately);
    }

    public override string ToString() => _isDestroyed.ToString();
}
