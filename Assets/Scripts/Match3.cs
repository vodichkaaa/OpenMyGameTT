using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Represents the underlying Grid logic
 * */
public class Match3 : MonoBehaviour {

    public event EventHandler OnCubeGridPositionDestroyed;
    public event EventHandler<OnLevelSetEventArgs> OnLevelSet;

    public class OnLevelSetEventArgs: EventArgs 
    {
        public Level level;
        public Grid<CubeGridPosition> grid;
    }

    [SerializeField] private Level level;
    [SerializeField] private bool autoLoadLevel;

    private int _gridWidth;
    private int _gridHeight;
    private Grid<CubeGridPosition> _grid;

    private void Start() 
    {
        if (autoLoadLevel) 
        {
            SetLevel(level);
        }
    }
    
    private void SetLevel(Level level) 
    {
        this.level = level;

        _gridWidth = level.width;
        _gridHeight = level.height;
        _grid = new Grid<CubeGridPosition>(_gridWidth, _gridHeight, 1f, Vector3.zero, (g, x, y) => new CubeGridPosition(g, x, y));

        // Initialize Grid
        for (int x = 0; x < _gridWidth; x++) {
            for (int y = 0; y < _gridHeight; y++) {

                // Get Saved LevelGridPosition
                Level.LevelGridPosition levelGridPosition = null;

                foreach (Level.LevelGridPosition tmpLevelGridPosition in level.levelGridPositionList) 
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

                var cube = levelGridPosition.cube;
                var cubeGrid = new CubeGrid(cube, x, y);
                var cubeGridPosition = _grid.GetGridObject(x, y);
                cubeGridPosition.SetCubeGrid(cubeGrid);
                
                if(levelGridPosition.cube == null)
                    TryDestroyCubeGridPosition(cubeGridPosition);
            }
        }
        
        OnLevelSet?.Invoke(this, new OnLevelSetEventArgs { level = level, grid = _grid });
    }
    
    public bool CanSwapGridPositions(int startX, int startY, int endX, int endY) 
    {
        if (!IsValidPosition(startX, startY) || !IsValidPosition(endX, endY))
        {
            Debug.Log("Invalid Position");
            return false;
        }
        
        /*if (_grid.GetGridObject(startX, startY) == default || _grid.GetGridObject(endX, endY) == default)
        {
            Debug.Log("Invalid GridObject");
            return false;
        }*/

        var gridObjStart = _grid.GetGridObject(startX, startY);
        var gridObjEnd = _grid.GetGridObject(endX, endY);
        
        var cubeStart = gridObjStart?.GetCubeGrid()?.GetCube();
        var cubeEnd = gridObjEnd?.GetCubeGrid()?.GetCube();
        
        if (cubeStart != null && cubeEnd != null && cubeStart!.Equals(cubeEnd))
        {
            Debug.Log("Same object");
            return false;
        }

        if (cubeEnd == null && startY < endY) return false; //Swapping with free space over cube
        
        //SwapGridPositions(startX, startY, endX, endY); // Swap
        //bool hasLinkAfterSwap = HasMatch3Link(startX, startY) || HasMatch3Link(endX, endY);
        //SwapGridPositions(startX, startY, endX, endY); // Swap Back
        
        return startX != endX || startY != endY;
    }

    public void SwapGridPositions(int startX, int startY, int endX, int endY) 
    {
        if (!IsValidPosition(startX, startY)  || !IsValidPosition(endX, endY)) return; // Invalid Position

        if (startX == endX && startY == endY) return; // Same Position

        CubeGridPosition startCubeGridPosition = _grid.GetGridObject(startX, startY);
        CubeGridPosition endCubeGridPosition = _grid.GetGridObject(endX, endY);

        CubeGrid startCubeGrid = startCubeGridPosition.GetCubeGrid();
        CubeGrid endCubeGrid = endCubeGridPosition.GetCubeGrid();
        
        startCubeGrid?.SetCubeXY(endX, endY);
        endCubeGrid?.SetCubeXY(startX, startY);

        startCubeGridPosition.SetCubeGrid(endCubeGrid);
        endCubeGridPosition.SetCubeGrid(startCubeGrid);
    }

