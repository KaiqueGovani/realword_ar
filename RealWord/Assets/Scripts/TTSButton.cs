using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class TTSButton : MonoBehaviour
{
    [Header("Referências")]
    public AndroidTTS tts;
    public TMP_InputField inputField;
    public TMP_Dropdown languageDropdown;
    public TextMeshProUGUI languageIndicator;

    [Header("Configuração")]
    [TextArea(3, 10)]
    public string textoFallback = "Olá, este é um teste de voz!";
    public float pitch = 1f;
    public float velocidade = 1f;
    public bool limparFila = true;

    private List<string> languageKeys;
    private const string CONTEXT = "TTSButton";

    void Start()
    {
        ConfigurarDropdown();
        AtualizarIndicador();

        AppLogger.Info("TTSButton inicializado", CONTEXT);
    }

    void ConfigurarDropdown()
    {
        if (languageDropdown == null)
        {
            AppLogger.Warning("Dropdown não configurado", CONTEXT);
            return;
        }

        languageDropdown.ClearOptions();

        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        languageKeys = new List<string>();

        foreach (var lang in AndroidTTS.AvailableLanguages)
        {
            options.Add(new TMP_Dropdown.OptionData(lang.Value.DisplayName));
            languageKeys.Add(lang.Key);
        }

        languageDropdown.AddOptions(options);
        languageDropdown.value = 0;
        languageDropdown.onValueChanged.AddListener(OnLanguageChanged);

        AppLogger.Info($"Dropdown configurado com {options.Count} idiomas", CONTEXT);
    }

    void OnLanguageChanged(int index)
    {
        if (index < 0 || index >= languageKeys.Count)
        {
            AppLogger.Error($"Índice de idioma inválido: {index}", CONTEXT, new Dictionary<string, object>
            {
                { "index", index },
                { "maxIndex", languageKeys.Count - 1 }
            });
            return;
        }

        string selectedLanguage = languageKeys[index];
        if (tts != null)
        {
            tts.SetLanguage(selectedLanguage);
            AtualizarIndicador();

            AppLogger.Breadcrumb($"Idioma alterado para {selectedLanguage}", "user_interaction");
        }
    }

    void AtualizarIndicador()
    {
        if (languageIndicator == null || tts == null)
            return;

        string currentLang = tts.CurrentLanguageCode;
        if (AndroidTTS.AvailableLanguages.ContainsKey(currentLang))
        {
            languageIndicator.text = $"Idioma atual: {AndroidTTS.AvailableLanguages[currentLang].DisplayName}";
        }
    }

    public void Falar()
    {
        AppLogger.Breadcrumb("Botão Falar clicado", "user_interaction");

        if (tts == null)
        {
            AppLogger.Error("TTS não configurado", CONTEXT);
            return;
        }

        string textoParaFalar = textoFallback;

        if (inputField != null && !string.IsNullOrEmpty(inputField.text))
        {
            textoParaFalar = inputField.text;
            AppLogger.Info($"Usando texto do InputField: {textoParaFalar}", CONTEXT);
        }
        else
        {
            AppLogger.Info($"Usando texto padrão: {textoParaFalar}", CONTEXT);
        }

        if (string.IsNullOrEmpty(textoParaFalar))
        {
            AppLogger.Warning("Tentativa de falar com texto vazio", CONTEXT);
            return;
        }

        try
        {
            tts.Speak(textoParaFalar, limparFila, pitch, velocidade);

            AppLogger.Breadcrumb($"TTS executado: {textoParaFalar.Substring(0, System.Math.Min(50, textoParaFalar.Length))}...",
                "tts_action",
                new Dictionary<string, string>
                {
                    { "language", tts.CurrentLanguageCode },
                    { "textLength", textoParaFalar.Length.ToString() }
                }
            );
        }
        catch (System.Exception ex)
        {
            AppLogger.Exception(ex, CONTEXT, new Dictionary<string, object>
            {
                { "texto", textoParaFalar },
                { "idioma", tts.CurrentLanguageCode }
            });
        }
    }

    public void Parar()
    {
        if (tts != null)
        {
            tts.Stop();
            AppLogger.Breadcrumb("TTS parado", "user_interaction");
        }
    }

    public void LimparCampo()
    {
        if (inputField != null)
        {
            inputField.text = "";
            AppLogger.Info("Campo de texto limpo", CONTEXT);
        }
    }
}