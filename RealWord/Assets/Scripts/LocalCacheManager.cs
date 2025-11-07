using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class CacheData
{
    // Unity's JsonUtility cannot serialize Dictionary, so we use parallel Lists
    public List<string> keys = new List<string>();
    public List<string> values = new List<string>();

    // Helper method to convert to Dictionary for in-memory use
    public Dictionary<string, string> ToDictionary()
    {
        Dictionary<string, string> dict = new Dictionary<string, string>();
        for (int i = 0; i < keys.Count && i < values.Count; i++)
        {
            dict[keys[i]] = values[i];
        }
        return dict;
    }

    // Helper method to populate from Dictionary for serialization
    public void FromDictionary(Dictionary<string, string> dict)
    {
        keys.Clear();
        values.Clear();
        foreach (var kvp in dict)
        {
            keys.Add(kvp.Key);
            values.Add(kvp.Value);
        }
    }
}

public class LocalCacheManager : MonoBehaviour
{
    private Dictionary<string, string> entries = new Dictionary<string, string>();
    private string cacheFilePath;

    void Awake()
    {
        cacheFilePath = Path.Combine(Application.persistentDataPath, "cache.json");
        LoadCache();
    }

    public void AddToCache(string key, string value)
    {
        entries[key] = value;
        SaveCache();
        Debug.Log($"[Cache] Salvou '{key}' → '{value}'");
    }

    public string GetFromCache(string key)
    {
        if (entries.TryGetValue(key, out string value))
        {
            Debug.Log($"[Cache] Recuperado do cache: {key} → {value}");
            return value;
        }

        Debug.Log($"[Cache] Nenhum valor encontrado para {key}");
        return null;
    }

    public void SaveCache()
    {
        try
        {
            CacheData cacheData = new CacheData();
            cacheData.FromDictionary(entries);
            string json = JsonUtility.ToJson(cacheData, true);
            File.WriteAllText(cacheFilePath, json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Erro ao salvar cache: {e.Message}\nStack trace: {e.StackTrace}");
        }
    }

    public void LoadCache()
    {
        if (File.Exists(cacheFilePath))
        {
            try
            {
                string json = File.ReadAllText(cacheFilePath);
                CacheData cacheData = JsonUtility.FromJson<CacheData>(json);
                entries = cacheData.ToDictionary();
                Debug.Log($"[Cache] Cache local carregado com sucesso. Entradas: {entries.Count}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Erro ao carregar cache: {e.Message}\nStack trace: {e.StackTrace}");
                entries = new Dictionary<string, string>(); // Initialize to empty if load fails
            }
        }
        else
        {
            Debug.Log("[Cache] Nenhum cache existente encontrado.");
            entries = new Dictionary<string, string>();
        }
    }

    public string GetOfflineFallback(string key, System.Func<string> onlineFetch)
    {
        string result = null;

        try
        {
            if (Application.internetReachability != NetworkReachability.NotReachable)
            {
                result = onlineFetch();
                
                // Only cache non-null and non-empty results
                if (!string.IsNullOrEmpty(result))
                {
                    AddToCache(key, result);
                    return result;
                }
                else
                {
                    Debug.LogWarning($"[Cache] Online fetch returned null or empty for key '{key}'. Not caching.");
                }
            }
            else
            {
                Debug.Log("[Cache] Sem internet — usando fallback local.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Cache] Falha ao buscar online — usando fallback local. Exception: {e.Message}");
        }

        // Try to get from cache
        result = GetFromCache(key);
        
        // Log error if both online fetch and cache retrieval failed
        if (result == null)
        {
            Debug.LogError($"[Cache] Falha ao obter dados para '{key}': sem conexão à internet e sem cache disponível.");
        }
        
        return result;
    }
}
