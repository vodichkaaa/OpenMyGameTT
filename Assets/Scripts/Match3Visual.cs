using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

public class Match3Visual : MonoBehaviour 
{
    public enum State 
    {
        Busy,
        WaitingForUser,
        TryFindMatches,
    }
    
    [SerializeField] 
    private Match3 _match3;

    private Grid<CubeGridPosition> _grid;
    private Dictionary<CubeGrid, CubeGridVisual> _cubeGridDictionary = new Dictionary<CubeGrid, CubeGridVisual>();
    
    private State _state;
    private Action _onBusyTimerElapsedAction;
    
    private Camera _mainCamera;
    
    private const int DefaultLayerOrder = 10;
    
    private const float CameraStartPosX = 0.45f;
    private const float CameraPosMultiplier = 0.4f;
    
    private bool _isSetup;
    private bool _isTimerSet;
    
    private int _startDragX;
    private int _startDragY;
    
    private float _busyTimer;
    
    private void Awake() 
    {
        _mainCamera = Camera.main;
        _match3.OnLevelSet += OnLevelSet;
    }
    
    private void OnLevelSet(object sender, OnLevelSetEventArgs e) 
    {
        _state = State.WaitingForUser;
        _isSetup = false;
        
        _busyTimer = 0f;
        _isTimerSet = false;
        
        Setup(sender as Match3, e.grid);
    }

    private void Setup(Match3 match3, Grid<CubeGridPosition> grid)
    {
        _match3 = match3;
        _grid = grid;

        _match3.OnCubeGridPositionDestroyed += OnCubeGridPositionDestroyed;
        _cubeGridDictionary = new Dictionary<CubeGrid, CubeGridVisual>();

        _mainCamera.transform.position = new Vector3(CameraStartPosX + CameraPosMultiplier * _grid.GetWidth(), 
            _mainCamera.transform.position.y, _mainCamera.transform.position.z);

        var t = 0;
        for (var x = 0; x < _grid.GetWidth(); x++) 
        {
            for (var y = 0; y < _grid.GetHeight(); y++) 
            {
                CubeGridPosition cubeGridPosition;
                
                if (this._match3.hasSaveFile)
                {
                    cubeGridPosition = _match3.gridPositions.positionsList[t];
                    
                    if(cubeGridPosition.GetCubeGrid() != null && cubeGridPosition.GetCubeGrid().GetCube() != null)
                    {
                        _grid.SetGridObject(x, y, cubeGridPosition);
                    }
                    t++;
                }
                else
                {
                    cubeGridPosition = _grid.GetGridObject(x, y);
                }
                
                var cubeGrid = cubeGridPosition.GetCubeGrid();
                var position = _grid.GetWorldPosition(x, y);
                
                position = new Vector3(position.x, 12);
                
                // Visual Transform
                if(cubeGrid != null && cubeGrid.GetCube() != null)
                {
                    var cubeGridVisualTransform = Instantiate(cubeGrid.GetCube().prefab.transform, position, Quaternion.identity);
                    
                    var cubeGridVisual = new CubeGridVisual(cubeGridVisualTransform, cubeGrid)
                    {
                        sprite =
                        {
                            sortingOrder = DefaultLayerOrder + _grid.GetGridID(x,y)
                        }
                    };
                    _cubeGridDictionary[cubeGrid] = cubeGridVisual;
                }
            }
        }
        
        SetDelayedState(0.5f, () => { SetState(State.TryFindMatches); });
        
        _isSetup = true;
    }

    private void OnCubeGridPositionDestroyed(object sender, EventArgs e, bool immediately) 
    {
        if (sender is CubeGridPosition cubeGridPosition && cubeGridPosition.GetCubeGrid() != null) 
        {
            _cubeGridDictionary.Remove(cubeGridPosition.GetCubeGrid());
        }
    }

