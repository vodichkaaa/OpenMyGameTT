using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class Cube : ScriptableObject 
{
    //public Sprite sprite;
    public GameObject prefab;
    
    [Header("Properties")]
    public float moveSpeed = 10f;
    public float destroyDelay = 0.5f;
    
    [HideInInspector]
    public int isDestroyedHash = Animator.StringToHash("isDestroyed");
}
