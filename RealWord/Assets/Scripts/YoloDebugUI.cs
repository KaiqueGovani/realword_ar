using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages debug UI overlay for YOLO detector
/// </summary>
public class YoloDebugUI : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private Canvas debugCanvas;
    [SerializeField] private Font debugFont;
    [SerializeField] private int sortingOrder = 100;

    // Debug UI components
    private Text fpsText;
    private Text modelInfoText;
    private Text detectionCountText;
    private Text inferenceTimeText;
    private Text tensorInfoText;
    private Text webcamInfoText;
    private Text labelsInfoText;
    private Text pathInfoText;
    private Text asyncModeText;
    private Text inferenceStateText;

    // FPS tracking
    private float lastFpsUpdate;
    private int frameCount;
    private float currentFps;

    private bool isInitialized = false;

    public void Initialize()
    {
        if (isInitialized) return;

        SetupCanvas();
        SetupFont();
        CreateDebugTexts();

        isInitialized = true;
        Debug.Log("[YoloDebugUI] Debug UI inicializada.");
    }

    private void SetupCanvas()
    {
        if (debugCanvas == null)
        {
            GameObject canvasGO = new GameObject("YoloDebugCanvas");
            canvasGO.transform.SetParent(transform, false);
            
            debugCanvas = canvasGO.AddComponent<Canvas>();
            debugCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            debugCanvas.sortingOrder = sortingOrder;
            
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }
    }

    private void SetupFont()
    {
        if (debugFont == null)
        {
            debugFont = Resources.Load<Font>("MobileARTemplateAssets/UI/Fonts/Inter-Regular");
            if (debugFont == null)
            {
                Debug.LogWarning("[YoloDebugUI] Could not find Inter-Regular font at Assets/MobileARTemplateAssets/UI/Fonts/Inter-Regular.ttf. Using Arial as fallback.");
                debugFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
        }
    }

    private void CreateDebugTexts()
    {
        fpsText = CreateDebugText("FPS: --", new Vector2(-50, 500));
        modelInfoText = CreateDebugText("Model: Loading...", new Vector2(-50, 470));
        asyncModeText = CreateDebugText("Async: --", new Vector2(-50, 440));
        inferenceStateText = CreateDebugText("State: Idle", new Vector2(-50, 410));
        detectionCountText = CreateDebugText("Detections: 0", new Vector2(-50, 380));
        inferenceTimeText = CreateDebugText("Inference: -- ms", new Vector2(-50, 350));
        tensorInfoText = CreateDebugText("Tensor: --", new Vector2(-50, 320));
        webcamInfoText = CreateDebugText("Webcam: --", new Vector2(-50, 290));
        labelsInfoText = CreateDebugText("Labels: Loading...", new Vector2(-50, 260));
        pathInfoText = CreateDebugText("Path: --", new Vector2(-50, 230));
    }

    private Text CreateDebugText(string initialText, Vector2 position)
    {
        GameObject textGO = new GameObject("DebugText");
        textGO.transform.SetParent(debugCanvas.transform, false);

        Text textComponent = textGO.AddComponent<Text>();
        textComponent.text = initialText;
        textComponent.font = debugFont;
        textComponent.fontSize = 16;
        textComponent.color = Color.white;
        textComponent.fontStyle = FontStyle.Bold;
        textComponent.horizontalOverflow = HorizontalWrapMode.Overflow;
        textComponent.verticalOverflow = VerticalWrapMode.Overflow;

        // Add outline for better visibility
        Outline outline = textGO.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(1, 1);

        RectTransform rectTransform = textGO.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = new Vector2(400, 20);

        return textComponent;
    }

    public void UpdateFPS()
    {
        if (!isInitialized) return;

        frameCount++;
        float currentTime = Time.realtimeSinceStartup;

        if (currentTime - lastFpsUpdate >= 1.0f)
        {
            currentFps = frameCount / (currentTime - lastFpsUpdate);
            frameCount = 0;
            lastFpsUpdate = currentTime;

            if (fpsText != null)
                fpsText.text = $"FPS: {currentFps:F1}";
        }
    }

    public void UpdateModelInfo(string modelName, string backendType)
    {
        if (!isInitialized || modelInfoText == null) return;
        modelInfoText.text = $"Model: {modelName} ({backendType})";
    }

    public void UpdateDetectionCount(int detections, int totalBoxes)
    {
        if (!isInitialized || detectionCountText == null) return;
        detectionCountText.text = $"Detections: {detections}/{totalBoxes}";
    }

    public void UpdateInferenceTime(float milliseconds)
    {
        if (!isInitialized || inferenceTimeText == null) return;
        inferenceTimeText.text = $"Inference: {milliseconds:F1} ms";
    }

    public void UpdateTensorInfo(int batch, int channels, int boxes)
    {
        if (!isInitialized || tensorInfoText == null) return;
        tensorInfoText.text = $"Tensor: {batch}x{channels}x{boxes}";
    }

    public void UpdateWebcamInfo(int width, int height, int rotation)
    {
        if (!isInitialized || webcamInfoText == null) return;
        webcamInfoText.text = $"Camera: {width}x{height} (Rotation: {rotation}°)";
    }

    public void UpdateLabelsInfo(int labelCount, bool loaded)
    {
        if (!isInitialized || labelsInfoText == null) return;
        
        if (loaded)
            labelsInfoText.text = $"Labels: {labelCount} loaded";
        else
            labelsInfoText.text = "Labels: FAILED to load";
    }

    public void UpdatePathInfo(string dataPath)
    {
        if (!isInitialized || pathInfoText == null) return;
        
        // Truncate path if too long for display
        if (dataPath.Length > 40)
        {
            dataPath = "..." + dataPath.Substring(dataPath.Length - 37);
        }
        pathInfoText.text = $"DataPath: {dataPath}";
    }

    public void SetInferenceError()
    {
        if (!isInitialized || inferenceTimeText == null) return;
        inferenceTimeText.text = "Inference: ERROR - No output";
    }

    public void UpdateAsyncMode(bool isAsync, int interval)
    {
        if (!isInitialized || asyncModeText == null) return;
        string mode = isAsync ? "ENABLED" : "DISABLED";
        asyncModeText.text = $"Async: {mode} (Interval: {interval})";
        asyncModeText.color = isAsync ? Color.green : Color.yellow;
    }

    public void UpdateInferenceState(string state, Color color)
    {
        if (!isInitialized || inferenceStateText == null) return;
        inferenceStateText.text = $"State: {state}";
        inferenceStateText.color = color;
    }

    public float GetCurrentFPS()
    {
        return currentFps;
    }

    private void OnDestroy()
    {
        if (debugCanvas != null && debugCanvas.gameObject != null)
        {
            Destroy(debugCanvas.gameObject);
        }
    }
}
