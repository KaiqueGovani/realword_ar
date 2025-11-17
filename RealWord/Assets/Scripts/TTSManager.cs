using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages TTS playback for detected phrases.
/// Coordinates with DetectionResultManager to play current phrases.
/// </summary>
public class TTSManager : MonoBehaviour
{
    private const string CONTEXT = "TTSManager";

    [Header("Dependencies")]
    public AndroidTTS androidTTS;
    public DetectionResultManager detectionResultManager;

    [Header("TTS Settings")]
    [Tooltip("Play translation instead of original phrase")]
    public bool playTranslation = false;

    [Tooltip("Pitch for TTS (1.0 = normal)")]
    [Range(0.5f, 2.0f)]
    public float pitch = 1.0f;

    [Tooltip("Speech rate for TTS (1.0 = normal)")]
    [Range(0.5f, 2.0f)]
    public float speechRate = 1.0f;

    [Header("Dual Language Settings")]
    [Tooltip("Delay between playing translation and original (in seconds)")]
    [Range(0.1f, 3.0f)]
    public float delayBetweenLanguages = 0.3f;

    [Header("Language Settings")]
    [Tooltip("Language code used when playing the translated phrase (user language).")]
    public string translationLanguageCode = "";

    [Tooltip("Language code used when playing the original English phrase.")]
    public string englishLanguageCode = "en_US";

    private Coroutine currentPlaybackCoroutine;

    void Start()
    {
        // Find AndroidTTS if not assigned
        if (androidTTS == null)
        {
            androidTTS = FindObjectOfType<AndroidTTS>();
            if (androidTTS == null)
            {
                AppLogger.Warning("AndroidTTS not found. TTS playback will not work.", CONTEXT);
            }
        }

        // Find DetectionResultManager if not assigned
        if (detectionResultManager == null)
        {
            detectionResultManager = FindObjectOfType<DetectionResultManager>();
            if (detectionResultManager == null)
            {
                AppLogger.Warning("DetectionResultManager not found. Cannot get phrase data.", CONTEXT);
            }
        }

        // Use the current TTS language as default translation language if not configured
        if (string.IsNullOrEmpty(translationLanguageCode) && androidTTS != null)
        {
            translationLanguageCode = androidTTS.CurrentLanguageCode;
        }

        if (string.IsNullOrEmpty(englishLanguageCode))
        {
            englishLanguageCode = "en_US";
        }
    }


    /// <summary>
    /// Plays the current phrase for an object.
    /// </summary>
    /// <param name="objectName">Name of the object (if null, uses currently displayed object)</param>
    /// <param name="playTranslation">If true, plays translation; if false, plays original phrase</param>
    public void PlayCurrentPhrase(string objectName = null, bool? playTranslationOverride = null)
    {
        if (androidTTS == null)
        {
            AppLogger.Warning("AndroidTTS not available. Cannot play audio.", CONTEXT);
            return;
        }

        if (detectionResultManager == null)
        {
            AppLogger.Warning("DetectionResultManager not available. Cannot get phrase data.", CONTEXT);
            return;
        }

        // Get current phrase data
        PhraseData phraseData = detectionResultManager.GetCurrentPhraseData(objectName);
        if (phraseData == null)
        {
            AppLogger.Warning($"No phrase data available for object '{objectName ?? "current"}'", CONTEXT);
            return;
        }

        // Determine which text to play
        bool shouldPlayTranslation = playTranslationOverride ?? playTranslation;
        string textToPlay = shouldPlayTranslation ? phraseData.translation : phraseData.phrase;

        if (string.IsNullOrEmpty(textToPlay))
        {
            AppLogger.Warning($"Text to play is empty for object '{phraseData.objectName}'", CONTEXT);
            return;
        }

        string languageCode = shouldPlayTranslation
            ? GetTranslationLanguageCode()
            : GetEnglishLanguageCode();

        if (!TrySetLanguage(languageCode, shouldPlayTranslation ? "Set translation language" : "Set English language", phraseData.objectName))
        {
            return;
        }

        // Play the text
        try
        {
            androidTTS.Speak(textToPlay, true, pitch, speechRate); // flush = true to stop any current playback

            AppLogger.Info($"Playing TTS for '{phraseData.objectName}' (phrase {phraseData.index + 1}/{phraseData.totalPhrases}, translation: {shouldPlayTranslation}, language: {languageCode}): '{textToPlay.Substring(0, System.Math.Min(50, textToPlay.Length))}...'", CONTEXT);

            AppLogger.Breadcrumb("TTS playback started", "tts_action", new Dictionary<string, string>
            {
                { "object", phraseData.objectName },
                { "text", textToPlay.Substring(0, System.Math.Min(100, textToPlay.Length)) },
                { "language", languageCode }
            });
        }
        catch (System.Exception e)
        {
            AppLogger.Exception(e, CONTEXT, new Dictionary<string, object>
            {
                { "object", phraseData.objectName },
                { "text", textToPlay },
                { "language", languageCode }
            });
        }
    }

