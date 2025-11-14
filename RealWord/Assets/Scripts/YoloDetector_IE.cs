using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Unity.InferenceEngine;

public class YoloDetector_IE : MonoBehaviour
{
    [Header("Modelo (arraste o asset .onnx / ModelAsset aqui)")]
    public ModelAsset modelAsset;

    [Header("UI")]
    public Text labelText;
    public RawImage cameraDisplay;

    [Header("Camera Settings - S25 Ultra")]
    public bool useFrontCamera = false;

    [Header("Debug Mode")]
    public bool enableDebugMode = true;
    public Canvas debugCanvas;
    public Font debugFont;

    [Header("Input")]
    public int inputWidth = 640;
    public int inputHeight = 640;
    
    [Header("Performance")]
    public int inferenceInterval = 10; // Run inference every N frames (was 6, now 10 for better performance)


    private Model runtimeModel;
    private Worker worker;
    private Tensor<float> inputTensor;
    private readonly string inputName = "images";

    private string[] labels;

    // Camera variables
    private WebCamTexture builtInWebCamTexture;
    private Canvas cameraCanvas;

    // Debug UI components
    private Text debugFpsText;
    private Text debugModelInfoText;
    private Text debugDetectionCountText;
    private Text debugInferenceTimeText;
    private Text debugTensorInfoText;
    private Text debugWebcamInfoText;
    private Text debugLabelsInfoText;
    private Text debugPathInfoText;

    // Debug timing
    private float inferenceStartTime;
    private float lastFpsUpdate;
    private int frameCount;
    private float fps;

