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

        // Determine language for TTS
        // If playing translation, use English (or target language)
        // If playing original, use Portuguese (or source language)
        string targetLanguage = androidTTS.CurrentLanguageCode;

        // For now, we'll use the current TTS language setting
        // In the future, this could be smarter based on which language we're playing

        // Play the text
        try
        {
            androidTTS.Speak(textToPlay, true, pitch, speechRate); // flush = true to stop any current playback

            AppLogger.Info($"Playing TTS for '{phraseData.objectName}' (phrase {phraseData.index + 1}/{phraseData.totalPhrases}, translation: {shouldPlayTranslation}, language: {targetLanguage}): '{textToPlay.Substring(0, System.Math.Min(50, textToPlay.Length))}...'", CONTEXT);

            AppLogger.Breadcrumb("TTS playback started", "tts_action", new Dictionary<string, string>
            {
                { "object", phraseData.objectName },
                { "text", textToPlay.Substring(0, System.Math.Min(100, textToPlay.Length)) },
                { "language", targetLanguage }
            });
        }
        catch (System.Exception e)
        {
            AppLogger.Exception(e, CONTEXT, new Dictionary<string, object>
            {
                { "object", phraseData.objectName },
                { "text", textToPlay },
                { "language", targetLanguage }
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
    /// Plays the original phrase (in Portuguese)
    /// </summary>
    public void PlayOriginalPhrase(string objectName = null)
    {
        PlayCurrentPhrase(objectName, false);
    }

    /// <summary>
    /// Plays the translation (in English)
    /// </summary>
    public void PlayTranslation(string objectName = null)
    {
        PlayCurrentPhrase(objectName, true);
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
        currentPlaybackCoroutine = StartCoroutine(PlayBothLanguagesCoroutine(objectName));
    }

    /// <summary>
    /// Coroutine that plays translation in target language first, then original phrase in English
    /// </summary>
    private IEnumerator PlayBothLanguagesCoroutine(string objectName)
    {
        if (androidTTS == null)
        {
            AppLogger.Warning("AndroidTTS not available. Cannot play audio.", CONTEXT);
            yield break;
        }

        if (detectionResultManager == null)
        {
            AppLogger.Warning("DetectionResultManager not available. Cannot get phrase data.", CONTEXT);
            yield break;
        }

        // Get current phrase data
        PhraseData phraseData = detectionResultManager.GetCurrentPhraseData(objectName);
        if (phraseData == null)
        {
            AppLogger.Warning($"No phrase data available for object '{objectName ?? "current"}'", CONTEXT);
            yield break;
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

        // Store the current language to restore it later
        string originalLanguage = androidTTS.CurrentLanguageCode;
        bool hasError = false;

        // STEP 1: Play translation in target language
        AppLogger.Info($"Playing translation in {originalLanguage} for '{phraseData.objectName}'", CONTEXT);
        
        // Make sure we're in the correct language (should already be, but just in case)
        try
        {
            androidTTS.SetLanguage(originalLanguage);
        }
        catch (System.Exception e)
        {
            AppLogger.Exception(e, CONTEXT, new Dictionary<string, object>
            {
                { "action", "SetLanguage to target language" },
                { "object", phraseData.objectName },
                { "targetLanguage", originalLanguage }
            });
            hasError = true;
        }

        if (!hasError)
        {
            yield return new WaitForSeconds(0.1f); // Small delay for language switch
            
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
                hasError = true;
            }

            if (!hasError)
            {
                // Estimate speech duration (rough approximation: ~150 words per minute)
                float estimatedDuration = EstimateSpeechDuration(phraseData.translation, speechRate);
                yield return new WaitForSeconds(estimatedDuration);

                // Add delay between languages
                yield return new WaitForSeconds(delayBetweenLanguages);

                // STEP 2: Play original phrase in English
                AppLogger.Info($"Playing original phrase in English for '{phraseData.objectName}'", CONTEXT);
                
                try
                {
                    androidTTS.SetLanguage("en_US");
                }
                catch (System.Exception e)
                {
                    AppLogger.Exception(e, CONTEXT, new Dictionary<string, object>
                    {
                        { "action", "SetLanguage to en_US" },
                        { "object", phraseData.objectName }
                    });
                    hasError = true;
                }

                if (!hasError)
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
                            { "targetLanguage", originalLanguage }
                        });
                    }
                    catch (System.Exception e)
                    {
                        AppLogger.Exception(e, CONTEXT, new Dictionary<string, object>
                        {
                            { "action", "Speak original phrase" },
                            { "object", phraseData.objectName }
                        });
                        hasError = true;
                    }
                }
            }
        }

        // Restore language if error occurred
        if (hasError)
        {
            try
            {
                androidTTS.SetLanguage(originalLanguage);
            }
            catch (System.Exception e)
            {
                AppLogger.Exception(e, CONTEXT, new Dictionary<string, object>
                {
                    { "action", "Restore original language after error" },
                    { "targetLanguage", originalLanguage }
                });
            }
        }

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
}

