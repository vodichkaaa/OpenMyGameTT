using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Visual Representation of the underlying Match3 Grid
 * */
public class Match3Visual : MonoBehaviour {

    public event EventHandler OnStateChanged;

    public enum State {
        Busy,
        WaitingForUser,
        TryFindMatches,
    }

    [SerializeField] private Transform pfCubeGridVisual;
    [SerializeField] private Match3 match3;

    private Grid<Match3.CubeGridPosition> grid;
    private Dictionary<Match3.CubeGrid, CubeGridVisual> cubeGridDictionary;

    private bool isSetup;
    private State state;
    private float busyTimer;
    private Action onBusyTimerElapsedAction;

    private int startDragX;
    private int startDragY;
    private Vector3 startDragMouseWorldPosition;

    private void Awake() 
    {
        state = State.Busy;
        isSetup = false;

        match3.OnLevelSet += OnLevelSet;
    }

    private void OnLevelSet(object sender, Match3.OnLevelSetEventArgs e) 
    {
        Setup(sender as Match3, e.grid);
    }

    public void Setup(Match3 match3, Grid<Match3.CubeGridPosition> grid) 
    {
        this.match3 = match3;
        this.grid = grid;

        match3.OnCubeGridPositionDestroyed += Match3_OnCubeGridPositionDestroyed;

        // Initialize Visual
        cubeGridDictionary = new Dictionary<Match3.CubeGrid, CubeGridVisual>();

        for (int x = 0; x < grid.GetWidth(); x++) 
        {
            for (int y = 0; y < grid.GetHeight(); y++) 
            {
                Match3.CubeGridPosition cubeGridPosition = grid.GetGridObject(x, y);
                Match3.CubeGrid cubeGrid = cubeGridPosition.GetCubeGrid();

                Vector3 position = grid.GetWorldPosition(x, y);
                position = new Vector3(position.x, 12);

                // Visual Transform
                Transform cubeGridVisualTransform = Instantiate(pfCubeGridVisual, position, Quaternion.identity);
                cubeGridVisualTransform.GetComponentInChildren<SpriteRenderer>().sprite = cubeGrid.GetCube().sprite;

                CubeGridVisual cubeGridVisual = new CubeGridVisual(cubeGridVisualTransform, cubeGrid);

                cubeGridDictionary[cubeGrid] = cubeGridVisual;
            }
        }

        SetBusyState(.5f, () => SetState(State.TryFindMatches));

        isSetup = true;
    }

    private void Match3_OnCubeGridPositionDestroyed(object sender, System.EventArgs e) 
    {
        if (sender is Match3.CubeGridPosition cubeGridPosition && cubeGridPosition.GetCubeGrid() != null) 
        {
            cubeGridDictionary.Remove(cubeGridPosition.GetCubeGrid());
        }
    }

    private void Update() {
        if (!isSetup) return;

        UpdateVisual();

        switch (state) {
            case State.Busy:
                busyTimer -= Time.deltaTime;
                if (busyTimer <= 0f) {
                    onBusyTimerElapsedAction();
                }
                break;
            case State.WaitingForUser:
                if (Input.GetMouseButtonDown(0)) {
                    Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);;
                    grid.GetXY(mouseWorldPosition, out startDragX, out startDragY);

                    startDragMouseWorldPosition = mouseWorldPosition;
                }

                if (Input.GetMouseButtonUp(0)) {
                    Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);;
                    grid.GetXY(mouseWorldPosition, out int x, out int y);

                    if (x != startDragX) 
                    {
                        // Different X
                        y = startDragY;

                        if (x < startDragX) 
                            x = startDragX - 1;
                        else 
                            x = startDragX + 1;
                    } 
                    else 
                    {
                        // Different Y
                        x = startDragX;

                        if (y < startDragY) 
                            y = startDragY - 1;
                        else 
                            y = startDragY + 1;
                    }

                    /*if (match3.CanSwapGridPositions(startDragX, startDragY, x, y)) 
                    {
                        SwapGridPositions(startDragX, startDragY, x, y);
                    }*/
                    SwapGridPositions(startDragX, startDragY, x, y);
                }
                break;
            case State.TryFindMatches:
                if (match3.TryFindMatchesAndDestroyThem()) 
                {
                    SetBusyState(.3f, () =>
                    {
                        match3.FallCubesIntoEmptyPositions();

                        SetBusyState(.3f, () => 
                        {
                            SetBusyState(.5f, () => SetState(State.TryFindMatches));
                        });
                    });
                } 
                else 
                {
                    TrySetStateWaitingForUser();
                }
                break;
        }
    }

    private void UpdateVisual() 
    {
        foreach (Match3.CubeGrid cubeGrid in cubeGridDictionary.Keys) 
        {
            cubeGridDictionary[cubeGrid].Update();
        }
    }

    public void SwapGridPositions(int startX, int startY, int endX, int endY) 
    {
        match3.SwapGridPositions(startX, startY, endX, endY);

        SetBusyState(.5f, () => SetState(State.TryFindMatches));
    }

    private void SetBusyState(float busyTimer, Action onBusyTimerElapsedAction) 
    {
        SetState(State.Busy);
        this.busyTimer = busyTimer;
        this.onBusyTimerElapsedAction = onBusyTimerElapsedAction;
    }

    private void TrySetStateWaitingForUser() 
    {
        SetState(State.WaitingForUser);
    }

    private void SetState(State state) 
    {
        this.state = state;
        OnStateChanged?.Invoke(this, EventArgs.Empty);
    }

    public State GetState() 
    {
        return state;
    }

    public class CubeGridVisual 
    {
        private Transform transform;
        private Match3.CubeGrid cubeGrid;

        public CubeGridVisual(Transform transform, Match3.CubeGrid cubeGrid) {
            this.transform = transform;
            this.cubeGrid = cubeGrid;

            cubeGrid.OnDestroyed += cubeGrid_OnDestroyed;
        }

        private void cubeGrid_OnDestroyed(object sender, System.EventArgs e) {
            transform.GetComponent<Animation>().Play();
            Destroy(transform.gameObject, 1f);
        }

        public void Update() {
            Vector3 targetPosition = cubeGrid.GetWorldPosition();
            Vector3 moveDir = (targetPosition - transform.position);
            float moveSpeed = 10f;
            transform.position += moveDir * moveSpeed * Time.deltaTime;
        }

    }
}
