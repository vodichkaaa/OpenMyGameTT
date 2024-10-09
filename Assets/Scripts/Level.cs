using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class Level : ScriptableObject
{
    [Space(5f)]
    public int width;
    public int height;
    
    [Space(5f)]
    public List<LevelGridPosition> levelGridPositionList;

    private void OnValidate()
    {
        var t = 0;
        if (levelGridPositionList.Count > width * height) return;
        for (var i = 0; i < height; i++)
        {
            for (var j = 0; j < width; j++)
            {
                if (levelGridPositionList.Count - 1 < t) continue;
                levelGridPositionList[t].x = j;
                levelGridPositionList[t].y = i;
                t++;
            }
        }
    }

    [Serializable]
    public class LevelGridPosition 
    {
        public Cube cube;
        public int x;
        public int y;
    }

}
