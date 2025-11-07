using UnityEngine;
using System.Collections.Generic;

public class AndroidTTS : MonoBehaviour
{
    AndroidJavaObject activity;
    AndroidJavaObject tts;
    bool ready;

    private string currentLanguageCode = "pt_BR";
    public string CurrentLanguageCode => currentLanguageCode;

    private const string CONTEXT = "AndroidTTS";

    public static Dictionary<string, LanguageInfo> AvailableLanguages = new Dictionary<string, LanguageInfo>()
    {
        { "pt_BR", new LanguageInfo("Português (Brasil)", "pt", "BR") },
        { "en_US", new LanguageInfo("English (US)", "en", "US") },
        { "es_ES", new LanguageInfo("Español (España)", "es", "ES") },
        { "zh_CN", new LanguageInfo("Chinese (China)", "zh", "CN") },
        { "hi_IN", new LanguageInfo("Hindi (India)", "hi", "IN") },
        { "ar_SA", new LanguageInfo("Arabic (Saudi Arabia)", "ar", "SA") },
        { "fr_FR", new LanguageInfo("Français (France)", "fr", "FR") },
        { "ru_RU", new LanguageInfo("Russian (Russia)", "ru", "RU") },
        { "de_DE", new LanguageInfo("Deutsch (Deutschland)", "de", "DE") },
        { "ja_JP", new LanguageInfo("Japanese (Japan)", "ja", "JP") }
    };

    public class LanguageInfo
    {
        public string DisplayName;
        public string LanguageCode;
        public string CountryCode;

        public LanguageInfo(string displayName, string languageCode, string countryCode)
        {
            DisplayName = displayName;
            LanguageCode = languageCode;
            CountryCode = countryCode;
        }
    }

    class OnInitListener : AndroidJavaProxy
    {
        readonly System.Action<int> _onInit;

        public OnInitListener(System.Action<int> onInit)
            : base("android.speech.tts.TextToSpeech$OnInitListener") =>
            this._onInit = onInit;

        void onInit(int status) => _onInit?.Invoke(status);
    }

    void Awake()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            var listener = new OnInitListener(status =>
            {
                ready = (status == 0);
                if (ready)
                {
                    SetLanguage(currentLanguageCode);
                    tts.Call<int>("setPitch", 1.0f);
                    tts.Call<int>("setSpeechRate", 1.0f);
                    
                    AppLogger.Info($"TTS inicializado com sucesso - Idioma: {currentLanguageCode}", CONTEXT);
                    AppLogger.Breadcrumb("TTS inicializado", "system", new Dictionary<string, string>
                    {
                        { "language", currentLanguageCode }
                    });
                }
                else
                {
                    AppLogger.Error($"Falha ao inicializar TTS - Status: {status}", CONTEXT, new Dictionary<string, object>
                    {
                        { "status", status }
                    });
                }
            });

            tts = new AndroidJavaObject("android.speech.tts.TextToSpeech", activity, listener);
        }
        catch (System.Exception ex)
        {
            AppLogger.Exception(ex, CONTEXT);
        }
#else
        AppLogger.Info("TTS em modo simulação (Editor)", CONTEXT);
        ready = true;
#endif
    }

    /// <summary>
    /// Define o idioma do TTS
    /// </summary>
    /// <param name="languageKey">Chave do idioma (ex: "pt_BR", "en_US")</param>
    public void SetLanguage(string languageKey)
    {
        if (!AvailableLanguages.ContainsKey(languageKey))
        {
            AppLogger.Error($"Idioma não reconhecido: {languageKey}", CONTEXT, new Dictionary<string, object>
            {
                { "requestedLanguage", languageKey },
                { "availableLanguages", string.Join(", ", AvailableLanguages.Keys) }
            });
            return;
        }

        currentLanguageCode = languageKey;
        LanguageInfo langInfo = AvailableLanguages[languageKey];

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!ready || tts == null)
        {
            AppLogger.Warning("TTS não está pronto para trocar idioma", CONTEXT);
            return;
        }

        try
        {
            AndroidJavaObject locale = new AndroidJavaObject("java.util.Locale", langInfo.LanguageCode, langInfo.CountryCode);
            int result = tts.Call<int>("setLanguage", locale);

            if (result >= 0)
            {
                AppLogger.Info($"Idioma alterado para: {langInfo.DisplayName}", CONTEXT);
            }
            else if (result == -1)
            {
                AppLogger.Error($"Dados do idioma {langInfo.DisplayName} não disponíveis", CONTEXT, new Dictionary<string, object>
                {
                    { "language", languageKey },
                    { "errorCode", result },
                    { "suggestion", "Instale o pacote de idioma no dispositivo" }
                });
            }
            else if (result == -2)
            {
                AppLogger.Error($"Idioma {langInfo.DisplayName} não é suportado pelo dispositivo", CONTEXT, new Dictionary<string, object>
                {
                    { "language", languageKey },
                    { "errorCode", result }
                });
            }

            locale?.Dispose();
        }
        catch (System.Exception ex)
        {
            AppLogger.Exception(ex, CONTEXT, new Dictionary<string, object>
            {
                { "targetLanguage", languageKey }
            });
        }
#else
        AppLogger.Info($"[SIMULAÇÃO] Idioma alterado para: {langInfo.DisplayName}", CONTEXT);
#endif
    }

    public void Speak(string text, bool flush = true, float pitch = 1f, float rate = 1f)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!ready || tts == null)
        {
            AppLogger.Warning("TTS não está pronto para falar", CONTEXT, new Dictionary<string, object>
            {
                { "ready", ready },
                { "ttsNull", tts == null }
            });
            return;
        }

        try
        {
            tts.Call<int>("setPitch", pitch);
            tts.Call<int>("setSpeechRate", rate);

            int sdk = new AndroidJavaClass("android.os.Build$VERSION").GetStatic<int>("SDK_INT");
            int queueMode = new AndroidJavaClass("android.speech.tts.TextToSpeech")
                .GetStatic<int>(flush ? "QUEUE_FLUSH" : "QUEUE_ADD");

            if (sdk >= 21)
            {
                using (var bundle = new AndroidJavaObject("android.os.Bundle"))
                {
                    tts.Call<int>("speak", text, queueMode, bundle, System.Guid.NewGuid().ToString());
                }
            }
            else
            {
                tts.Call<int>("speak", text, queueMode, null);
            }

            AppLogger.Info($"TTS falando em {currentLanguageCode}", CONTEXT);
        }
        catch (System.Exception ex)
        {
            AppLogger.Exception(ex, CONTEXT, new Dictionary<string, object>
            {
                { "text", text },
                { "language", currentLanguageCode },
                { "pitch", pitch },
                { "rate", rate }
            });
        }
#else
        AppLogger.Info($"[SIMULAÇÃO] Falaria em {currentLanguageCode}: '{text}'", CONTEXT);
#endif
    }

    public void Stop()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            tts?.Call<int>("stop");
            AppLogger.Info("TTS parado", CONTEXT);
        }
        catch (System.Exception ex)
        {
            AppLogger.Exception(ex, CONTEXT);
        }
#endif
    }

    void OnDestroy()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            tts?.Call<int>("stop");
            tts?.Call<int>("shutdown");
            tts?.Dispose();
            activity?.Dispose();
            
            AppLogger.Info("TTS destruído e recursos liberados", CONTEXT);
        }
        catch (System.Exception ex)
        {
            AppLogger.Exception(ex, CONTEXT);
        }
#endif
    }
}