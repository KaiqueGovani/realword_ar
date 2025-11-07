using System.Collections.Generic;
using System.Linq;
using Sentry;
using Sentry.Unity;
using TMPro;
using UnityEngine;

public class TTSButton : MonoBehaviour
{
    [Header("Referências")]
    public AndroidTTS tts;
    public TMP_InputField inputField;
    public TMP_Dropdown languageDropdown;
    public TextMeshProUGUI languageIndicator; // Texto que mostra o idioma atual

    [Header("Configuração")]
    [TextArea(3, 10)]
    public string textoFallback = "Olá, este é um teste de voz!";
    public float pitch = 1f;
    public float velocidade = 1f;
    public bool limparFila = true;

    private List<string> languageKeys;

    void Start()
    {
        ConfigurarDropdown();
        AtualizarIndicador();
    }

    void ConfigurarDropdown()
    {
        if (languageDropdown == null)
        {
            Debug.LogWarning("⚠️ Dropdown não configurado!");
            return;
        }

        // Limpa as opções existentes
        languageDropdown.ClearOptions();

        // Cria a lista de opções
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        languageKeys = new List<string>();

        foreach (var lang in AndroidTTS.AvailableLanguages)
        {
            options.Add(new TMP_Dropdown.OptionData(lang.Value.DisplayName));
            languageKeys.Add(lang.Key);
        }

        // Adiciona as opções ao dropdown
        languageDropdown.AddOptions(options);

        // Define o valor inicial (Português Brasil = index 0)
        languageDropdown.value = 0;

        // Adiciona listener para quando o valor mudar
        languageDropdown.onValueChanged.AddListener(OnLanguageChanged);

        Debug.Log($"✅ Dropdown configurado com {options.Count} idiomas");
    }

    void OnLanguageChanged(int index)
    {
        if (index < 0 || index >= languageKeys.Count)
        {
            Debug.LogError($"❌ Índice inválido: {index}");
            return;
        }

        string selectedLanguage = languageKeys[index];
        if (tts != null)
        {
            tts.SetLanguage(selectedLanguage);
            AtualizarIndicador();
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
        SentrySdk.CaptureMessage("[INFO] Botão 'Falar' clicado");
        Debug.Log("===== BOTÃO CLICADO =====");

        if (tts == null)
        {
            SentrySdk.CaptureMessage("[ERROR] TTS não configurado!");
            Debug.LogError("TTS não configurado!");
            return;
        }

        string textoParaFalar = textoFallback;

        if (inputField != null && !string.IsNullOrEmpty(inputField.text))
        {
            textoParaFalar = inputField.text;
            SentrySdk.CaptureMessage($"[INFO] Texto do InputField: {textoParaFalar}");
            Debug.Log($"Texto do InputField: {textoParaFalar}");
        }
        else
        {
            SentrySdk.CaptureMessage($"[INFO] Usando texto padrão: {textoParaFalar}");
            Debug.Log($"Usando texto padrão: {textoParaFalar}");
        }

        if (string.IsNullOrEmpty(textoParaFalar))
        {
            SentrySdk.CaptureMessage("[WARN] Texto vazio!");
            Debug.LogWarning("Texto vazio!");
            return;
        }

        SentrySdk.CaptureMessage($"[INFO] Falando: {textoParaFalar}");
        Debug.Log($"Falando: {textoParaFalar}");
        tts.Speak(textoParaFalar, limparFila, pitch, velocidade);
    }

    public void Parar()
    {
        if (tts != null)
        {
            tts.Stop();
        }
    }

    public void LimparCampo()
    {
        if (inputField != null)
        {
            inputField.text = "";
        }
    }
}
