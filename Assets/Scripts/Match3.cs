using System;
using System.Collections.Generic;
using UnityEngine;

public class OnLevelSetEventArgs: EventArgs
{
    public Grid<CubeGridPosition> grid;
}

public class Match3: MonoBehaviour
{
    public delegate void BoolEventHandler(object sender, EventArgs e, bool value);
    public delegate void CubesHandler();
    
    public event BoolEventHandler OnCubeGridPositionDestroyed;
    public event CubesHandler OnCubeDestroyed; 
    public event EventHandler<OnLevelSetEventArgs> OnLevelSet;
    
    [HideInInspector] public GridPositions<CubeGridPosition> gridPositions;
    [HideInInspector] public bool hasSaveFile;
    
    private Level _level;
    private Grid<CubeGridPosition> _grid;
    
    private int _gridWidth;
    private int _gridHeight;
    private int _currentActiveCubes;

    public int CurrentActiveCubes => _currentActiveCubes;

    public void LoadLevel()
    {
        SetLevel(_level);
    }
    
    public void SetLevel(Level level)
    {
        hasSaveFile = SaveManager.HasSaveFile(SaveManager.GridData);
        _currentActiveCubes = 0;
        
        _level = level;
        _gridWidth = level.width;
        _gridHeight = level.height;
        
        _grid = new Grid<CubeGridPosition>(_gridWidth, _gridHeight, 1f, Vector3.zero, (g, x, y) => new CubeGridPosition(g, x, y));
        gridPositions = new GridPositions<CubeGridPosition>
        {
            positionsList = new List<CubeGridPosition>(_gridWidth * _gridHeight)
        };

        if (hasSaveFile)
        {
            gridPositions = SaveManager.GetDataJson<GridPositions<CubeGridPosition>>(SaveManager.GridData);

            foreach (var cubeGridPosition in gridPositions.positionsList)
            {
                if (cubeGridPosition.GetCubeGrid() != null && cubeGridPosition.GetCubeGrid().GetCube() != null)
                    _currentActiveCubes++;
            }
        }
        else
        {
            for (var x = 0; x < _gridWidth; x++) 
            {
                for (var y = 0; y < _gridHeight; y++) 
                {
                    LevelGridPosition levelGridPosition = null;
                
                    foreach (var tmpLevelGridPosition in level.levelGridPositionList) 
                    {
                        if (tmpLevelGridPosition.x == x && tmpLevelGridPosition.y == y) 
                        {
                            levelGridPosition = tmpLevelGridPosition;
                            break;
                        }
                    }

                    if (levelGridPosition == null) 
                    {
                        Debug.LogError("Couldn't find LevelGridPosition with this x, y!");
                    }
                    
                    var cube = levelGridPosition?.cube;
                    var cubeGrid = new CubeGrid(cube, x, y);
                    var cubeGridPosition = _grid.GetGridObject(x, y);
                    cubeGridPosition.SetCubeGrid(cubeGrid);
                    
                    if(levelGridPosition?.cube == null)
                        TryDestroyCubeGridPosition(cubeGridPosition, true);
                    else
                        _currentActiveCubes++;
                    
                    gridPositions.positionsList.Add(cubeGridPosition);
                }
            }
        }
        
        SaveManager.SetDataJson(SaveManager.GridData, gridPositions, true);
        
        OnLevelSet?.Invoke(this, new OnLevelSetEventArgs
        {
            grid = _grid
        });
    }

    public void ClearLevel(bool clearSave)
    {
        if(clearSave)
            SaveManager.DeleteSaveFile(SaveManager.GridData);
        
        for (int x = 0; x < _gridWidth; x++)
        {
            for (int y = 0; y < _gridHeight; y++)
            {
                var cubeGridPosition = _grid.GetGridObject(x, y);
                TryDestroyCubeGridPosition(cubeGridPosition, true);
            }
        }
    }
    
