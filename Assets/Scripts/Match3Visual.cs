using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Visual Representation of the underlying Match3 Grid
 * */
public class Match3Visual : MonoBehaviour 
{
    public enum State 
    {
        Busy,
        WaitingForUser,
        TryFindMatches,
    }

    [SerializeField] 
    private Transform _cubePrefab;
    
    [SerializeField] 
    private Match3 _match3;

    private Grid<Match3.CubeGridPosition> _grid;
    private Dictionary<Match3.CubeGrid, CubeGridVisual> _cubeGridDictionary;

    private bool _isSetup;
    
    private bool _isTimerSet;
    private float _busyTimer;
    
    private State _state;
    private Action _onBusyTimerElapsedAction;

    private int _startDragX;
    private int _startDragY;

    private void Awake() 
    {
        //_state = State.Busy;
        _state = State.WaitingForUser;
        _isSetup = false;

        _match3.OnLevelSet += OnLevelSet;
    }
    
    private void OnLevelSet(object sender, Match3.OnLevelSetEventArgs e) 
    {
        Setup(sender as Match3, e.grid);
    }

    private void Setup(Match3 match3, Grid<Match3.CubeGridPosition> grid) 
    {
        _match3 = match3;
        _grid = grid;

        _match3.OnCubeGridPositionDestroyed += Match3_OnCubeGridPositionDestroyed;

        // Initialize Visual
        _cubeGridDictionary = new Dictionary<Match3.CubeGrid, CubeGridVisual>();

        for (var x = 0; x < _grid.GetWidth(); x++) 
        {
            for (var y = 0; y < _grid.GetHeight(); y++) 
            {
                Match3.CubeGridPosition cubeGridPosition = _grid.GetGridObject(x, y);
                Match3.CubeGrid cubeGrid = cubeGridPosition.GetCubeGrid();

                Vector3 position = _grid.GetWorldPosition(x, y);
                position = new Vector3(position.x, 12);

                // Visual Transform
                if(cubeGrid != null && cubeGrid.GetCube() != null)
                {
                    Transform cubeGridVisualTransform = Instantiate(_cubePrefab, position, Quaternion.identity);
                    cubeGridVisualTransform.GetComponentInChildren<SpriteRenderer>().sprite = cubeGrid.GetCube().sprite;

                    CubeGridVisual cubeGridVisual = new CubeGridVisual(cubeGridVisualTransform, cubeGrid);

                    _cubeGridDictionary[cubeGrid] = cubeGridVisual;
                }
            }
        }

        SeDelayedState(.5f, () => SetState(State.TryFindMatches));

        _isSetup = true;
    }

    private void Match3_OnCubeGridPositionDestroyed(object sender, EventArgs e) 
    {
        if (sender is Match3.CubeGridPosition cubeGridPosition && cubeGridPosition.GetCubeGrid() != null) 
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
                    var mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    _grid.GetXY(mouseWorldPosition, out _startDragX, out _startDragY);
                }

                if (Input.GetMouseButtonUp(0))
                {
                    var mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    _grid.GetXY(mouseWorldPosition, out int x, out int y);

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
                        SwapGridPositions(_startDragX, _startDragY, x, y);
                    }
                }
                
                break;
            }
            case State.TryFindMatches:
            {
                /*if (_match3.TryFindMatchesAndDestroyThem())
                {
                    SeDelayedState(.3f, () =>
                    {
                        _match3.FallCubesIntoEmptyPositions();
                        SeDelayedState(.2f, () => SetState(State.TryFindMatches));
                    });
                }
                else TrySetStateWaitingForUser();*/
                
                SeDelayedState(.3f, () =>
                {
                    if (_match3.TryFindMatchesAndDestroyThem())
                    {
                        _match3.FallCubesIntoEmptyPositions();
                        SetState(State.TryFindMatches);
                    }
                    else TrySetStateWaitingForUser();
                });

                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void UpdateTimer()
    {
        if(_isTimerSet)
        {
            _busyTimer -= Time.deltaTime;
            if (_busyTimer <= 0f)
            {
                _isTimerSet = false;
                _onBusyTimerElapsedAction();
            }
        }
    }

    private void UpdateVisual() 
    {
        foreach (var cubeGrid in _cubeGridDictionary.Keys) 
        {
            _cubeGridDictionary[cubeGrid].Update();
        }
    }

    private void SwapGridPositions(int startX, int startY, int endX, int endY) 
    {
        _match3.SwapGridPositions(startX, startY, endX, endY);
        
        if (_isTimerSet) return;
        SeDelayedState(0.3f, () =>
        {
            _match3.FallCubesIntoEmptyPositions();
            SetState(State.TryFindMatches);
        });
    }

    private void SeDelayedState(float busyTimer, Action onBusyTimerElapsedAction) 
    {
        SetState(State.WaitingForUser);
        _onBusyTimerElapsedAction = onBusyTimerElapsedAction;

        _busyTimer = busyTimer;
        _isTimerSet = true;
    }

    private void TrySetStateWaitingForUser() 
    {
        SetState(State.WaitingForUser);
    }

    private void SetState(State state) 
    {
        _state = state;
    }

    public State GetState() 
    {
        return _state;
    }

    private class CubeGridVisual
    {
        private readonly Transform _transform;
        private readonly Match3.CubeGrid _cubeGrid;

        private const float MoveSpeed = 10f;

        public CubeGridVisual(Transform transform, Match3.CubeGrid cubeGrid) 
        {
            _transform = transform;
            _cubeGrid = cubeGrid;

            cubeGrid.OnDestroyed += OnGridDestroyed;
        }

        private void OnGridDestroyed(object sender, EventArgs e) 
        {
            _transform.GetComponent<Animation>().Play();
            Destroy(_transform.gameObject, 1f);
        }

        public void Update() 
        {
            Vector3 targetPosition = _cubeGrid.GetWorldPosition();
            Vector3 moveDir = (targetPosition - _transform.position);
            
            _transform.position += moveDir * MoveSpeed * Time.deltaTime;
        }

    }
}
