using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;


public class BalloonSpawner : MonoBehaviour
{
    [Header("Sprites")] 
    [SerializeField]
    private Balloon _balloonPrefab;
    
    [SerializeField] 
    private List<Sprite> _sprites;
    
    private List<Balloon> _balloonsList;
    private List<Balloon> _activeBalloons;
    
    private int _currentVisibleBalloons;

    private const int MaxVisibleBalloons = 3;
    private const float ScreenBorder = 100f;
    
    private void Start()
    {
        _balloonsList = new List<Balloon>();
        _activeBalloons = new List<Balloon>();
        
        for (var i = 0; i < 10; i++)
        {
            var balloon = Instantiate(_balloonPrefab, transform);
            balloon.GetComponent<Image>().sprite = _sprites[Random.Range(0, _sprites.Count)];
            
            _balloonsList.Add(balloon);
            balloon.gameObject.SetActive(false);
        }

        SetActiveBalloons();
    }

    private void SetActiveBalloons()
    {
        for (var i = _currentVisibleBalloons; i < MaxVisibleBalloons; i++)
        {
            var balloonIndex = (int)Random.Range(0f, _balloonsList.Count);
            
            _balloonsList[balloonIndex].gameObject.SetActive(true);
            
            _activeBalloons.Add(_balloonsList[balloonIndex]);
            _balloonsList.RemoveAt(balloonIndex);

            _currentVisibleBalloons++;
        }
    }

    private void LateUpdate()
    {
        for (var i = 0; i < _activeBalloons.Count; i++)
        {
            var isOffScreen = CheckTargetVisibility(_activeBalloons[i].transform);

            if (isOffScreen)
            {
                _activeBalloons[i].gameObject.SetActive(false);
                
                _balloonsList.Add(_activeBalloons[i]);
                _activeBalloons.RemoveAt(i);

                _currentVisibleBalloons--;
                SetActiveBalloons();
            }
        }
    }
    
    private bool CheckTargetVisibility(Transform target)
    {
        var isOffScreen = target.localPosition.x <= -(Screen.width / 2 + ScreenBorder) || 
                          target.localPosition.x >= Screen.width / 2 + ScreenBorder;
        
        return isOffScreen;
    }
}