    /// <summary>
    /// Stops current TTS playback
    /// </summary>
    public void StopPlayback()
    {
        // Stop any ongoing dual language playback coroutine
        if (currentPlaybackCoroutine != null)
        {
            StopCoroutine(currentPlaybackCoroutine);
            currentPlaybackCoroutine = null;
        }

        if (androidTTS != null)
        {
            androidTTS.Stop();
            AppLogger.Info("TTS playback stopped", CONTEXT);
            AppLogger.Breadcrumb("TTS playback stopped", "tts_action");
        }
    }

    /// <summary>
    /// Plays the original phrase (in English only)
    /// </summary>
    public void PlayOriginalPhrase(string objectName = null)
    {
        PlayCurrentPhrase(objectName, false);
    }

    /// <summary>
    /// Plays the translated phrase using the configured user language.
    /// </summary>
    public void PlayTranslation(string objectName = null)
    {
        PlayCurrentPhrase(objectName, true);
    }

    /// <summary>
    /// Convenience helper to play only the original English phrase once.
    /// </summary>
    public void PlayEnglishOnly(string objectName = null)
    {
        PlayCurrentPhrase(objectName, false);
    }

    /// <summary>
    /// Plays both the translation and original phrase sequentially.
    /// Translation is played in the target language first, then original in English.
    /// </summary>
    /// <param name="objectName">Name of the object (if null, uses currently displayed object)</param>
    public void PlayBothLanguages(string objectName = null)
    {
        // Stop any ongoing playback
        StopPlayback();

        // Start the coroutine to play both languages
        currentPlaybackCoroutine = StartCoroutine(PlayBothLanguagesCoroutine(objectName, null));
    }

    /// <summary>
    /// Plays a dual-language sequence for a specific history entry.
    /// </summary>
    /// <param name="phraseData">Phrase data stored in history.</param>
    public void PlayHistoryEntry(PhraseData phraseData)
    {
        if (phraseData == null)
        {
            AppLogger.Warning("History entry is empty. Cannot play audio.", CONTEXT);
            return;
        }

        if (androidTTS == null)
        {
            AppLogger.Warning("AndroidTTS not available. Cannot play audio.", CONTEXT);
            return;
        }

        // Stop any ongoing playback before starting the history entry
        StopPlayback();
        currentPlaybackCoroutine = StartCoroutine(PlayBothLanguagesCoroutine(null, phraseData));
    }

