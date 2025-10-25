using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class CacheData
{
    public Dictionary<string, string> entries = new Dictionary<string, string>();
}

public class LocalCacheManager : MonoBehaviour
{
    private CacheData cacheData = new CacheData();
    private string cacheFilePath;

    void Awake()
    {
        cacheFilePath = Path.Combine(Application.persistentDataPath, "cache.json");
        LoadCache();
    }

    public void AddToCache(string key, string value)
    {
        cacheData.entries[key] = value;
        SaveCache();
        Debug.Log($"[Cache] Salvou '{key}' → '{value}'");
    }

    public string GetFromCache(string key)
    {
        if (cacheData.entries.TryGetValue(key, out string value))
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
            string json = JsonUtility.ToJson(cacheData, true);
            File.WriteAllText(cacheFilePath, json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Erro ao salvar cache: {e.Message}");
        }
    }

    public void LoadCache()
    {
        if (File.Exists(cacheFilePath))
        {
            try
            {
                string json = File.ReadAllText(cacheFilePath);
                cacheData = JsonUtility.FromJson<CacheData>(json);
                Debug.Log("[Cache] Cache local carregado com sucesso.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Erro ao carregar cache: {e.Message}");
            }
        }
        else
        {
            Debug.Log("[Cache] Nenhum cache existente encontrado.");
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
                AddToCache(key, result);
                return result;
            }
            else
            {
                Debug.Log("[Cache] Sem internet — usando fallback local.");
            }
        }
        catch
        {
            Debug.LogWarning("[Cache] Falha ao buscar online — usando fallback local.");
        }

        return GetFromCache(key);
    }
}
