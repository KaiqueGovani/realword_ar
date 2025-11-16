using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages detection results, coordinates between YOLO detector, backend API, and UI.
/// Handles carousel navigation and overlay display logic.
/// </summary>
public class DetectionResultManager : MonoBehaviour
{
    private const string CONTEXT = "DetectionResultManager";

    [Header("Dependencies")]
    public YoloDetector_IE yoloDetector;
    public BackendRequestManager backendManager;
    public UIManager uiManager;
    public AndroidTTS tts;

    // Track highest confidence detection
    private string currentBestObject = null;
    private float currentBestConfidence = 0f;

    // Track current displayed object
    private string currentDisplayedObject = null;

    // Track which object the overlay button is for
    private string pendingOverlayObject = null;

    // Track carousel state per object (phrase index)
    private Dictionary<string, int> carouselIndices = new Dictionary<string, int>();

    // Store API responses per object
    private Dictionary<string, SentencesResponseDto> objectData = new Dictionary<string, SentencesResponseDto>();

    void Start()
    {
        // Subscribe to YOLO detection events
        if (yoloDetector != null)
        {
            yoloDetector.OnObjectDetected += OnObjectDetected;
            AppLogger.Info("Subscribed to YOLO detector events", CONTEXT);
        }
        else
        {
            AppLogger.Warning("YoloDetector_IE not assigned. Detection events will not be received.", CONTEXT);
        }

        // Subscribe to backend request completion
        if (backendManager != null)
        {
            backendManager.OnRequestComplete += OnBackendRequestComplete;
            backendManager.OnRequestFailed += OnBackendRequestFailed;
            AppLogger.Info("Subscribed to backend request events", CONTEXT);
        }
        else
        {
            AppLogger.Warning("BackendRequestManager not assigned. API requests will not work.", CONTEXT);
        }

        // Initialize UI
        if (uiManager != null)
        {
            uiManager.ShowOverlayButton(false);
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (yoloDetector != null)
        {
            yoloDetector.OnObjectDetected -= OnObjectDetected;
        }

        if (backendManager != null)
        {
            backendManager.OnRequestComplete -= OnBackendRequestComplete;
            backendManager.OnRequestFailed -= OnBackendRequestFailed;
        }
    }

    /// <summary>
    /// Called when YOLO detects an object
    /// </summary>
    private void OnObjectDetected(string objectName, float confidence)
    {
        if (string.IsNullOrEmpty(objectName))
        {
            return;
        }

        AppLogger.Info($"Object detected: '{objectName}' (confidence: {confidence:F2})", CONTEXT);

        // Update highest confidence detection
        if (currentBestObject == null || confidence > currentBestConfidence)
        {
            currentBestObject = objectName;
            currentBestConfidence = confidence;

            AppLogger.Info($"New best detection: '{objectName}' ({confidence:F2})", CONTEXT);
        }

        // Queue backend request (will check cache and dedupe internally)
        if (backendManager != null)
        {
            backendManager.RequestSentences(objectName);
        }

        // Show overlay button only if:
        // 1. This is a different object than currently displayed
        // 2. The object has data available (has been processed by backend)
        if (uiManager != null)
        {
            bool shouldShowButton = false;
            
            // Check if this is a different object than currently displayed
            if (objectName != currentDisplayedObject)
            {
                // Check if object has data available (either in memory or cache)
                if (objectData.ContainsKey(objectName))
                {
                    shouldShowButton = true;
                    pendingOverlayObject = objectName;
                }
                else if (backendManager != null)
                {
                    // Check cache and store in memory if found
                    SentencesResponseDto cached = backendManager.GetCachedSentences(objectName);
                    if (cached != null)
                    {
                        objectData[objectName] = cached;
                        // Initialize carousel index if not exists
                        if (!carouselIndices.ContainsKey(objectName))
                        {
                            carouselIndices[objectName] = 0;
                        }
                        shouldShowButton = true;
                        pendingOverlayObject = objectName;
                    }
                }
            }
            
            if (!shouldShowButton)
            {
                pendingOverlayObject = null;
            }
            
            uiManager.ShowOverlayButton(shouldShowButton);
        }
    }

    /// <summary>
    /// Called when backend request completes successfully
    /// </summary>
    private void OnBackendRequestComplete(string objectName, string language, SentencesResponseDto response)
    {
        if (response == null || response.phrases == null || response.phrases.Length == 0)
        {
            AppLogger.Warning($"Received empty response for '{objectName}'", CONTEXT);
            return;
        }

        // Store the data
        objectData[objectName] = response;

        // Initialize carousel index if not exists
        if (!carouselIndices.ContainsKey(objectName))
        {
            carouselIndices[objectName] = 0;
        }

        AppLogger.Info($"Backend data received for '{objectName}': {response.phrases.Length} phrases", CONTEXT);

        // If this object is currently displayed, update UI
        if (objectName == currentDisplayedObject)
        {
            UpdateOverlayWithData(objectName, response);
        }
        
        // Check if we should show overlay button for this newly processed object
        if (uiManager != null && objectName != currentDisplayedObject)
        {
            // This object is not currently displayed and has data, show button
            pendingOverlayObject = objectName;
            uiManager.ShowOverlayButton(true);
        }
    }

    /// <summary>
    /// Called when backend request fails
    /// </summary>
    private void OnBackendRequestFailed(string objectName, string language, string error)
    {
        AppLogger.Error($"Backend request failed for '{objectName}': {error}", CONTEXT);

        // If this object is currently displayed, show error state
        if (objectName == currentDisplayedObject && uiManager != null)
        {
            uiManager.ShowLoadingState(objectName, false, error);
        }
    }

    /// <summary>
    /// Selects an object to display in the overlay (uses pending overlay object if null)
    /// </summary>
    public void SelectObjectForOverlay(string objectName = null)
    {
        // Use pending overlay object if no object specified
        if (string.IsNullOrEmpty(objectName))
        {
            objectName = pendingOverlayObject;
        }

        // If still null, try highest confidence as fallback
        if (string.IsNullOrEmpty(objectName))
        {
            objectName = currentBestObject;
        }

        if (string.IsNullOrEmpty(objectName))
        {
            AppLogger.Warning("No object selected for overlay display", CONTEXT);
            return;
        }

        currentDisplayedObject = objectName;
        pendingOverlayObject = null; // Clear pending since we're now displaying it

        AppLogger.Info($"Selecting object for overlay: '{objectName}'", CONTEXT);

        // Hide overlay button since we're now displaying this object
        if (uiManager != null)
        {
            uiManager.ShowOverlayButton(false);
        }

        // Check if we have data for this object
        if (objectData.ContainsKey(objectName))
        {
            // We have data, display it
            UpdateOverlayWithData(objectName, objectData[objectName]);
        }
        else
        {
            // Check cache
            if (backendManager != null)
            {
                SentencesResponseDto cached = backendManager.GetCachedSentences(objectName);
                if (cached != null)
                {
                    objectData[objectName] = cached;
                    if (!carouselIndices.ContainsKey(objectName))
                    {
                        carouselIndices[objectName] = 0;
                    }
                    UpdateOverlayWithData(objectName, cached);
                    return;
                }
            }

            // No data yet, show loading state
            if (uiManager != null)
            {
                uiManager.ShowLoadingState(objectName, true);
            }
        }
    }

    /// <summary>
    /// Updates the overlay UI with API response data
    /// </summary>
    public void UpdateOverlayWithData(string objectName, SentencesResponseDto data)
    {
        if (data == null || data.phrases == null || data.phrases.Length == 0)
        {
            AppLogger.Warning($"Cannot update overlay: invalid data for '{objectName}'", CONTEXT);
            return;
        }

        // Ensure carousel index exists and is valid
        if (!carouselIndices.ContainsKey(objectName))
        {
            carouselIndices[objectName] = 0;
        }

        int currentIndex = carouselIndices[objectName];
        if (currentIndex < 0 || currentIndex >= data.phrases.Length)
        {
            currentIndex = 0;
            carouselIndices[objectName] = 0;
        }

        // Get current phrase and translation
        string phrase = data.phrases[currentIndex];
        string translation = data.translations[currentIndex];

        // Update UI
        if (uiManager != null)
        {
            PhraseData currentPhrase = new PhraseData
            {
                objectName = objectName,
                phrase = phrase,
                translation = translation,
                index = currentIndex,
                totalPhrases = data.phrases.Length
            };

            uiManager.UpdatePhraseCarousel(
                currentPhrase.index,
                currentPhrase.totalPhrases,
                currentPhrase.objectName,
                currentPhrase.phrase,
                currentPhrase.translation,
                currentPhrase.translation
            );
            uiManager.AddHistoryEntry(currentPhrase);

            // Update carousel button states
            UpdateCarouselButtonStates(objectName, currentIndex, data.phrases.Length);
        }

        AppLogger.Info($"Overlay updated for '{objectName}' - phrase {currentIndex + 1}/{data.phrases.Length}", CONTEXT);
    }

    /// <summary>
    /// Shows the next phrase in the carousel
    /// </summary>
    public void ShowNextPhrase()
    {
        if (string.IsNullOrEmpty(currentDisplayedObject))
        {
            return;
        }

        if (!objectData.ContainsKey(currentDisplayedObject))
        {
            AppLogger.Warning($"No data available for '{currentDisplayedObject}'", CONTEXT);
            return;
        }

        SentencesResponseDto data = objectData[currentDisplayedObject];
        int currentIndex = carouselIndices.ContainsKey(currentDisplayedObject) 
            ? carouselIndices[currentDisplayedObject] 
            : 0;

        // Move to next (wrap around)
        currentIndex = (currentIndex + 1) % data.phrases.Length;
        carouselIndices[currentDisplayedObject] = currentIndex;

        UpdateOverlayWithData(currentDisplayedObject, data);

        AppLogger.Breadcrumb("Carousel next", "user_interaction", new Dictionary<string, string>
        {
            { "object", currentDisplayedObject },
            { "index", currentIndex.ToString() }
        });
    }

    /// <summary>
    /// Shows the previous phrase in the carousel
    /// </summary>
    public void ShowPreviousPhrase()
    {
        if (string.IsNullOrEmpty(currentDisplayedObject))
        {
            return;
        }

        if (!objectData.ContainsKey(currentDisplayedObject))
        {
            AppLogger.Warning($"No data available for '{currentDisplayedObject}'", CONTEXT);
            return;
        }

        SentencesResponseDto data = objectData[currentDisplayedObject];
        int currentIndex = carouselIndices.ContainsKey(currentDisplayedObject) 
            ? carouselIndices[currentDisplayedObject] 
            : 0;

        // Move to previous (wrap around)
        currentIndex = (currentIndex - 1 + data.phrases.Length) % data.phrases.Length;
        carouselIndices[currentDisplayedObject] = currentIndex;

        UpdateOverlayWithData(currentDisplayedObject, data);

        AppLogger.Breadcrumb("Carousel previous", "user_interaction", new Dictionary<string, string>
        {
            { "object", currentDisplayedObject },
            { "index", currentIndex.ToString() }
        });
    }

    /// <summary>
    /// Gets the current phrase data for an object
    /// </summary>
    public PhraseData GetCurrentPhraseData(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
        {
            objectName = currentDisplayedObject;
        }

        if (string.IsNullOrEmpty(objectName) || !objectData.ContainsKey(objectName))
        {
            return null;
        }

        SentencesResponseDto data = objectData[objectName];
        int currentIndex = carouselIndices.ContainsKey(objectName) ? carouselIndices[objectName] : 0;

        if (currentIndex < 0 || currentIndex >= data.phrases.Length)
        {
            return null;
        }

        return new PhraseData
        {
            objectName = objectName,
            phrase = data.phrases[currentIndex],
            translation = data.translations[currentIndex],
            index = currentIndex,
            totalPhrases = data.phrases.Length
        };
    }

    /// <summary>
    /// Gets the currently displayed object name
    /// </summary>
    public string GetCurrentObjectName()
    {
        return currentDisplayedObject;
    }

    /// <summary>
    /// Resets the highest confidence tracking (call when starting new detection cycle)
    /// </summary>
    public void ResetBestDetection()
    {
        currentBestObject = null;
        currentBestConfidence = 0f;
    }

    /// <summary>
    /// Updates the enabled/disabled state of carousel navigation buttons
    /// </summary>
    private void UpdateCarouselButtonStates(string objectName, int currentIndex, int totalPhrases)
    {
        if (uiManager == null)
        {
            return;
        }

        // Buttons should be enabled if there's more than 1 phrase
        // Since carousel wraps around, both buttons are always enabled when totalPhrases > 1
        bool buttonsEnabled = totalPhrases > 1;
        
        uiManager.SetCarouselButtonsEnabled(buttonsEnabled);
    }
}

/// <summary>
/// Helper class to hold current phrase data
/// </summary>
public class PhraseData
{
    public string objectName;
    public string phrase;
    public string translation;
    public int index;
    public int totalPhrases;
}

