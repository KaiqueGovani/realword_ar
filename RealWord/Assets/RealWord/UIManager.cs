using UnityEngine;
using UnityEngine.UI; // Necess�rio para UI (Button, Image, etc.)
using TMPro;          // Necess�rio para TextMeshPro
using System.Collections; // Necess�rio para Coroutines (esconder snackbars)

/// <summary>
/// Gerencia toda a interface gr�fica (UI) do aplicativo RealWord.
/// Controla a troca de pain�is, preenche textos e gerencia estados de UI.
/// </summary>
public class UIManager : MonoBehaviour
{
    // --- PAIN�IS PRINCIPAIS (As "Telas") ---
    [Header("Pain�is de Tela")]
    public GameObject welcomePanel; // A "Tela Inicial"
    public GameObject mainPanel;    // A "Tela Principal" (com a c�mera)
    public GameObject menuPanel;    // A tela de Menu
    public GameObject settingsPanel; // A tela de Configura��es
    public GameObject historyPanel;  // A tela de Hist�rico

    // --- BOT�ES ---
    [Header("Bot�es Principais")]
    public Button startButton;      // Bot�o "Come�ar" da tela inicial

    // Bot�es do MainPanel (que voc� criou no topo)
    public Button mainHistoryButton;
    public Button mainSettingsButton;

    // Bot�es dos Pain�is de Menu
    // (Adicione refer�ncias para os bot�es "Voltar", "Tutorial", etc. se precisar)
    // Ex: public Button menuBackButton;
    // Ex: public Button settingsTutorialButton;


    // --- GRUPO DE DETEC��O (Come�a desativado) ---
    [Header("Grupo de Detec��o (Overlay)")]
    public GameObject detectionOverlayGroup; // O "pai" de toda a UI de detec��o

    // --- ELEMENTOS DA TAG FLUTUANTE ---
    [Header("Tag Flutuante (AR)")]
    public GameObject floatingTagObject; // O objeto "Floating_Tag_BG"
    public TextMeshProUGUI floatingTagText; // O texto "Chair"

    // --- ELEMENTOS DO CART�O DE RESULTADO ---
    [Header("Cart�o de Resultado Detalhado")]
    public GameObject resultCardObject;      // O "Result_Card_BG" (azul-escuro)
    public TextMeshProUGUI originalTextMain;     // "Cadeira"
    public TextMeshProUGUI originalTextDetail;   // "A cadeira � marrom"
    public TextMeshProUGUI translatedTextMain;   // "Chair"
    public TextMeshProUGUI translatedTextDetail; // "The chair is brown"
    public Button ttsButton;               // O bot�o azul de �udio

    // --- CAROUSEL NAVIGATION ---
    [Header("Carousel Navigation")]
    public Button phrasePrevButton;  // Reference to Prev_Arrow GameObject
    public Button phraseNextButton;  // Reference to Next_Arrow GameObject
    public TextMeshProUGUI phraseIndexText; // Display "1/2" or similar

    // --- OVERLAY BUTTON ---
    [Header("Overlay Button")]
    public Button showOverlayButton; // Button that appears when object detected

    // --- DEPENDENCIES ---
    [Header("Dependencies")]
    public DetectionResultManager detectionResultManager;

    // --- SNACKBARS DE CONECTIVIDADE ---
    [Header("Snackbars de Conectividade")]
    // (Arraste os 3 objetos de dentro do seu "ConnectionStatus_Group" para c�)
    public GameObject offlineSnackbar;
    public GameObject reconnectingSnackbar;
    public GameObject onlineSnackbar;