    void Start()
    {
        if (modelAsset == null)
        {
            Debug.LogError("Arraste o ModelAsset do .onnx no Inspector.");
            labelText.text = "Modelo missing";
            return;
        }

        // Initialize camera
        InitializeCamera();

        try
        {
            // 1) Carrega modelo
            runtimeModel = ModelLoader.Load(modelAsset);
            Debug.Log("[YoloDetector_IE] Modelo carregado.");

            // 2) Cria worker (GPU se disponível)
            worker = new Worker(runtimeModel, BackendType.GPUCompute);
            Debug.Log("[YoloDetector_IE] Worker criado (GPU).");

            // 3) Aloca tensor
            inputTensor = new Tensor<float>(new TensorShape(1, 3, inputHeight, inputWidth));

            // 4) Carrega labels
            LoadLabelsFromFile();
            
            // Update labels debug info
            if (enableDebugMode && debugLabelsInfoText != null)
            {
                if (labels != null && labels.Length > 0)
                {
                    debugLabelsInfoText.text = $"Labels: {labels.Length} loaded";
                }
                else
                {
                    debugLabelsInfoText.text = "Labels: FAILED to load";
                }
            }

            // 5) Setup debug UI se necessário
            if (enableDebugMode)
            {
                SetupDebugUI();
                UpdateModelDebugInfo();
                UpdatePathDebugInfo();
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[YoloDetector_IE] Erro de inicialização: " + e);
            labelText.text = "Erro init";
        }
    }

    void InitializeCamera()
    {
        try
        {
            Debug.Log("[YoloDetector_IE] Initializing camera for S25 Ultra...");
            
            // Samsung S25 Ultra optimal settings for portrait mode
            int cameraWidth = 1080;
            int cameraHeight = 2340;
            int fps = 30;
            
            // Find the desired camera (rear or front)
            WebCamDevice[] devices = WebCamTexture.devices;
            string selectedDevice = "";
            
            foreach (var device in devices)
            {
                if (device.isFrontFacing == useFrontCamera)
                {
                    selectedDevice = device.name;
                    Debug.Log($"[YoloDetector_IE] Selected camera: {selectedDevice}");
                    break;
                }
            }
            
            // Create camera texture
            if (!string.IsNullOrEmpty(selectedDevice))
            {
                builtInWebCamTexture = new WebCamTexture(selectedDevice, cameraWidth, cameraHeight, fps);
            }
            else
            {
                builtInWebCamTexture = new WebCamTexture(cameraWidth, cameraHeight, fps);
                Debug.LogWarning("[YoloDetector_IE] Using default camera");
            }
            
            // Start camera
            builtInWebCamTexture.Play();
            
            // Setup display
            SetupCameraDisplay();
            
            Debug.Log("[YoloDetector_IE] Camera initialized successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[YoloDetector_IE] Camera initialization failed: {e.Message}");
            labelText.text = $"Camera error: {e.Message}";
        }
    }
    
    void SetupCameraDisplay()
    {
        // Create canvas for camera display
        if (cameraCanvas == null)
        {
            GameObject canvasGO = new GameObject("CameraCanvas");
            cameraCanvas = canvasGO.AddComponent<Canvas>();
            cameraCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cameraCanvas.sortingOrder = -1; // Behind UI
            canvasGO.AddComponent<GraphicRaycaster>();
        }
        
        // Create camera display
        if (cameraDisplay == null)
        {
            GameObject displayGO = new GameObject("CameraDisplay");
            displayGO.transform.SetParent(cameraCanvas.transform, false);
            
            cameraDisplay = displayGO.AddComponent<RawImage>();
            cameraDisplay.texture = builtInWebCamTexture;
            
            // Fill entire screen
            RectTransform rect = displayGO.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            // Handle camera rotation using UV rect instead of transform rotation
            int rotation = builtInWebCamTexture.videoRotationAngle;
            bool verticallyMirrored = builtInWebCamTexture.videoVerticallyMirrored;
            
            Debug.Log($"[YoloDetector_IE] Camera rotation: {rotation}, mirrored: {verticallyMirrored}, size: {builtInWebCamTexture.width}x{builtInWebCamTexture.height}");
            
            // Adjust UV coordinates based on rotation
            // This handles the rotation without actually rotating the RectTransform
            Rect uvRect = new Rect(0, 0, 1, 1);
            
            if (rotation == 90)
            {
                rect.localRotation = Quaternion.Euler(0, 0, -90);
                rect.localScale = new Vector3(Screen.height / (float)Screen.width, Screen.width / (float)Screen.height, 1);
            }
            else if (rotation == 270)
            {
                rect.localRotation = Quaternion.Euler(0, 0, -270);
                rect.localScale = new Vector3(Screen.height / (float)Screen.width, Screen.width / (float)Screen.height, 1);
            }
            else if (rotation == 180)
            {
                rect.localRotation = Quaternion.Euler(0, 0, -180);
            }
            
            // Mirror for front camera if needed
            if (useFrontCamera && !verticallyMirrored)
            {
                Vector3 scale = rect.localScale;
                scale.x *= -1;
                rect.localScale = scale;
            }
            
            cameraDisplay.uvRect = uvRect;
        }
    }

    void LoadLabelsFromFile()
    {
        try
        {
            string path = Path.Combine(Application.dataPath, "Resources/classes.txt");
            Debug.Log($"[YoloDetector_IE] Tentando carregar labels de: {path}");
            Debug.Log($"[YoloDetector_IE] Application.dataPath = {Application.dataPath}");
            Debug.Log($"[YoloDetector_IE] Application.streamingAssetsPath = {Application.streamingAssetsPath}");
            Debug.Log($"[YoloDetector_IE] Application.persistentDataPath = {Application.persistentDataPath}");
            Debug.Log($"[YoloDetector_IE] Platform: {Application.platform}");
            
            if (File.Exists(path))
            {
                labels = File.ReadAllLines(path);
                Debug.Log($"[YoloDetector_IE] {labels.Length} labels carregadas com sucesso.");
                
                // Log first few labels for verification
                for (int i = 0; i < Math.Min(5, labels.Length); i++)
                {
                    Debug.Log($"[YoloDetector_IE] Label {i}: {labels[i]}");
                }
            }
            else
            {
                Debug.LogWarning($"[YoloDetector_IE] Arquivo de labels não encontrado em: {path}");
                
                // Try alternative paths
                TryAlternativeLabelPaths();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[YoloDetector_IE] Falha ao ler labels: " + ex.Message);
            Debug.LogError("[YoloDetector_IE] Stack trace: " + ex.StackTrace);
            labels = new string[0];
            
            // Try alternative paths on error
            TryAlternativeLabelPaths();
        }
    }
    
    void TryAlternativeLabelPaths()
    {
        Debug.Log("[YoloDetector_IE] Tentando caminhos alternativos...");
        
        // Try Resources folder using Resources.Load
        try
        {
            TextAsset labelAsset = Resources.Load<TextAsset>("classes");
            if (labelAsset != null)
            {
                labels = labelAsset.text.Split('\n');
                // Clean up any empty lines or carriage returns
                labels = System.Array.FindAll(labels, s => !string.IsNullOrWhiteSpace(s));
                for (int i = 0; i < labels.Length; i++)
                {
                    labels[i] = labels[i].Trim();
                }
                Debug.Log($"[YoloDetector_IE] Labels carregadas via Resources.Load: {labels.Length} labels");
                return;
            }
            else
            {
                Debug.LogWarning("[YoloDetector_IE] classes.txt não encontrado em Resources/");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[YoloDetector_IE] Erro ao carregar via Resources.Load: " + ex.Message);
        }
        
        // Try StreamingAssets path
        try
        {
            string streamingPath = Path.Combine(Application.streamingAssetsPath, "classes.txt");
            Debug.Log($"[YoloDetector_IE] Tentando StreamingAssets: {streamingPath}");
            
            if (File.Exists(streamingPath))
            {
                labels = File.ReadAllLines(streamingPath);
                Debug.Log($"[YoloDetector_IE] Labels carregadas de StreamingAssets: {labels.Length} labels");
                return;
            }
            else
            {
                Debug.LogWarning($"[YoloDetector_IE] classes.txt não encontrado em StreamingAssets: {streamingPath}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[YoloDetector_IE] Erro ao carregar de StreamingAssets: " + ex.Message);
        }
        
        // List all files in Resources directory for debugging
        try
        {
            string resourcesPath = Path.Combine(Application.dataPath, "Resources");
            if (Directory.Exists(resourcesPath))
            {
                Debug.Log("[YoloDetector_IE] Arquivos encontrados em Resources:");
                string[] files = Directory.GetFiles(resourcesPath);
                foreach (string file in files)
                {
                    Debug.Log($"[YoloDetector_IE] - {Path.GetFileName(file)}");
                }
            }
            else
            {
                Debug.LogWarning($"[YoloDetector_IE] Diretório Resources não existe: {resourcesPath}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[YoloDetector_IE] Erro ao listar arquivos Resources: " + ex.Message);
        }
        
        // Create fallback labels if nothing works
        if (labels == null || labels.Length == 0)
        {
            Debug.LogWarning("[YoloDetector_IE] Criando labels padrão como fallback");
            labels = new string[] { "person", "bicycle", "car", "motorcycle", "airplane", "bus", "train", "truck", "boat" };
        }
    }

    void SetupDebugUI()
    {
        // Se não há canvas especificado, cria um
        if (debugCanvas == null)
        {
            GameObject canvasGO = new GameObject("DebugCanvas");
            debugCanvas = canvasGO.AddComponent<Canvas>();
            debugCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            debugCanvas.sortingOrder = 100;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        // Se não há fonte especificada, usa Arial
        if (debugFont == null)
        {
            debugFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        // Cria textos de debug
        debugFpsText = CreateDebugText("FPS: --", new Vector2(-350, 250));
        debugModelInfoText = CreateDebugText("Model: Loading...", new Vector2(-350, 220));
        debugDetectionCountText = CreateDebugText("Detections: 0", new Vector2(-350, 190));
        debugInferenceTimeText = CreateDebugText("Inference: -- ms", new Vector2(-350, 160));
        debugTensorInfoText = CreateDebugText("Tensor: --", new Vector2(-350, 130));
        debugWebcamInfoText = CreateDebugText("Webcam: --", new Vector2(-350, 100));
        debugLabelsInfoText = CreateDebugText("Labels: Loading...", new Vector2(-350, 70));
        debugPathInfoText = CreateDebugText("Path: --", new Vector2(-350, 40));

        Debug.Log("[YoloDetector_IE] Debug UI configurada.");
    }

    Text CreateDebugText(string initialText, Vector2 position)
    {
        GameObject textGO = new GameObject("DebugText");
        textGO.transform.SetParent(debugCanvas.transform, false);

        Text textComponent = textGO.AddComponent<Text>();
        textComponent.text = initialText;
        textComponent.font = debugFont;
        textComponent.fontSize = 14;
        textComponent.color = Color.white;
        textComponent.fontStyle = FontStyle.Bold;

        // Add outline for better visibility
        Outline outline = textGO.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(1, 1);

        RectTransform rectTransform = textGO.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = new Vector2(300, 20);

        return textComponent;
    }

    void Update()
    {
        if (worker == null || runtimeModel == null) return;
        
        // Get the current webcam texture from either built-in or external webcam feed
        Texture currentWebcamTexture = GetCurrentWebcamTexture();
        
        if (currentWebcamTexture == null) return;
        
        if (labels == null || labels.Length == 0)
        {
            string debugInfo = "";
            if (enableDebugMode)
            {
                debugInfo = $" (Platform: {Application.platform}, DataPath exists: {Directory.Exists(Application.dataPath)})";
            }
            labelText.text = $"Labels não carregadas.{debugInfo}";
            
            // Update debug info
            if (enableDebugMode && debugLabelsInfoText != null)
            {
                debugLabelsInfoText.text = "Labels: NOT LOADED - Check console";
            }
            return;
        }

        // Update camera display and debug info
        if (enableDebugMode)
        {
            UpdateFPS();
            if (Time.frameCount % 30 == 0) // Only update webcam debug every 30 frames
            {
                UpdateWebcamDebugInfo();
            }
        }
        
        // Update camera display
        if (cameraDisplay != null && builtInWebCamTexture != null && builtInWebCamTexture.isPlaying)
        {
            cameraDisplay.texture = builtInWebCamTexture;
        }

        // roda a cada N frames para não sobrecarregar
        if (Time.frameCount % inferenceInterval != 0) return;

        // Start inference timing
        if (enableDebugMode)
        {
            inferenceStartTime = Time.realtimeSinceStartup;
        }

        try
        {
            TextureConverter.ToTensor(currentWebcamTexture, inputTensor,
                new TextureTransform().SetTensorLayout(TensorLayout.NCHW));
        }
        catch
        {
            CopyWebcamToTensorFallback(currentWebcamTexture, inputTensor);
        }

        // roda inferência
        worker.SetInput(inputName, inputTensor);
        worker.Schedule();

        // lê saída
        var outTensor = worker.PeekOutput() as Tensor<float>;
        if (outTensor == null)
        {
            labelText.text = "Sem output.";
            if (enableDebugMode && debugInferenceTimeText != null)
            {
                debugInferenceTimeText.text = "Inference: ERROR - No output";
            }
            return;
        }

        using var cpuCopy = outTensor.ReadbackAndClone();

        // Update inference time debug info
        if (enableDebugMode)
        {
            float inferenceTime = (Time.realtimeSinceStartup - inferenceStartTime) * 1000f;
            if (debugInferenceTimeText != null && Time.frameCount % 10 == 0) // Update every 10 frames
                debugInferenceTimeText.text = $"Inference: {inferenceTime:F1} ms";
            
            if (debugTensorInfoText != null && Time.frameCount % 30 == 0) // Update every 30 frames
                debugTensorInfoText.text = $"Tensor: {cpuCopy.shape[0]}x{cpuCopy.shape[1]}x{cpuCopy.shape[2]}";
        }

        int numAttrs = cpuCopy.shape[1];
        int numBoxes = cpuCopy.shape[2];
        float confThreshold = 0.25f;

        int bestClass = -1;
        float bestScore = 0f;
        string bestLabel = "";
        int totalDetections = 0;

        // Loop sobre todas as detecções
        for (int i = 0; i < numBoxes; i++)
        {
            float x = cpuCopy[0, 0, i]; // centro x normalizado
            float y = cpuCopy[0, 1, i]; // centro y normalizado
            float w = cpuCopy[0, 2, i];
            float h = cpuCopy[0, 3, i];

            // encontra classe com maior confiança
            int bestClassIdx = -1;
            float maxScore = 0f;
            for (int c = 4; c < numAttrs; c++)
            {
                float score = cpuCopy[0, c, i];
                if (score > maxScore)
                {
                    maxScore = score;
                    bestClassIdx = c - 4;
                }
            }

            if (maxScore > confThreshold)
            {
                totalDetections++;
                string label = (bestClassIdx >= 0 && bestClassIdx < labels.Length)
                    ? labels[bestClassIdx]
                    : $"cls {bestClassIdx}";


                if (maxScore > bestScore)
                {
                    bestScore = maxScore;
                    bestClass = bestClassIdx;
                    bestLabel = label;
                }
            }
        }

        // Update debug detection count
        if (enableDebugMode && debugDetectionCountText != null && Time.frameCount % 10 == 0)
        {
            debugDetectionCountText.text = $"Detections: {totalDetections}/{numBoxes}";
        }

        // Mostra o label do objeto mais confiante
        if (bestClass >= 0)
            labelText.text = $"Detectado: {bestLabel} ({bestScore * 100f:F1}%)";
        else
            labelText.text = "Nenhum objeto detectado.";
    }

    void CopyWebcamToTensorFallback(Texture webTex, Tensor<float> tensor)
    {
        WebCamTexture wtex = webTex as WebCamTexture ?? builtInWebCamTexture;
        if (wtex == null) return;
        
        Texture2D tmp = new Texture2D(wtex.width, wtex.height, TextureFormat.RGBA32, false);

        RenderTexture rt = RenderTexture.GetTemporary(wtex.width, wtex.height);
        Graphics.Blit(wtex, rt);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;
        tmp.ReadPixels(new Rect(0, 0, wtex.width, wtex.height), 0, 0);
        tmp.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);

        var pixels = tmp.GetPixels();
        int h = inputHeight, w = inputWidth;
        int idx = 0;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Color c = pixels[Mathf.Clamp(y * w + x, 0, pixels.Length - 1)];
                tensor[idx++] = c.r;
                tensor[idx++] = c.g;
                tensor[idx++] = c.b;
            }
        }
        Destroy(tmp);
    }

    Texture GetCurrentWebcamTexture()
    {
        return builtInWebCamTexture;
    }

    void UpdateWebcamDebugInfo()
    {
        if (debugWebcamInfoText != null && builtInWebCamTexture != null)
        {
            debugWebcamInfoText.text = $"Camera: {builtInWebCamTexture.width}x{builtInWebCamTexture.height} @ {builtInWebCamTexture.requestedFPS}fps";
        }
    }

    void UpdateFPS()
    {
        frameCount++;
        float currentTime = Time.realtimeSinceStartup;
        
        if (currentTime - lastFpsUpdate >= 1.0f)
        {
            fps = frameCount / (currentTime - lastFpsUpdate);
            frameCount = 0;
            lastFpsUpdate = currentTime;
            
            if (debugFpsText != null)
                debugFpsText.text = $"FPS: {fps:F1}";
        }
    }

    public void SwitchCamera()
    {
        // Toggle between front and rear camera
        useFrontCamera = !useFrontCamera;
        
        // Stop current camera
        if (builtInWebCamTexture != null)
        {
            builtInWebCamTexture.Stop();
            builtInWebCamTexture = null;
        }
        
        // Reinitialize with new camera
        InitializeCamera();
    }

    void UpdateModelDebugInfo()
    {
        if (debugModelInfoText != null && runtimeModel != null)
        {
            string backendInfo = worker != null ? "GPU" : "CPU";
            debugModelInfoText.text = $"Model: {modelAsset.name} ({backendInfo})";
        }
    }

    void UpdatePathDebugInfo()
    {
        if (debugPathInfoText != null)
        {
            string dataPath = Application.dataPath;
            // Truncate path if too long for display
            if (dataPath.Length > 40)
            {
                dataPath = "..." + dataPath.Substring(dataPath.Length - 37);
            }
            debugPathInfoText.text = $"DataPath: {dataPath}";
        }
    }

    void OnDestroy()
    {
        inputTensor?.Dispose();
        worker?.Dispose();

        // Clean up built-in camera
        if (builtInWebCamTexture != null)
        {
            builtInWebCamTexture.Stop();
            builtInWebCamTexture = null;
        }

        // Clean up debug UI
        if (enableDebugMode && debugCanvas != null && debugCanvas.gameObject != null)
        {
            DestroyImmediate(debugCanvas.gameObject);
        }
        
        // Clean up camera canvas
        if (cameraCanvas != null && cameraCanvas.gameObject != null)
        {
            DestroyImmediate(cameraCanvas.gameObject);
        }
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (builtInWebCamTexture != null)
        {
            if (pauseStatus)
            {
                builtInWebCamTexture.Pause();
            }
            else
            {
                builtInWebCamTexture.Play();
            }
        }
    }
}