    public bool CanSwapGridPositions(int startX, int startY, int endX, int endY) 
    {
        if (!IsValidPosition(startX, startY) || !IsValidPosition(endX, endY))
        {
            return false;
        }
        
        var gridObjStart = _grid.GetGridObject(startX, startY);
        var gridObjEnd = _grid.GetGridObject(endX, endY);
        
        var cubeStart = gridObjStart?.GetCubeGrid()?.GetCube();
        var cubeEnd = gridObjEnd?.GetCubeGrid()?.GetCube();
        
        if (cubeStart != null && cubeEnd != null && cubeStart!.Equals(cubeEnd))
        {
            return false;
        }
        if (cubeEnd == null && startY < endY) return false;
        
        return startX != endX || startY != endY;
    }

    public void SwapGridPositions(int startX, int startY, int endX, int endY) 
    {
        if (!IsValidPosition(startX, startY)  || !IsValidPosition(endX, endY)) return; // Invalid Position

        if (startX == endX && startY == endY) return; // Same Position

        var startCubeGridPosition = _grid.GetGridObject(startX, startY);
        var endCubeGridPosition = _grid.GetGridObject(endX, endY);

        var startCubeGrid = startCubeGridPosition.GetCubeGrid();
        var endCubeGrid = endCubeGridPosition.GetCubeGrid();
        
        startCubeGrid?.SetCubeXY(endX, endY);
        endCubeGrid?.SetCubeXY(startX, startY);

        startCubeGridPosition.SetCubeGrid(endCubeGrid);
        endCubeGridPosition.SetCubeGrid(startCubeGrid);
    }

    public bool TryFindMatchesAndDestroyThem() 
    {
        var allLinkedCubeGridPositionList = GetAllMatch3Links();

        var foundMatch = false;
        
        foreach (var linkedCubeGridPositionList in allLinkedCubeGridPositionList) 
        {
            foreach (var cubeGridPosition in linkedCubeGridPositionList) 
            {
                TryDestroyCubeGridPosition(cubeGridPosition, false);
            }
            foundMatch = true;
        }
        
        return foundMatch;
    }

    private void TryDestroyCubeGridPosition(CubeGridPosition cubeGridPosition, bool immediately) 
    {
        if (cubeGridPosition.HasCubeGrid()) 
        {
            cubeGridPosition.DestroyCube(immediately);
            OnCubeGridPositionDestroyed?.Invoke(cubeGridPosition, EventArgs.Empty, true);
            cubeGridPosition.ClearCubeGrid();

            if (immediately) return;
            _currentActiveCubes--;
        }
    }

    public void FallCubesIntoEmptyPositions() 
    {
        for (var x = 0; x < _gridWidth; x++) 
        {
            for (var y = 0; y < _gridHeight; y++) 
            {
                var cubeGridPosition = _grid.GetGridObject(x, y);

                if (!cubeGridPosition.IsEmpty()) 
                {
                    // Grid Position has Cube
                    for (var i = y - 1; i >= 0; i--) 
                    {
                        var nextCubeGridPosition = _grid.GetGridObject(x, i);
                        if (nextCubeGridPosition.IsEmpty()) 
                        {
                            cubeGridPosition.GetCubeGrid().SetCubeXY(x, i);
                            nextCubeGridPosition.SetCubeGrid(cubeGridPosition.GetCubeGrid());
                            cubeGridPosition.ClearCubeGrid();

                            cubeGridPosition = nextCubeGridPosition;
                        } 
                        else break;
                    }
                }
            }
        }
    }
    
    public void InvokeCubeDestroyEvent()
    {
        OnCubeDestroyed?.Invoke();
    }

    private bool HasMatch3Link(int x, int y) 
    {
        var linkedCubeGridPositionList = GetMatch3Links(x, y);
        return linkedCubeGridPositionList is { Count: >= 3 };
    }

