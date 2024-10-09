using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class Cube : ScriptableObject 
{
    //public Sprite sprite;
    public GameObject prefab;
    
    [Header("Properties")]
    [NonSerialized] public float moveSpeed = 10f;
    
    [NonSerialized] public int isDestroyedHash = Animator.StringToHash("isDestroyed");
}
