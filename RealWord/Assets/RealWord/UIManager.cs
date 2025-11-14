using UnityEngine;
using UnityEngine.UI; // Necessário para UI (Button, Image, etc.)
using TMPro;          // Necessário para TextMeshPro
using System.Collections; // Necessário para Coroutines (esconder snackbars)

/// <summary>
/// Gerencia toda a interface gráfica (UI) do aplicativo RealWord.
/// Controla a troca de painéis, preenche textos e gerencia estados de UI.
/// </summary>
public class UIManager : MonoBehaviour
{
    // --- PAINÉIS PRINCIPAIS (As "Telas") ---
    [Header("Painéis de Tela")]
    public GameObject welcomePanel; // A "Tela Inicial"
    public GameObject mainPanel;    // A "Tela Principal" (com a câmera)
    public GameObject menuPanel;    // A tela de Menu
    public GameObject settingsPanel; // A tela de Configurações
    public GameObject historyPanel;  // A tela de Histórico

    // --- BOTÕES ---
    [Header("Botões Principais")]
    public Button startButton;      // Botão "Começar" da tela inicial

    // Botões do MainPanel (que você criou no topo)
    public Button mainHistoryButton;
    public Button mainSettingsButton;

    // Botões dos Painéis de Menu
    // (Adicione referências para os botões "Voltar", "Tutorial", etc. se precisar)
    // Ex: public Button menuBackButton;
    // Ex: public Button settingsTutorialButton;


    // --- GRUPO DE DETECÇÃO (Começa desativado) ---
    [Header("Grupo de Detecção (Overlay)")]
    public GameObject detectionOverlayGroup; // O "pai" de toda a UI de detecção

    // --- ELEMENTOS DA TAG FLUTUANTE ---
    [Header("Tag Flutuante (AR)")]
    public GameObject floatingTagObject; // O objeto "Floating_Tag_BG"
    public TextMeshProUGUI floatingTagText; // O texto "Chair"

    // --- ELEMENTOS DO CARTÃO DE RESULTADO ---
    [Header("Cartão de Resultado Detalhado")]
    public GameObject resultCardObject;      // O "Result_Card_BG" (azul-escuro)
    public TextMeshProUGUI originalTextMain;     // "Cadeira"
    public TextMeshProUGUI originalTextDetail;   // "A cadeira é marrom"
    public TextMeshProUGUI translatedTextMain;   // "Chair"
    public TextMeshProUGUI translatedTextDetail; // "The chair is brown"
    public Button ttsButton;               // O botão azul de áudio

    // --- SNACKBARS DE CONECTIVIDADE ---
    [Header("Snackbars de Conectividade")]
    // (Arraste os 3 objetos de dentro do seu "ConnectionStatus_Group" para cá)
    public GameObject offlineSnackbar;
    public GameObject reconnectingSnackbar;
    public GameObject onlineSnackbar;


    /// <summary>
    /// Chamado quando o script é iniciado.
    /// Define o estado inicial da UI e configura os botões.
    /// </summary>
    void Start()
    {
        // Define o estado inicial da UI (só a tela de boas-vindas é visível)
        welcomePanel.SetActive(true);
        mainPanel.SetActive(false);
        menuPanel.SetActive(false);
        settingsPanel.SetActive(false);
        historyPanel.SetActive(false);
        detectionOverlayGroup.SetActive(false);
        HideAllSnackbars();

        // Configura os "listeners" dos botões (o que acontece ao clicar)
        startButton.onClick.AddListener(StartApplication);
        ttsButton.onClick.AddListener(OnTTSButtonPressed);
        mainHistoryButton.onClick.AddListener(ShowMenu); // Ex: Botão do menu no MainPanel
        mainSettingsButton.onClick.AddListener(ShowMenu); // Ex: Botão de config no MainPanel

        // TODO: Conectar os outros botões (Voltar, Tutorial, etc.)
        // Ex: menuBackButton.onClick.AddListener(ShowMainPanel);
    }

    // --- MÉTODOS PÚBLICOS (para outros scripts chamarem) ---