    public bool TryFindMatchesAndDestroyThem() 
    {
        List<List<CubeGridPosition>> allLinkedCubeGridPositionList = GetAllMatch3Links();

        var foundMatch = false;

        List<Vector2Int> explosionGridPositionList = new List<Vector2Int>();
        
        foreach (List<CubeGridPosition> linkedCubeGridPositionList in allLinkedCubeGridPositionList) 
        {
            foreach (var cubeGridPosition in linkedCubeGridPositionList) 
            {
                TryDestroyCubeGridPosition(cubeGridPosition);
            }

            if (linkedCubeGridPositionList.Count >= 4) 
            {
                // More than 4 linked

                // Special Explosion Cube
                var explosionOriginCubeGridPosition = linkedCubeGridPositionList[0];

                var explosionX = explosionOriginCubeGridPosition.GetX();
                var explosionY = explosionOriginCubeGridPosition.GetY();

                // Explode all 8 neighbours
                explosionGridPositionList.Add(new Vector2Int(explosionX - 1, explosionY - 1));
                explosionGridPositionList.Add(new Vector2Int(explosionX + 0, explosionY - 1));
                explosionGridPositionList.Add(new Vector2Int(explosionX + 1, explosionY - 1));
                explosionGridPositionList.Add(new Vector2Int(explosionX - 1, explosionY + 0));
                explosionGridPositionList.Add(new Vector2Int(explosionX + 1, explosionY + 0));
                explosionGridPositionList.Add(new Vector2Int(explosionX - 1, explosionY + 1));
                explosionGridPositionList.Add(new Vector2Int(explosionX + 0, explosionY + 1));
                explosionGridPositionList.Add(new Vector2Int(explosionX + 1, explosionY + 1));
            }

            foundMatch = true;
        }
        
        return foundMatch;
    }

    private void TryDestroyCubeGridPosition(CubeGridPosition cubeGridPosition) 
    {
        if (cubeGridPosition.HasCubeGrid()) 
        {
            cubeGridPosition.DestroyCube();
            OnCubeGridPositionDestroyed?.Invoke(cubeGridPosition, EventArgs.Empty);
            cubeGridPosition.ClearCubeGrid();
        }
    }