    /// <summary>
    /// Chamado quando o script � iniciado.
    /// Define o estado inicial da UI e configura os bot�es.
    /// </summary>
    void Start()
    {
        // Define o estado inicial da UI (s� a tela de boas-vindas � vis�vel)
        welcomePanel.SetActive(true);
        mainPanel.SetActive(false);
        menuPanel.SetActive(false);
        settingsPanel.SetActive(false);
        historyPanel.SetActive(false);
        detectionOverlayGroup.SetActive(false);
        HideAllSnackbars();

        // Configura os "listeners" dos bot�es (o que acontece ao clicar)
        startButton.onClick.AddListener(StartApplication);
        ttsButton.onClick.AddListener(OnTTSButtonPressed);
        mainHistoryButton.onClick.AddListener(ShowMenu); // Ex: Bot�o do menu no MainPanel
        mainSettingsButton.onClick.AddListener(ShowMenu); // Ex: Bot�o de config no MainPanel

        // Carousel navigation buttons
        if (phrasePrevButton != null)
        {
            phrasePrevButton.onClick.AddListener(OnPreviousPhraseClicked);
        }
        if (phraseNextButton != null)
        {
            phraseNextButton.onClick.AddListener(OnNextPhraseClicked);
        }

        // Overlay button
        if (showOverlayButton != null)
        {
            showOverlayButton.onClick.AddListener(OnShowOverlayClicked);
            showOverlayButton.gameObject.SetActive(false); // Initially hidden
        }

        // Find DetectionResultManager if not assigned
        if (detectionResultManager == null)
        {
            detectionResultManager = FindObjectOfType<DetectionResultManager>();
        }

        // TODO: Conectar os outros bot�es (Voltar, Tutorial, etc.)
        // Ex: menuBackButton.onClick.AddListener(ShowMainPanel);
    }

    // --- M�TODOS P�BLICOS (para outros scripts chamarem) ---

    /// <summary>
    /// Mostra o resultado da detec��o na tela.
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
    /// Updates the phrase carousel display with current index and total phrases
    /// </summary>
    public void UpdatePhraseCarousel(int currentIndex, int totalPhrases, string origMain, string origDetail, string transMain, string transDetail)
    {
        // Update texts
        if (floatingTagText != null)
        {
            floatingTagText.text = origMain; // Object name
        }
        if (originalTextMain != null)
        {
            originalTextMain.text = origMain; // Object name
        }
        if (originalTextDetail != null)
        {
            originalTextDetail.text = origDetail; // Current phrase
        }
        if (translatedTextMain != null)
        {
            translatedTextMain.text = transMain; // Translation (object name or phrase)
        }
        if (translatedTextDetail != null)
        {
            translatedTextDetail.text = transDetail; // Current translation
        }

        // Update phrase index indicator
        if (phraseIndexText != null)
        {
            if (totalPhrases > 1)
            {
                phraseIndexText.text = $"{currentIndex + 1}/{totalPhrases}";
                phraseIndexText.gameObject.SetActive(true);
            }
            else
            {
                phraseIndexText.gameObject.SetActive(false);
            }
        }

        // Show/hide and enable/disable navigation buttons based on phrase count
        bool hasMultiplePhrases = totalPhrases > 1;
        if (phrasePrevButton != null)
        {
            phrasePrevButton.gameObject.SetActive(hasMultiplePhrases);
            phrasePrevButton.interactable = hasMultiplePhrases;
        }
        if (phraseNextButton != null)
        {
            phraseNextButton.gameObject.SetActive(hasMultiplePhrases);
            phraseNextButton.interactable = hasMultiplePhrases;
        }

        // Show the overlay
        if (detectionOverlayGroup != null)
        {
            detectionOverlayGroup.SetActive(true);
        }
    }

    /// <summary>
    /// Shows loading state while waiting for API response
    /// </summary>
    public void ShowLoadingState(string objectName, bool isLoading = true, string errorMessage = null)
    {
        if (detectionOverlayGroup != null)
        {
            detectionOverlayGroup.SetActive(true);
        }

        if (!string.IsNullOrEmpty(errorMessage))
        {
            // Show error
            if (originalTextDetail != null)
            {
                originalTextDetail.text = $"Erro: {errorMessage}";
            }
            if (translatedTextDetail != null)
            {
                translatedTextDetail.text = "";
            }
        }
        else if (isLoading)
        {
            // Show loading
            if (floatingTagText != null)
            {
                floatingTagText.text = objectName;
            }
            if (originalTextMain != null)
            {
                originalTextMain.text = objectName;
            }
            if (originalTextDetail != null)
            {
                originalTextDetail.text = "Carregando...";
            }
            if (translatedTextDetail != null)
            {
                translatedTextDetail.text = "Loading...";
            }
        }

        // Hide and disable carousel controls during loading
        if (phraseIndexText != null)
        {
            phraseIndexText.gameObject.SetActive(false);
        }
        if (phrasePrevButton != null)
        {
            phrasePrevButton.gameObject.SetActive(false);
            phrasePrevButton.interactable = false;
        }
        if (phraseNextButton != null)
        {
            phraseNextButton.gameObject.SetActive(false);
            phraseNextButton.interactable = false;
        }
    }