    private List<CubeGridPosition> GetMatch3Links(int x, int y)
    {
        var cube = GetCube(x, y);
        if (cube == null) return null;

        var rightLinkAmount = 0;
        
        for (var i = 1; i < _gridWidth; i++)
        {
            if (IsValidPosition(x + i, y))
            {
                var nextCube = GetCube(x + i, y);
                if (nextCube == cube)
                {
                    // Same Cube
                    rightLinkAmount++;
                }
                else break;
            }
            else break;
        }

        var leftLinkAmount = 0;
        
        for (var i = 1; i < _gridWidth; i++)
        {
            if (IsValidPosition(x - i, y))
            {
                var nextCube = GetCube(x - i, y);
                if (nextCube == cube)
                {
                    // Same Cube
                    leftLinkAmount++;
                }
                else break;
            }
            else break;
        }

        var horizontalLinkAmount = 1 + leftLinkAmount + rightLinkAmount; // This Cube + left + right

        if (horizontalLinkAmount >= 3) 
        {
            // Has 3 horizontal linked gems
            var linkedCubeGridPositionList = new List<CubeGridPosition>();
            var leftMostX = x - leftLinkAmount;
            
            for (var i = 0; i < horizontalLinkAmount; i++) 
            {
                linkedCubeGridPositionList.Add(_grid.GetGridObject(leftMostX + i, y));
            }
            return linkedCubeGridPositionList;
        }
        
        var upLinkAmount = 0;
        
        for (var i = 1; i < _gridHeight; i++)
        {
            if (IsValidPosition(x, y + i))
            {
                var nextCube = GetCube(x, y + i);
                if (nextCube == cube)
                {
                    // Same Cube
                    upLinkAmount++;
                }
                else break;
            }
            else break;
        }

        var downLinkAmount = 0;
        
        for (var i = 1; i < _gridHeight; i++)
        {
            if (IsValidPosition(x, y - i))
            {
                var nextCube = GetCube(x, y - i);
                if (nextCube == cube)
                {
                    // Same Cube
                    downLinkAmount++;
                }
                else break;
            }
            else break;
        }

        var verticalLinkAmount = 1 + downLinkAmount + upLinkAmount; // This Cube + down + up

        if (verticalLinkAmount >= 3) 
        {
            // Has 3 vertical linked gems
            var linkedCubeGridPositionList = new List<CubeGridPosition>();
            var downMostY = y - downLinkAmount;
            
            for (var i = 0; i < verticalLinkAmount; i++) 
            {
                linkedCubeGridPositionList.Add(_grid.GetGridObject(x, downMostY + i));
            }
            return linkedCubeGridPositionList;
        }
        // No links
        return null;
    }

    private List<List<CubeGridPosition>> GetAllMatch3Links() 
    {
        // Finds all the links with the current _grid
        var allLinkedCubeGridPositionList = new List<List<CubeGridPosition>>();

        for (var x = 0; x < _gridWidth; x++) 
        {
            for (var y = 0; y < _gridHeight; y++) 
            {
                if (HasMatch3Link(x, y)) 
                {
                    var linkedCubeGridPositionList = GetMatch3Links(x, y);

                    if (allLinkedCubeGridPositionList.Count == 0) 
                    {
                        // First one
                        allLinkedCubeGridPositionList.Add(linkedCubeGridPositionList);
                    } else 
                    {
                        var uniqueNewLink = true;

                        foreach (var tmpLinkedCubeGridPositionList in allLinkedCubeGridPositionList) 
                        {
                            if (linkedCubeGridPositionList.Count == tmpLinkedCubeGridPositionList.Count) 
                            {
                                // Same number of links
                                var allTheSame = true;
                                
                                for (var i = 0; i < linkedCubeGridPositionList.Count; i++) 
                                {
                                    if (linkedCubeGridPositionList[i] != tmpLinkedCubeGridPositionList[i]) 
                                    {
                                        allTheSame = false;
                                        break;
                                    } 
                                }
                                if (allTheSame)
                                {
                                    // Nodes are all the same, not a new unique link
                                    uniqueNewLink = false;
                                }
                            }
                        }
                        // Add to the total list if it's a unique link
                        if (uniqueNewLink) 
                            allLinkedCubeGridPositionList.Add(linkedCubeGridPositionList);
                    }
                }
            }
        }
        return allLinkedCubeGridPositionList;
    }

    private Cube GetCube(int x, int y) 
    {
        if (!IsValidPosition(x, y)) return null;

        var cubeGridPosition = _grid.GetGridObject(x, y);
        return cubeGridPosition.GetCubeGrid() == null ? null : cubeGridPosition.GetCubeGrid().GetCube();
    }

    private bool IsValidPosition(int x, int y)
    {
        return x >= 0 && y >= 0 && x < _gridWidth && y < _gridHeight;
    }
}