    public void FallCubesIntoEmptyPositions() 
    {
        for (var x = 0; x < _gridWidth; x++) 
        {
            for (var y = 0; y < _gridHeight; y++) 
            {
                CubeGridPosition cubeGridPosition = _grid.GetGridObject(x, y);

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

    private bool HasMatch3Link(int x, int y) 
    {
        List<CubeGridPosition> linkedCubeGridPositionList = GetMatch3Links(x, y);
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
            List<CubeGridPosition> linkedCubeGridPositionList = new List<CubeGridPosition>();
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
                Cube nextCube = GetCube(x, y + i);
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
            List<CubeGridPosition> linkedCubeGridPositionList = new List<CubeGridPosition>();
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
        List<List<CubeGridPosition>> allLinkedCubeGridPositionList = new List<List<CubeGridPosition>>();

        for (var x = 0; x < _gridWidth; x++) 
        {
            for (var y = 0; y < _gridHeight; y++) 
            {
                if (HasMatch3Link(x, y)) 
                {
                    List<CubeGridPosition> linkedCubeGridPositionList = GetMatch3Links(x, y);

                    if (allLinkedCubeGridPositionList.Count == 0) 
                    {
                        // First one
                        allLinkedCubeGridPositionList.Add(linkedCubeGridPositionList);
                    } else 
                    {
                        var uniqueNewLink = true;

                        foreach (List<CubeGridPosition> tmpLinkedCubeGridPositionList in allLinkedCubeGridPositionList) 
                        {
                            if (linkedCubeGridPositionList.Count == tmpLinkedCubeGridPositionList.Count) 
                            {
                                // Same number of links
                                // Are they all the same?
                                var allTheSame = true;
                                
                                for (var i = 0; i < linkedCubeGridPositionList.Count; i++) 
                                {
                                    if (linkedCubeGridPositionList[i] == tmpLinkedCubeGridPositionList[i]) 
                                    {
                                        // This one is the same, link is not unique
                                    } 
                                    else 
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

    public List<PossibleMove> GetAllPossibleMoves() 
    {
        List<PossibleMove> allPossibleMovesList = new List<PossibleMove>();

        // Test the Horizontal Axis first, prioritize nodes lower on the _grid
        for (var y = 0; y < _gridHeight; y++) 
        {
            for (var x = 0; x < _gridWidth; x++) 
            {
                // Test Swap: Left, Right, Up, Down
                List<PossibleMove> testPossibleMoveList = new List<PossibleMove>
                {
                    new(x, y, x - 1, y + 0),
                    new(x, y, x + 1, y + 0),
                    new(x, y, x + 0, y + 1),
                    new(x, y, x + 0, y - 1)
                };

                for (var i=0; i<testPossibleMoveList.Count; i++) 
                {
                    PossibleMove possibleMove = testPossibleMoveList[i];

                    var skipPossibleMove = false;

                    for (var j = 0; j < allPossibleMovesList.Count; j++) 
                    {
                        var tmpPossibleMove = allPossibleMovesList[j];
                        if (tmpPossibleMove.startX == possibleMove.startX &&
                            tmpPossibleMove.startY == possibleMove.startY &&
                            tmpPossibleMove.endX == possibleMove.endX &&
                            tmpPossibleMove.endY == possibleMove.endY) 
                        {
                            // Already tested this combo
                            skipPossibleMove = true;
                            break;
                        }
                        if (tmpPossibleMove.startX == possibleMove.endX &&
                            tmpPossibleMove.startY == possibleMove.endY &&
                            tmpPossibleMove.endX == possibleMove.startX &&
                            tmpPossibleMove.endY == possibleMove.startY) 
                        {
                            // Already tested this combo
                            skipPossibleMove = true;
                            break;
                        }
                    }

                    if (skipPossibleMove) continue;

                    SwapGridPositions(possibleMove.startX, possibleMove.startY, possibleMove.endX, possibleMove.endY); // Swap

                    List<List<CubeGridPosition>> allLinkedCubeGridPositionList = GetAllMatch3Links();

                    if (allLinkedCubeGridPositionList.Count > 0) 
                    {
                        // Making this Move results in a Match
                        possibleMove.allLinkedCubeGridPositionList = allLinkedCubeGridPositionList;
                        allPossibleMovesList.Add(possibleMove);
                    }

                    SwapGridPositions(possibleMove.startX, possibleMove.startY, possibleMove.endX, possibleMove.endY); // Swap Back
                }

            }
        }

        return allPossibleMovesList;
    }

    private Cube GetCube(int x, int y) 
    {
        if (!IsValidPosition(x, y)) return null;

        CubeGridPosition cubeGridPosition = _grid.GetGridObject(x, y);

        return cubeGridPosition.GetCubeGrid() == null ? null : cubeGridPosition.GetCubeGrid().GetCube();
    }

    private bool IsValidPosition(int x, int y)
    {
        return x >= 0 && y >= 0 && x < _gridWidth && y < _gridHeight;
    }
    
    /*
     * Possible Move and what matches would happen if this move was made
     * */
    public class PossibleMove 
    {
        public readonly int startX;
        public readonly int startY;
        public readonly int endX;
        public readonly int endY;
        
        public List<List<CubeGridPosition>> allLinkedCubeGridPositionList;

        public PossibleMove() { }

        public PossibleMove(int startX, int startY, int endX, int endY) 
        {
            this.startX = startX;
            this.startY = startY;
            this.endX = endX;
            this.endY = endY;
        }
        
        public override string ToString() 
        {
            return startX + ", " + startY + " => " + endX + ", " + endY + " == " + allLinkedCubeGridPositionList?.Count;
        }
    }
    
    /*
     * Represents a single Grid Position
     * Only the Grid Position which may or may not have an actual Cube on it
     * */
    public class CubeGridPosition 
    {
        private CubeGrid _cubeGrid;

        private readonly Grid<CubeGridPosition> _grid;
        
        private int _x;
        private int _y;

        public CubeGridPosition(Grid<CubeGridPosition> grid, int x, int y) 
        {
            _grid = grid;
            _x = x;
            _y = y;
        }

        public void SetCubeGrid(CubeGrid cubeGrid) 
        {
            _cubeGrid = cubeGrid;
            _grid.TriggerGridObjectChanged();
        }

        public int GetX() => _x;

        public int GetY() => _y;

        public Vector3 GetWorldPosition() => _grid.GetWorldPosition(_x, _y);

        public CubeGrid GetCubeGrid() => _cubeGrid;

        public void ClearCubeGrid() 
        {
            _cubeGrid = null;
        }

        public void DestroyCube() 
        {
            _cubeGrid?.Destroy();
            _grid.TriggerGridObjectChanged();
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

    /*
     * Represents a Cube Object in the Grid
     * */
    public class CubeGrid 
    {
        public event EventHandler OnDestroyed;

        private Cube _cube;
        private int _x;
        private int _y;
        private bool _isDestroyed;

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

        public void Destroy() 
        {
            _isDestroyed = true;
            OnDestroyed?.Invoke(this, EventArgs.Empty);
        }

        public override string ToString() => _isDestroyed.ToString();
    }

}
