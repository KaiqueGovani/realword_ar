using UnityEngine;

public class CacheTestExample : MonoBehaviour
{
    private LocalCacheManager cache;

    [System.Obsolete]
    void Start()
    {
        cache = FindObjectOfType<LocalCacheManager>();

        cache.AddToCache("teste", "Frase de teste salva no cache.");
        string recuperado = cache.GetFromCache("teste");
        Debug.Log($"Recuperado: {recuperado}");
    }
}
