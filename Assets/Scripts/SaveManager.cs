using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public const string GridData = "gridPositions.json";
    public const string LevelData = "currentLevelInfo.json";
    
    public static bool HasSaveFile(string fileName)
    {
        return File.Exists(Path.Combine(Application.persistentDataPath, fileName));
    }
    
    public static T GetDataJson<T>(string fileName)
    {
        var json = File.ReadAllText(Path.Combine(Application.persistentDataPath, fileName));
        return JsonUtility.FromJson<T>(json);
    }
    
    public static void SetDataJson<T>(string fileName, T value, bool prettyPrint)
    {
        var json = JsonUtility.ToJson(value, prettyPrint);
        File.WriteAllText(Path.Combine(Application.persistentDataPath, fileName), json);
    }
    
    public static void DeleteSaveFile(string fileName)
    {
        File.Delete(Path.Combine(Application.persistentDataPath, fileName));
    }
}