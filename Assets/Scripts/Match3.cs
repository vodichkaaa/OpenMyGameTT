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

    public class OnLevelSetEventArgs : EventArgs 
    {
        public Level level;
        public Grid<CubeGridPosition> grid;
    }

    [SerializeField] private Level level;
    [SerializeField] private bool autoLoadLevel;

    private int gridWidth;
    private int gridHeight;
    private Grid<CubeGridPosition> grid;

    private void Start() 
    {
        if (autoLoadLevel) 
        {
            SetLevel(level);
        }
    }
    

    public void SetLevel(Level level) 
    {
        this.level = level;

        gridWidth = level.width;
        gridHeight = level.height;
        grid = new Grid<CubeGridPosition>(gridWidth, gridHeight, 1f, Vector3.zero, (g, x, y) => new CubeGridPosition(g, x, y));

        // Initialize Grid
        for (int x = 0; x < gridWidth; x++) {
            for (int y = 0; y < gridHeight; y++) {

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

                Cube cube = levelGridPosition.cube;
                CubeGrid cubeGrid = new CubeGrid(cube, x, y);
                grid.GetGridObject(x, y).SetCubeGrid(cubeGrid);
            }
        }
        
        OnLevelSet?.Invoke(this, new OnLevelSetEventArgs { level = level, grid = grid });
    }
    
    /*public bool CanSwapGridPositions(int startX, int startY, int endX, int endY) 
    {
        if (!IsValidPosition(startX, startY) || !IsValidPosition(endX, endY)) return false; // Invalid Position

        if (startX == endX && startY == endY) return false; // Same Position

        SwapGridPositions(startX, startY, endX, endY); // Swap

        bool hasLinkAfterSwap = HasMatch3Link(startX, startY) || HasMatch3Link(endX, endY);

        SwapGridPositions(startX, startY, endX, endY); // Swap Back

        return hasLinkAfterSwap;
    }*/

    public void SwapGridPositions(int startX, int startY, int endX, int endY) 
    {
        if (!IsValidPosition(startX, startY) || !IsValidPosition(endX, endY)) return; // Invalid Position

        if (startX == endX && startY == endY) return; // Same Position

        CubeGridPosition startCubeGridPosition = grid.GetGridObject(startX, startY);
        CubeGridPosition endCubeGridPosition = grid.GetGridObject(endX, endY);

        CubeGrid startcubeGrid = startCubeGridPosition.GetCubeGrid();
        CubeGrid endcubeGrid = endCubeGridPosition.GetCubeGrid();

        startcubeGrid.SetCubeXY(endX, endY);
        endcubeGrid.SetCubeXY(startX, startY);

        startCubeGridPosition.SetCubeGrid(endcubeGrid);
        endCubeGridPosition.SetCubeGrid(startcubeGrid);
    }

    public bool TryFindMatchesAndDestroyThem() {
        List<List<CubeGridPosition>> allLinkedCubeGridPositionList = GetAllMatch3Links();

        bool foundMatch = false;

        List<Vector2Int> explosionGridPositionList = new List<Vector2Int>();

        foreach (List<CubeGridPosition> linkedCubeGridPositionList in allLinkedCubeGridPositionList) 
        {
            foreach (CubeGridPosition cubeGridPosition in linkedCubeGridPositionList) 
            {
                TryDestroyCubeGridPosition(cubeGridPosition);
            }

            if (linkedCubeGridPositionList.Count >= 4) 
            {
                // More than 4 linked

                // Special Explosion Cube
                CubeGridPosition explosionOriginCubeGridPosition = linkedCubeGridPositionList[0];

                int explosionX = explosionOriginCubeGridPosition.GetX();
                int explosionY = explosionOriginCubeGridPosition.GetY();

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

    private void TryDestroyCubeGridPosition(CubeGridPosition cubeGridPosition) {
        if (cubeGridPosition.HasCubeGrid()) {

            cubeGridPosition.DestroyCube();
            OnCubeGridPositionDestroyed?.Invoke(cubeGridPosition, EventArgs.Empty);
            cubeGridPosition.ClearCubeGrid();
        }
    }

    public void FallCubesIntoEmptyPositions() {
        for (int x = 0; x < gridWidth; x++) {
            for (int y = 0; y < gridHeight; y++) {
                CubeGridPosition cubeGridPosition = grid.GetGridObject(x, y);

                if (!cubeGridPosition.IsEmpty()) {
                    // Grid Position has Cube
                    for (int i = y - 1; i >= 0; i--) {
                        CubeGridPosition nextCubeGridPosition = grid.GetGridObject(x, i);
                        if (nextCubeGridPosition.IsEmpty()) {
                            cubeGridPosition.GetCubeGrid().SetCubeXY(x, i);
                            nextCubeGridPosition.SetCubeGrid(cubeGridPosition.GetCubeGrid());
                            cubeGridPosition.ClearCubeGrid();

                            cubeGridPosition = nextCubeGridPosition;
                        } else {
                            // Next Grid Position is not empty, stop looking
                            break;
                        }
                    }
                }
            }
        }
    }

    public bool HasMatch3Link(int x, int y) {
        List<CubeGridPosition> linkedCubeGridPositionList = GetMatch3Links(x, y);
        return linkedCubeGridPositionList != null && linkedCubeGridPositionList.Count >= 3;
    }

    public List<CubeGridPosition> GetMatch3Links(int x, int y) {
        Cube cube = GetCubeSO(x, y);

        if (cube == null) return null;

        int rightLinkAmount = 0;
        for (int i = 1; i < gridWidth; i++) {
            if (IsValidPosition(x + i, y)) {
                Cube nextCube = GetCubeSO(x + i, y);
                if (nextCube == cube) {
                    // Same Cube
                    rightLinkAmount++;
                } else {
                    // Not same Cube
                    break;
                }
            } else {
                // Invalid position
                break;
            }
        }

        int leftLinkAmount = 0;
        for (int i = 1; i < gridWidth; i++) {
            if (IsValidPosition(x - i, y)) {
                Cube nextCube = GetCubeSO(x - i, y);
                if (nextCube == cube) {
                    // Same Cube
                    leftLinkAmount++;
                } else {
                    // Not same Cube
                    break;
                }
            } else {
                // Invalid position
                break;
            }
        }

        int horizontalLinkAmount = 1 + leftLinkAmount + rightLinkAmount; // This Cube + left + right

        if (horizontalLinkAmount >= 3) {
            // Has 3 horizontal linked gems
            List<CubeGridPosition> linkedCubeGridPositionList = new List<CubeGridPosition>();
            int leftMostX = x - leftLinkAmount;
            for (int i = 0; i < horizontalLinkAmount; i++) {
                linkedCubeGridPositionList.Add(grid.GetGridObject(leftMostX + i, y));
            }
            return linkedCubeGridPositionList;
        }


        int upLinkAmount = 0;
        for (int i = 1; i < gridHeight; i++) {
            if (IsValidPosition(x, y + i)) {
                Cube nextCube = GetCubeSO(x, y + i);
                if (nextCube == cube) {
                    // Same Cube
                    upLinkAmount++;
                } else {
                    // Not same Cube
                    break;
                }
            } else {
                // Invalid position
                break;
            }
        }

        int downLinkAmount = 0;
        for (int i = 1; i < gridHeight; i++) {
            if (IsValidPosition(x, y - i)) {
                Cube nextCube = GetCubeSO(x, y - i);
                if (nextCube == cube) {
                    // Same Cube
                    downLinkAmount++;
                } else {
                    // Not same Cube
                    break;
                }
            } else {
                // Invalid position
                break;
            }
        }

        int verticalLinkAmount = 1 + downLinkAmount + upLinkAmount; // This Cube + down + up

        if (verticalLinkAmount >= 3) {
            // Has 3 vertical linked gems
            List<CubeGridPosition> linkedCubeGridPositionList = new List<CubeGridPosition>();
            int downMostY = y - downLinkAmount;
            for (int i = 0; i < verticalLinkAmount; i++) {
                linkedCubeGridPositionList.Add(grid.GetGridObject(x, downMostY + i));
            }
            return linkedCubeGridPositionList;
        }

        // No links
        return null;
    }

    public List<List<CubeGridPosition>> GetAllMatch3Links() {
        // Finds all the links with the current grid
        List<List<CubeGridPosition>> allLinkedCubeGridPositionList = new List<List<CubeGridPosition>>();

        for (int x = 0; x < gridWidth; x++) {
            for (int y = 0; y < gridHeight; y++) {
                if (HasMatch3Link(x, y)) {
                    List<CubeGridPosition> linkedCubeGridPositionList = GetMatch3Links(x, y);

                    if (allLinkedCubeGridPositionList.Count == 0) {
                        // First one
                        allLinkedCubeGridPositionList.Add(linkedCubeGridPositionList);
                    } else {
                        bool uniqueNewLink = true;

                        foreach (List<CubeGridPosition> tmpLinkedCubeGridPositionList in allLinkedCubeGridPositionList) {
                            if (linkedCubeGridPositionList.Count == tmpLinkedCubeGridPositionList.Count) {
                                // Same number of links
                                // Are they all the same?
                                bool allTheSame = true;
                                for (int i = 0; i < linkedCubeGridPositionList.Count; i++) {
                                    if (linkedCubeGridPositionList[i] == tmpLinkedCubeGridPositionList[i]) {
                                        // This one is the same, link is not unique
                                    } else {
                                        // These don't match
                                        allTheSame = false;
                                        break;
                                    }
                                }

                                if (allTheSame) {
                                    // Nodes are all the same, not a new unique link
                                    uniqueNewLink = false;
                                }
                            }
                        }

                        // Add to the total list if it's a unique link
                        if (uniqueNewLink) {
                            allLinkedCubeGridPositionList.Add(linkedCubeGridPositionList);
                        }
                    }
                }
            }
        }

        return allLinkedCubeGridPositionList;
    }

    public List<PossibleMove> GetAllPossibleMoves() {
        List<PossibleMove> allPossibleMovesList = new List<PossibleMove>();

        // Test the Horizontal Axis first, prioritize nodes lower on the grid
        for (int y = 0; y < gridHeight; y++) {
            for (int x = 0; x < gridWidth; x++) {
                // Test Swap: Left, Right, Up, Down
                List<PossibleMove> testPossibleMoveList = new List<PossibleMove>();
                testPossibleMoveList.Add(new PossibleMove(x, y, x - 1, y + 0));
                testPossibleMoveList.Add(new PossibleMove(x, y, x + 1, y + 0));
                testPossibleMoveList.Add(new PossibleMove(x, y, x + 0, y + 1));
                testPossibleMoveList.Add(new PossibleMove(x, y, x + 0, y - 1));

                for (int i=0; i<testPossibleMoveList.Count; i++) {
                    PossibleMove possibleMove = testPossibleMoveList[i];

                    bool skipPossibleMove = false;

                    for (int j = 0; j < allPossibleMovesList.Count; j++) {
                        PossibleMove tmpPossibleMove = allPossibleMovesList[j];
                        if (tmpPossibleMove.startX == possibleMove.startX &&
                            tmpPossibleMove.startY == possibleMove.startY &&
                            tmpPossibleMove.endX == possibleMove.endX &&
                            tmpPossibleMove.endY == possibleMove.endY) {
                            // Already tested this combo
                            skipPossibleMove = true;
                            break;
                        }
                        if (tmpPossibleMove.startX == possibleMove.endX &&
                            tmpPossibleMove.startY == possibleMove.endY &&
                            tmpPossibleMove.endX == possibleMove.startX &&
                            tmpPossibleMove.endY == possibleMove.startY) {
                            // Already tested this combo
                            skipPossibleMove = true;
                            break;
                        }
                    }

                    if (skipPossibleMove) {
                        continue;
                    }

                    SwapGridPositions(possibleMove.startX, possibleMove.startY, possibleMove.endX, possibleMove.endY); // Swap

                    List<List<CubeGridPosition>> allLinkedCubeGridPositionList = GetAllMatch3Links();

                    if (allLinkedCubeGridPositionList.Count > 0) {
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

    private Cube GetCubeSO(int x, int y) {
        if (!IsValidPosition(x, y)) return null;

        CubeGridPosition cubeGridPosition = grid.GetGridObject(x, y);

        if (cubeGridPosition.GetCubeGrid() == null) return null;

        return cubeGridPosition.GetCubeGrid().GetCube();
    }

    private bool IsValidPosition(int x, int y) {
        if (x < 0 || y < 0 ||
            x >= gridWidth || y >= gridHeight) {
            // Invalid position
            return false;
        } else {
            return true;
        }
    }
    
    /*
     * Possible Move and what matches would happen if this move was made
     * */
    public class PossibleMove {

        public int startX;
        public int startY;
        public int endX;
        public int endY;
        public List<List<CubeGridPosition>> allLinkedCubeGridPositionList;

        public PossibleMove() { }

        public PossibleMove(int startX, int startY, int endX, int endY) {
            this.startX = startX;
            this.startY = startY;
            this.endX = endX;
            this.endY = endY;
        }
        
        public override string ToString() {
            return startX + ", " + startY + " => " + endX + ", " + endY + " == " + allLinkedCubeGridPositionList?.Count;
        }

    }
    
    /*
     * Represents a single Grid Position
     * Only the Grid Position which may or may not have an actual Cube on it
     * */
    public class CubeGridPosition {
        
        private CubeGrid cubeGrid;

        private Grid<CubeGridPosition> grid;
        private int x;
        private int y;

        public CubeGridPosition(Grid<CubeGridPosition> grid, int x, int y) {
            this.grid = grid;
            this.x = x;
            this.y = y;
        }

        public void SetCubeGrid(CubeGrid cubeGrid) {
            this.cubeGrid = cubeGrid;
            grid.TriggerGridObjectChanged(x, y);
        }

        public int GetX() {
            return x;
        }

        public int GetY() {
            return y;
        }

        public Vector3 GetWorldPosition() {
            return grid.GetWorldPosition(x, y);
        }

        public CubeGrid GetCubeGrid() {
            return cubeGrid;
        }

        public void ClearCubeGrid() {
            cubeGrid = null;
        }

        public void DestroyCube() {
            cubeGrid?.Destroy();
            grid.TriggerGridObjectChanged(x, y);
        }

        public bool HasCubeGrid() {
            return cubeGrid != null;
        }

        public bool IsEmpty() {
            return cubeGrid == null;
        }

        public override string ToString() {
            return cubeGrid?.ToString();
        }
    }

    /*
     * Represents a Cube Object in the Grid
     * */
    public class CubeGrid {

        public event EventHandler OnDestroyed;

        private Cube cube;
        private int x;
        private int y;
        private bool isDestroyed;

        public CubeGrid(Cube cube, int x, int y) {
            this.cube = cube;
            this.x = x;
            this.y = y;

            isDestroyed = false;
        }

        public Cube GetCube() {
            return cube;
        }

        public Vector3 GetWorldPosition() {
            return new Vector3(x, y);
        }

        public void SetCubeXY(int x, int y) {
            this.x = x;
            this.y = y;
        }

        public void Destroy() {
            isDestroyed = true;
            OnDestroyed?.Invoke(this, EventArgs.Empty);
        }

        public override string ToString() {
            return isDestroyed.ToString();
        }

    }

}