    private void Update() 
    {
        if (!_isSetup) return;

        UpdateVisual();
        UpdateTimer();
        
        switch (_state) 
        {
            case State.Busy:
            case State.WaitingForUser:
            {
                if (Input.GetMouseButtonDown(0))
                {
                    var mouseWorldPosition = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
                    _grid.GetXY(mouseWorldPosition, out _startDragX, out _startDragY);
                }

                if (Input.GetMouseButtonUp(0))
                {
                    var mouseWorldPosition = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
                    _grid.GetXY(mouseWorldPosition, out var x, out var y);

                    if (x != _startDragX)
                    {
                        // Different X
                        y = _startDragY;

                        if (x < _startDragX)
                            x = _startDragX - 1;
                        else
                            x = _startDragX + 1;
                    }
                    else
                    {
                        // Different Y
                        x = _startDragX;

                        if (y < _startDragY)
                            y = _startDragY - 1;
                        else
                            y = _startDragY + 1;
                    }

                    if (_match3.CanSwapGridPositions(_startDragX, _startDragY, x, y))
                    {
                        Debug.Log($"1 - Swap_Match3Visual - {_startDragX}, {_startDragY} - {x}, {y}");
                        SwapGridPositions(_startDragX, _startDragY, x, y);
                    }
                }
                
                break;
            }
            case State.TryFindMatches:
            {
                SetDelayedState(0.01f, () =>
                {
                    if (_cubeGridDictionary.All(obj => obj.Value.isGrounded))
                    {
                        if (_match3.TryFindMatchesAndDestroyThem())
                        {
                            SetDelayedState(0.3f, () =>
                            {
                                _match3.FallCubesIntoEmptyPositions();
                                SetState(State.TryFindMatches);
                            });
                        }
                        else TrySetStateWaitingForUser();
                    }
                    else SetState(State.TryFindMatches);
                });
                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void UpdateTimer()
    {
        if (!_isTimerSet) return;
        
        _busyTimer -= Time.deltaTime;
        if (_busyTimer <= 0f)
        {
            _isTimerSet = false;
            _onBusyTimerElapsedAction();
        }
    }

    private void UpdateVisual() 
    {
        foreach (var cubeGrid in _cubeGridDictionary.Keys) 
        {
            _cubeGridDictionary[cubeGrid].UpdatePosition();
        }
    }
    
    private void SwapOrderIndexes(int startX, int startY, int endX, int endY)
    {
        var startCubeGrid = _grid.GetGridObject(startX, startY).GetCubeGrid();
        var endCubeGrid = _grid.GetGridObject(endX, endY).GetCubeGrid();
        
        Debug.Log($"3 - SwapOrderIndexes_Match3 - {startCubeGrid} - {endCubeGrid}");
        
        if (startCubeGrid != null && _cubeGridDictionary.TryGetValue(startCubeGrid, out var startVisual))
        {
            startVisual.sprite.sortingOrder = DefaultLayerOrder + _grid.GetGridID(startX, startY);
        }
        if (endCubeGrid != null && _cubeGridDictionary.TryGetValue(endCubeGrid, out var endVisual))
        {
            endVisual.sprite.sortingOrder = DefaultLayerOrder + _grid.GetGridID(endX, endY);
        }
    }

    private void SwapGridPositions(int startX, int startY, int endX, int endY) 
    {
        _match3.SwapGridPositions(startX, startY, endX, endY);
        Debug.Log($"2 - SwapOrderIndexes_Match3Visual - {startX}, {startY} - {endX}, {endY}");
        SwapOrderIndexes(startX, startY, endX, endY);
        
        if (_isTimerSet) return;
        SetDelayedState(0.3f, () =>
        {
            _match3.FallCubesIntoEmptyPositions();
            SetState(State.TryFindMatches);
        });
    }

    private void SetDelayedState(float busyTimer, Action onBusyTimerElapsedAction) 
    {
        SetState(State.WaitingForUser);
        _onBusyTimerElapsedAction = onBusyTimerElapsedAction;

        _busyTimer = busyTimer;
        _isTimerSet = true;
    }

    private void TrySetStateWaitingForUser()
    {
        SaveManager.SetDataJson(SaveManager.GridData, _match3.gridPositions, true);
        
        SetState(State.WaitingForUser);

        _match3.InvokeCubeDestroyEvent();
    }

    private void SetState(State state) 
    {
        _state = state;
    }

    public State GetState() 
    {
        return _state;
    }

    [Serializable]
    private class CubeGridVisual
    {
        public SpriteRenderer sprite;
        
        public readonly Transform CubeTransform;
        
        private readonly Cube _cube;
        [SerializeField] private CubeGrid _cubeGrid;
        private readonly Animator _anim;

        public bool isGrounded;
        
        public CubeGridVisual(Transform transform, CubeGrid cubeGrid)
        {
            CubeTransform = transform;
            _cubeGrid = cubeGrid;
            sprite = transform.GetComponentInChildren<SpriteRenderer>();
            
            _cube = _cubeGrid.GetCube();
            _anim = CubeTransform.GetComponentInChildren<Animator>();
            
            cubeGrid.OnDestroyed += OnGridDestroyed;
        }

        private void OnGridDestroyed(object sender, EventArgs e, bool immediately)
        {
            if (immediately)
                CubeTransform.gameObject.SetActive(false);
            else 
                _anim.SetBool(_cube.IsDestroyedHash, true);
            
            isGrounded = false;
        }
        
        public void UpdatePosition() 
        {
            var targetPosition = _cubeGrid.GetWorldPosition();
            var moveDir = (targetPosition - CubeTransform.position);
            
            CubeTransform.position += moveDir * Cube.MoveSpeed * Time.deltaTime;
            CubeTransform.localScale = Vector3.one;
            
            isGrounded = !(moveDir.magnitude > 0.03f);
        }
    }
}
