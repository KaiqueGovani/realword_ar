using UnityEngine;
using System.Collections.Generic;

public class AndroidTTS : MonoBehaviour
{
    AndroidJavaObject activity;
    AndroidJavaObject tts;
    bool ready;

    // Idioma atual
    private string currentLanguageCode = "pt_BR";
    public string CurrentLanguageCode => currentLanguageCode;

    // Dicionário com os idiomas disponíveis
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
                Debug.Log($"✅ TTS inicializado com idioma: {currentLanguageCode}");
            }
            else
            {
                Debug.LogWarning("❌ TTS init failed");
            }
        });

        tts = new AndroidJavaObject("android.speech.tts.TextToSpeech", activity, listener);
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
            Debug.LogError($"❌ Idioma não reconhecido: {languageKey}");
            return;
        }

        currentLanguageCode = languageKey;
        LanguageInfo langInfo = AvailableLanguages[languageKey];

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!ready || tts == null)
        {
            Debug.LogWarning("⚠️ TTS não está pronto!");
            return;
        }

        AndroidJavaObject locale = new AndroidJavaObject("java.util.Locale", langInfo.LanguageCode, langInfo.CountryCode);
        int result = tts.Call<int>("setLanguage", locale);

        if (result >= 0)
        {
            Debug.Log($"✅ Idioma alterado para: {langInfo.DisplayName}");
        }
        else if (result == -1)
        {
            Debug.LogError($"❌ Dados do idioma {langInfo.DisplayName} não disponíveis! Instale o pacote de idioma.");
        }
        else if (result == -2)
        {
            Debug.LogError($"❌ Idioma {langInfo.DisplayName} não é suportado!");
        }

        locale?.Dispose();
#else
        Debug.Log($"🎤 [SIMULAÇÃO] Idioma alterado para: {langInfo.DisplayName}");
#endif
    }

    public void Speak(string text, bool flush = true, float pitch = 1f, float rate = 1f)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!ready || tts == null)
        {
            Debug.LogWarning("⚠️ TTS not ready");
            return;
        }

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

        Debug.Log($"🔊 Falando em {currentLanguageCode}: '{text}'");
#else
        Debug.Log($"🎤 [SIMULAÇÃO] Falaria em {currentLanguageCode}: '{text}'");
#endif
    }

    public void Stop()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        tts?.Call<int>("stop");
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
        }
        catch { }
#endif
    }
}
