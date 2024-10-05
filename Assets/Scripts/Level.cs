using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class Level : ScriptableObject 
{
    public List<Cube> cubeList;
    public int width;
    public int height;
    public List<LevelGridPosition> levelGridPositionList;
    
    [System.Serializable]
    public class LevelGridPosition 
    {
        public Cube cube;
        public int x;
        public int y;
    }

}
