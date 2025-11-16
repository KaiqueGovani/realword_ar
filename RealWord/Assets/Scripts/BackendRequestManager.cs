using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Manages backend API requests with deduplication and caching.
/// Handles all communication with the RealWord API.
/// </summary>
public class BackendRequestManager : MonoBehaviour
{
    private const string API_URL = "https://realword.kaique.net/sentences";
    private const string CONTEXT = "BackendRequestManager";
    private const string DEFAULT_LANGUAGE = "português brasileiro";
    private const string LANGUAGE_PREF_KEY = "RealWord_TargetLanguage";

    [Header("Dependencies")]
    public LocalCacheManager cacheManager;

    // Track pending requests to prevent duplicates
    private Dictionary<string, bool> pendingRequests = new Dictionary<string, bool>();

    // Callback for when requests complete
    public System.Action<string, string, SentencesResponseDto> OnRequestComplete;
    public System.Action<string, string, string> OnRequestFailed;

    void Awake()
    {
        if (cacheManager == null)
        {
            cacheManager = FindObjectOfType<LocalCacheManager>();
            if (cacheManager == null)
            {
                AppLogger.Error("LocalCacheManager not found. Caching will be disabled.", CONTEXT);
            }
        }
    }

    /// <summary>
    /// Gets the target language from settings, with fallback to Portuguese
    /// </summary>
    public string GetLanguageFromSettings()
    {
        // Try to get from PlayerPrefs (if settings system uses it)
        string language = PlayerPrefs.GetString(LANGUAGE_PREF_KEY, DEFAULT_LANGUAGE);
        
        // If empty or invalid, use default
        if (string.IsNullOrEmpty(language))
        {
            language = DEFAULT_LANGUAGE;
        }

        // Normalize to lowercase for consistency
        return language.ToLower();
    }

    /// <summary>
    /// Gets cached sentences for an object, if available
    /// </summary>
    public SentencesResponseDto GetCachedSentences(string objectName, string language = null)
    {
        if (cacheManager == null)
        {
            return null;
        }

        if (string.IsNullOrEmpty(language))
        {
            language = GetLanguageFromSettings();
        }

        string cacheKey = GenerateCacheKey(objectName, language);
        string cachedJson = cacheManager.GetFromCache(cacheKey);

        if (!string.IsNullOrEmpty(cachedJson))
        {
            try
            {
                SentencesResponseDto response = JsonUtility.FromJson<SentencesResponseDto>(cachedJson);
                AppLogger.Info($"Cache hit for '{objectName}' in '{language}'", CONTEXT);
                return response;
            }
            catch (System.Exception e)
            {
                AppLogger.Warning($"Failed to deserialize cached data for '{objectName}': {e.Message}", CONTEXT);
                return null;
            }
        }

        AppLogger.Info($"Cache miss for '{objectName}' in '{language}'", CONTEXT);
        return null;
    }

    /// <summary>
    /// Checks if a request is currently pending
    /// </summary>
    public bool IsRequestPending(string objectName, string language = null)
    {
        if (string.IsNullOrEmpty(language))
        {
            language = GetLanguageFromSettings();
        }

        string cacheKey = GenerateCacheKey(objectName, language);
        return pendingRequests.ContainsKey(cacheKey) && pendingRequests[cacheKey];
    }

    /// <summary>
    /// Requests sentences for an object from the backend API.
    /// Checks cache first, then queues request if not already pending.
    /// </summary>
    public void RequestSentences(string objectName, string language = null)
    {
        if (string.IsNullOrEmpty(objectName))
        {
            AppLogger.Warning("Attempted to request sentences for empty object name", CONTEXT);
            return;
        }

        if (string.IsNullOrEmpty(language))
        {
            language = GetLanguageFromSettings();
        }

        string cacheKey = GenerateCacheKey(objectName, language);

        // Check cache first
        SentencesResponseDto cached = GetCachedSentences(objectName, language);
        if (cached != null)
        {
            AppLogger.Info($"Using cached data for '{objectName}'", CONTEXT);
            OnRequestComplete?.Invoke(objectName, language, cached);
            return;
        }

        // Check if request is already pending
        if (IsRequestPending(objectName, language))
        {
            AppLogger.Info($"Request already pending for '{objectName}' in '{language}'. Skipping duplicate.", CONTEXT);
            return;
        }

        // Mark as pending and start request
        pendingRequests[cacheKey] = true;
        StartCoroutine(SendRequestCoroutine(objectName, language));
    }