    /// <summary>
    /// Coroutine that plays translation in target language first, then original phrase in English
    /// </summary>
    private IEnumerator PlayBothLanguagesCoroutine(string objectName, PhraseData overridePhraseData)
    {
        if (androidTTS == null)
        {
            AppLogger.Warning("AndroidTTS not available. Cannot play audio.", CONTEXT);
            yield break;
        }

        PhraseData phraseData = overridePhraseData;

        if (phraseData == null)
        {
            if (detectionResultManager == null)
            {
                AppLogger.Warning("DetectionResultManager not available. Cannot get phrase data.", CONTEXT);
                yield break;
            }

            // Get current phrase data
            phraseData = detectionResultManager.GetCurrentPhraseData(objectName);
            if (phraseData == null)
            {
                AppLogger.Warning($"No phrase data available for object '{objectName ?? "current"}'", CONTEXT);
                yield break;
            }
        }

        // Validate both texts are available
        if (string.IsNullOrEmpty(phraseData.phrase))
        {
            AppLogger.Warning($"Original phrase is empty for object '{phraseData.objectName}'", CONTEXT);
            yield break;
        }

        if (string.IsNullOrEmpty(phraseData.translation))
        {
            AppLogger.Warning($"Translation is empty for object '{phraseData.objectName}'", CONTEXT);
            yield break;
        }

        string translationLanguage = GetTranslationLanguageCode();
        string englishLanguage = GetEnglishLanguageCode();

        if (!TrySetLanguage(translationLanguage, "Set translation language for dual playback", phraseData.objectName))
        {
            yield break;
        }

        yield return new WaitForSeconds(0.1f); // Small delay for language switch

        bool translationFailed = false;
        try
        {
            androidTTS.Speak(phraseData.translation, true, pitch, speechRate);
        }
        catch (System.Exception e)
        {
            AppLogger.Exception(e, CONTEXT, new Dictionary<string, object>
            {
                { "action", "Speak translation" },
                { "object", phraseData.objectName }
            });
            translationFailed = true;
        }

        if (!translationFailed)
        {
            // Estimate speech duration (rough approximation: ~150 words per minute)
            float estimatedDuration = EstimateSpeechDuration(phraseData.translation, speechRate);
            yield return new WaitForSeconds(estimatedDuration);

            // Add delay between languages
            yield return new WaitForSeconds(delayBetweenLanguages);

            if (TrySetLanguage(englishLanguage, "Set English language for dual playback", phraseData.objectName))
            {
                yield return new WaitForSeconds(0.1f); // Small delay for language switch
                try
                {
                    androidTTS.Speak(phraseData.phrase, true, pitch, speechRate);

                    AppLogger.Breadcrumb("Dual language TTS playback completed", "tts_action", new Dictionary<string, string>
                    {
                        { "object", phraseData.objectName },
                        { "translationText", phraseData.translation.Substring(0, System.Math.Min(50, phraseData.translation.Length)) },
                        { "originalText", phraseData.phrase.Substring(0, System.Math.Min(50, phraseData.phrase.Length)) },
                        { "translationLanguage", translationLanguage },
                        { "englishLanguage", englishLanguage }
                    });
                }
                catch (System.Exception e)
                {
                    AppLogger.Exception(e, CONTEXT, new Dictionary<string, object>
                    {
                        { "action", "Speak original (English) phrase" },
                        { "object", phraseData.objectName }
                    });
                }
            }
        }

        // Restore translation language so subsequent playback continues in the user-selected locale
        TrySetLanguage(translationLanguage, "Restore translation language after dual playback", phraseData.objectName);

        currentPlaybackCoroutine = null;
    }

    /// <summary>
    /// Estimates the duration of speech based on text length and speech rate
    /// </summary>
    /// <param name="text">Text to be spoken</param>
    /// <param name="rate">Speech rate multiplier</param>
    /// <returns>Estimated duration in seconds</returns>
    private float EstimateSpeechDuration(string text, float rate)
    {
        if (string.IsNullOrEmpty(text))
            return 0f;

        // Average speaking rate is about 150 words per minute (2.5 words per second)
        // We'll use character count as a simpler approximation
        // Average word length is ~5 characters, so ~12.5 characters per second at normal rate
        float charactersPerSecond = 12.5f * rate;
        float estimatedSeconds = text.Length / charactersPerSecond;
        
        // Add a small buffer for safety (10%)
        return estimatedSeconds * 1.1f;
    }

    private string GetTranslationLanguageCode()
    {
        if (!string.IsNullOrEmpty(translationLanguageCode))
        {
            return translationLanguageCode;
        }

        if (androidTTS != null && !string.IsNullOrEmpty(androidTTS.CurrentLanguageCode))
        {
            return androidTTS.CurrentLanguageCode;
        }

        return "pt_BR";
    }

    private string GetEnglishLanguageCode()
    {
        if (!string.IsNullOrEmpty(englishLanguageCode))
        {
            return englishLanguageCode;
        }

        return "en_US";
    }

    private bool TrySetLanguage(string languageCode, string actionDescription, string objectName = null)
    {
        if (androidTTS == null)
        {
            AppLogger.Warning($"AndroidTTS not available. Cannot {actionDescription}.", CONTEXT);
            return false;
        }

        if (string.IsNullOrEmpty(languageCode))
        {
            AppLogger.Warning($"Language code missing for action '{actionDescription}'.", CONTEXT);
            return false;
        }

        try
        {
            androidTTS.SetLanguage(languageCode);
            return true;
        }
        catch (System.Exception e)
        {
            AppLogger.Exception(e, CONTEXT, new Dictionary<string, object>
            {
                { "action", actionDescription },
                { "language", languageCode },
                { "object", objectName ?? "current" }
            });
            return false;
        }
    }
}

