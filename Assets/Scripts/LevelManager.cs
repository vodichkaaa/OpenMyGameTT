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
    
    private int _currentLevelIndex = 0;

    private void Start()
    {
        if (Serializer.HasKeyCache("CurrentLevelIndex"))
            _currentLevelIndex = Serializer.GetData<int>("CurrentLevelIndex");
        
        SetIndexLevel();
    }

    public void SetIndexLevel()
    {
        _match3.ClearLevel();
        _match3.SetLevel(_levels[_currentLevelIndex]);
    }
    
    public void SetIndex()
    {
        if (_currentLevelIndex >= 0 && _currentLevelIndex < _levels.Length - 1)
        {
            _currentLevelIndex++;
        }
        else _currentLevelIndex = 0;
        
        Serializer.SetData("CurrentLevelIndex", _currentLevelIndex);
    }
}