    /// <summary>
    /// Coroutine that sends the API request
    /// </summary>
    private IEnumerator SendRequestCoroutine(string objectName, string language)
    {
        string cacheKey = GenerateCacheKey(objectName, language);

        AppLogger.Info($"Starting API request for '{objectName}' in '{language}'", CONTEXT);

        // Create request DTO
        SentenceRequestDto requestDto = new SentenceRequestDto(objectName, language);

        // Serialize to JSON
        string jsonBody = JsonUtility.ToJson(requestDto);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);

        using (UnityWebRequest request = new UnityWebRequest(API_URL, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 30; // 30 second timeout

            yield return request.SendWebRequest();

            // Clear pending flag
            pendingRequests[cacheKey] = false;

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string responseText = request.downloadHandler.text;
                    SentencesResponseDto responseDto = JsonUtility.FromJson<SentencesResponseDto>(responseText);

                    // Validate response
                    if (responseDto == null || responseDto.phrases == null || responseDto.translations == null)
                    {
                        throw new System.Exception("Invalid response structure");
                    }

                    if (responseDto.phrases.Length == 0)
                    {
                        throw new System.Exception("Response contains no phrases");
                    }

                    if (responseDto.phrases.Length != responseDto.translations.Length)
                    {
                        throw new System.Exception($"Phrases count ({responseDto.phrases.Length}) doesn't match translations count ({responseDto.translations.Length})");
                    }

                    // Save to cache
                    if (cacheManager != null)
                    {
                        cacheManager.AddToCache(cacheKey, responseText);
                    }

                    AppLogger.Info($"API request successful for '{objectName}' in '{language}'. Received {responseDto.phrases.Length} phrases.", CONTEXT);

                    // Notify listeners
                    OnRequestComplete?.Invoke(objectName, language, responseDto);
                }
                catch (System.Exception e)
                {
                    AppLogger.Error($"Failed to parse API response for '{objectName}': {e.Message}", CONTEXT, new Dictionary<string, object>
                    {
                        { "object", objectName },
                        { "language", language },
                        { "error", e.Message }
                    });

                    OnRequestFailed?.Invoke(objectName, language, e.Message);
                }
            }
            else
            {
                string errorMessage = request.error ?? "Unknown error";
                AppLogger.Error($"API request failed for '{objectName}': {errorMessage}", CONTEXT, new Dictionary<string, object>
                {
                    { "object", objectName },
                    { "language", language },
                    { "error", errorMessage },
                    { "result", request.result.ToString() }
                });

                OnRequestFailed?.Invoke(objectName, language, errorMessage);
            }
        }
    }

    /// <summary>
    /// Generates a cache key from object name and language
    /// </summary>
    private string GenerateCacheKey(string objectName, string language)
    {
        // Normalize: lowercase, trim
        string normalizedObject = objectName.ToLower().Trim();
        string normalizedLanguage = language.ToLower().Trim();
        return $"{normalizedObject}_{normalizedLanguage}";
    }

    /// <summary>
    /// Sets the target language preference
    /// </summary>
    public void SetLanguagePreference(string language)
    {
        if (!string.IsNullOrEmpty(language))
        {
            PlayerPrefs.SetString(LANGUAGE_PREF_KEY, language);
            PlayerPrefs.Save();
            AppLogger.Info($"Language preference updated to: {language}", CONTEXT);
        }
    }
}

