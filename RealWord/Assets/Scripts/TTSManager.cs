using UnityEngine;
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
}

