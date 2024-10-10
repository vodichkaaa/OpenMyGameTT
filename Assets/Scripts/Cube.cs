using System;
using UnityEngine;

[CreateAssetMenu()]
public class Cube : ScriptableObject 
{
    public GameObject prefab;
    
    [NonSerialized] public const float MoveSpeed = 10f;
    [NonSerialized] public readonly int IsDestroyedHash = Animator.StringToHash("isDestroyed");
}
