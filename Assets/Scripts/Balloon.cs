using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class Balloon : MonoBehaviour
{
    [SerializeField]
    private float2 _moveSpeedValue = new (2f, 4f);
    [SerializeField]
    private float2 _frequencyValue = new (5f, 10f);
    [SerializeField] 
    private float2 _magnitudeValue = new (0.25f, 0.5f);
    
    private float _moveSpeed;
    private float _frequency;
    private float _magnitude;

    private bool _isFacingRight = true;
    
    private Vector3 _pos;

    private const float SpawnOffsetX = 100f; 
    private const float SpawnOffsetY = 400f; 
    
    private void OnEnable()
    {
        SetEnableValues();
        
        _pos = transform.position;
    }

    private void SetEnableValues()
    {
        _moveSpeed = Random.Range(_moveSpeedValue.x, _moveSpeedValue.y);
        _frequency = Random.Range(_frequencyValue.x, _frequencyValue.y);
        _magnitude = Random.Range(_magnitudeValue.x, _magnitudeValue.y);
        
        _isFacingRight = Random.Range(0, 2) == 0;

        var spawnWidth = Screen.width / 2;
        var spawnHeight = Screen.height / 2;

        if (_isFacingRight)
            transform.localPosition = new Vector3(-(spawnWidth + SpawnOffsetX), 
                Random.Range(spawnHeight - SpawnOffsetY, -(spawnHeight - SpawnOffsetY)));
        else
            transform.localPosition = new Vector3(spawnWidth + SpawnOffsetX, 
                Random.Range(spawnHeight - SpawnOffsetY, -(spawnHeight - SpawnOffsetY)));
    }
    
    private void Update()
    {
        Move(_isFacingRight);
    }

    private void Move(bool isFacingRight)
    {
        var movePos = transform.right * (Time.deltaTime * _moveSpeed);
        
        if (isFacingRight) 
            _pos += movePos;
        else
            _pos -= movePos;
        
        transform.position = _pos + transform.up * Mathf.Sin(Time.time * _frequency) * _magnitude;
    }
}
