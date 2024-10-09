using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [SerializeField] 
    private Level[] _levels;
    [SerializeField]
    private Match3 _match3;
    private LevelInfo _levelInfo;

    private void Awake()
    {
        _match3.OnCubeDestroyed += OnCubeDestroyed;
    }

    private void Start()
    {
        /*if (SaveManager.HasKeyCache("CurrentLevelIndex"))
            _currentLevelIndex = SaveManager.GetData<int>("CurrentLevelIndex");*/

        _levelInfo = SaveManager.HasSaveFile(SaveManager.LevelData) ? 
            SaveManager.GetDataJson<LevelInfo>(SaveManager.LevelData) : new LevelInfo();
        
        SetIndexLevel(false);
    }

    private void OnCubeDestroyed()
    {
        if(_match3.CurrentActiveCubes <= 0)
        {
            SetIndex();
            SetIndexLevel(true);
        }
    }

    public void SetIndexLevel(bool clearSave)
    {
        _match3.ClearLevel(clearSave);
        _match3.SetLevel(_levels[_levelInfo.currentLevelIndex]);
    }
    
    public void SetIndex()
    {
        if (_levelInfo.currentLevelIndex >= 0 && _levelInfo.currentLevelIndex < _levels.Length - 1)
        {
            _levelInfo.currentLevelIndex++;
        }
        else _levelInfo.currentLevelIndex = 0;
        
        SaveManager.SetDataJson(SaveManager.LevelData, _levelInfo, true);
    }
}

[Serializable]
public class LevelInfo
{
    public int currentLevelIndex;
}