    /// <summary>
    /// Mostra o resultado da detecção na tela.
    /// </summary>
    public void ShowDetectionResult(string tagText, string origMain, string origDetail, string transMain, string transDetail)
    {
        // Preenche os textos
        floatingTagText.text = tagText;
        originalTextMain.text = origMain;
        originalTextDetail.text = origDetail;
        translatedTextMain.text = transMain;
        translatedTextDetail.text = transDetail;

        // Ativa os grupos de UI
        detectionOverlayGroup.SetActive(true);
    }

    /// <summary>
    /// Esconde o overlay de detecção.
    /// </summary>
    public void HideDetectionResult()
    {
        detectionOverlayGroup.SetActive(false);
    }

    /// <summary>
    /// Define o estado da conectividade e mostra o snackbar apropriado.
    /// </summary>
    public void SetConnectionStatus(ConnectionStatus status)
    {
        HideAllSnackbars();

        switch (status)
        {
            case ConnectionStatus.Online:
                ShowSnackbar(onlineSnackbar, 3f); // Mostra "Online" por 3 segundos
                break;
            case ConnectionStatus.Offline:
                ShowSnackbar(offlineSnackbar, 0); // Mostra "Offline" indefinidamente
                break;
            case ConnectionStatus.Reconnecting:
                ShowSnackbar(reconnectingSnackbar, 0); // Mostra "Reconectando" indefinidamente
                break;
        }
    }

    // --- MÉTODOS DE NAVEGAÇÃO (para os botões chamarem) ---

    /// <summary>
    /// Chamado pelo `startButton`. Esconde a tela de boas-vindas e mostra a principal.
    /// </summary>
    public void StartApplication()
    {
        welcomePanel.SetActive(false);
        mainPanel.SetActive(true);
    }

    /// <summary>
    /// Mostra a tela principal (ex: ao voltar do menu)
    /// </summary>
    public void ShowMainPanel()
    {
        menuPanel.SetActive(false);
        settingsPanel.SetActive(false);
        historyPanel.SetActive(false);

        mainPanel.SetActive(true);
    }

    /// <summary>
    /// Mostra o menu.
    /// </summary>
    public void ShowMenu()
    {
        mainPanel.SetActive(false);
        settingsPanel.SetActive(false); // Esconde outros menus caso estejam abertos
        historyPanel.SetActive(false);

        menuPanel.SetActive(true);
    }

    /// <summary>
    /// Mostra a tela de Configurações.
    /// </summary>
    public void ShowSettings()
    {
        menuPanel.SetActive(false); // Esconde o menu
        settingsPanel.SetActive(true);
    }

    /// <summary>
    /// Mostra a tela de Histórico.
    /// </summary>
    public void ShowHistory()
    {
        menuPanel.SetActive(false); // Esconde o menu
        historyPanel.SetActive(true);
    }

    // --- LISTENERS DE AÇÃO ---

    /// <summary>
    /// Chamado pelo `ttsButton`.
    /// </summary>
    private void OnTTSButtonPressed()
    {
        Debug.Log("Botão TTS (Áudio) pressionado!");
        // TODO: Chamar seu script de áudio aqui.
    }


    // --- MÉTODOS AUXILIARES (para os snackbars) ---

    private void ShowSnackbar(GameObject snackbar, float duration)
    {
        snackbar.SetActive(true);
        if (duration > 0)
        {
            // Esconde o snackbar após 'duration' segundos
            StartCoroutine(HideSnackbarAfterTime(snackbar, duration));
        }
    }

    private void HideAllSnackbars()
    {
        if (offlineSnackbar) offlineSnackbar.SetActive(false);
        if (reconnectingSnackbar) reconnectingSnackbar.SetActive(false);
        if (onlineSnackbar) onlineSnackbar.SetActive(false);
    }

    private IEnumerator HideSnackbarAfterTime(GameObject snackbar, float delay)
    {
        yield return new WaitForSeconds(delay);
        snackbar.SetActive(false);
    }
}

// Enum para facilitar o controle do status de conexão
public enum ConnectionStatus
{
    Online,
    Offline,
    Reconnecting
}