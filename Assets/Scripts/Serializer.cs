using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

public class Serializer : MonoBehaviour
{
    private static Dictionary<string, string> cacheData = new();

    private void Awake()
    {
        LoadState();
    }

    #region Storage
    
    public static bool CheckFileSave()
    {
        return File.Exists(Application.dataPath + "/datapath.json");
    }
    public static void LoadState()
    {
        if (CheckFileSave())
        {
            var serializedState = File.ReadAllText(Application.dataPath + "/datapath.json");
            cacheData = JsonConvert.DeserializeObject<Dictionary<string, string>>(serializedState);
        }
        else
        {
            cacheData = new Dictionary<string, string>();
        }
    }
    
    public static void SaveState()
    {
        var serializedState = JsonConvert.SerializeObject(cacheData);
        File.WriteAllText(Application.dataPath + "/datapath.json", serializedState);
    }
    
    #endregion

    #region Cache
    
    public static T GetDataJson<T>(string key)
    {
        var serializedData = cacheData[key];
        return JsonUtility.FromJson<T>(serializedData);
    }
    
    public static T GetData<T>(string key)
    {
        var serializedData = cacheData[key];
        return JsonConvert.DeserializeObject<T>(serializedData); 
    }
    
    public static void SetDataJson<T>(string key, T value)
    {
        var serializedData = JsonUtility.ToJson(value, true);
        //Debug.Log($"{serializedData}");
        cacheData[key] = serializedData;
        SaveState();
    }
    
    public static void SetData<T>(string key, T value)
    {
        var serializedData = JsonConvert.SerializeObject(value);
        
        //Debug.Log($"{serializedData}");
        
        cacheData[key] = serializedData;
        SaveState();
    }
    
    public static bool HasKeyCache(string key)
    {
        return cacheData.ContainsKey(key);
    }
    
    public static void DeleteKey(string key)
    {
        if (HasKeyCache(key))
        {
            cacheData.Remove(key);
            SaveState();
        }
    }
    
    public static void DeleteAll()
    {
        cacheData.Clear();
        SaveState();
    }

    #endregion
    
    public static bool TryGetData<T>(string key, out T value)
    {
        if (cacheData.TryGetValue(key, out var serializedData))
        {
            value = JsonConvert.DeserializeObject<T>(serializedData);
            return true;
        }

        value = default;
        return false;
    }
}