    /// <summary>
    /// Shows or hides the overlay button
    /// </summary>
    public void ShowOverlayButton(bool show)
    {
        if (showOverlayButton != null)
        {
            showOverlayButton.gameObject.SetActive(show);
        }
    }

    /// <summary>
    /// Sets the enabled/disabled state of carousel navigation buttons
    /// </summary>
    public void SetCarouselButtonsEnabled(bool enabled)
    {
        if (phrasePrevButton != null)
        {
            phrasePrevButton.interactable = enabled;
        }
        if (phraseNextButton != null)
        {
            phraseNextButton.interactable = enabled;
        }
    }

    /// <summary>
    /// Esconde o overlay de detec��o.
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

    // --- M�TODOS DE NAVEGA��O (para os bot�es chamarem) ---

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
    /// Mostra a tela de Configura��es.
    /// </summary>
    public void ShowSettings()
    {
        menuPanel.SetActive(false); // Esconde o menu
        settingsPanel.SetActive(true);
    }

    /// <summary>
    /// Mostra a tela de Hist�rico.
    /// </summary>
    public void ShowHistory()
    {
        menuPanel.SetActive(false); // Esconde o menu
        historyPanel.SetActive(true);
    }

    // --- LISTENERS DE A��O ---

    /// <summary>
    /// Chamado pelo `ttsButton`.
    /// </summary>
    private void OnTTSButtonPressed()
    {
        if (detectionResultManager == null)
        {
            Debug.LogWarning("DetectionResultManager not assigned. Cannot play TTS.");
            return;
        }

        // Get current phrase data
        PhraseData phraseData = detectionResultManager.GetCurrentPhraseData(null);
        if (phraseData == null)
        {
            Debug.LogWarning("No phrase data available for TTS playback.");
            return;
        }

        // Use TTSManager if available, otherwise log
        TTSManager ttsManager = FindObjectOfType<TTSManager>();
        if (ttsManager != null)
        {
            ttsManager.PlayBothLanguages(phraseData.objectName); // Play english phrase
        }
        else
        {
            Debug.LogWarning("TTSManager not found. Cannot play audio.");
        }
    }

    /// <summary>
    /// Called when previous phrase button is clicked
    /// </summary>
    private void OnPreviousPhraseClicked()
    {
        if (detectionResultManager != null)
        {
            detectionResultManager.ShowPreviousPhrase();
        }
    }

    /// <summary>
    /// Called when next phrase button is clicked
    /// </summary>
    private void OnNextPhraseClicked()
    {
        if (detectionResultManager != null)
        {
            detectionResultManager.ShowNextPhrase();
        }
    }

    /// <summary>
    /// Called when show overlay button is clicked
    /// </summary>
    private void OnShowOverlayClicked()
    {
        if (detectionResultManager != null)
        {
            detectionResultManager.SelectObjectForOverlay(); // Uses highest confidence
        }
    }


    // --- M�TODOS AUXILIARES (para os snackbars) ---

    private void ShowSnackbar(GameObject snackbar, float duration)
    {
        snackbar.SetActive(true);
        if (duration > 0)
        {
            // Esconde o snackbar ap�s 'duration' segundos
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

// Enum para facilitar o controle do status de conex�o
public enum ConnectionStatus
{
    Online,
    Offline,
    Reconnecting
}