using UnityEngine;
using UnityEngine.UI;
using Unity.InferenceEngine;

/// <summary>
/// Main YOLO object detection component using Unity Inference Engine
/// </summary>
public class YoloDetector_IE : MonoBehaviour
{
    [Header("Model Configuration")]
    [Tooltip("Drag the .onnx ModelAsset here")]
    public ModelAsset modelAsset;

    [Header("Input Sources")]
    [Tooltip("Reference to WebcamFeed component")]
    public WebcamFeed webcamFeed;

    [Header("UI Output")]
    [Tooltip("Text component to display detection results")]
    public Text labelText;

    [Header("Model Input Settings")]
    [Tooltip("Input tensor width (default: 640 for YOLOv8)")]
    public int inputWidth = 640;
    [Tooltip("Input tensor height (default: 640 for YOLOv8)")]
    public int inputHeight = 640;
    
    [Header("Performance")]
    [Tooltip("Run inference every N frames to improve performance")]
    public int inferenceInterval = 15;
    
    [Tooltip("Use asynchronous readback to prevent frame freezing")]
    public bool useAsyncReadback = true;
    
    [Tooltip("Backend type for inference")]
    public BackendType backendType = BackendType.GPUCompute;

    [Header("Detection Settings")]
    [Tooltip("Minimum confidence threshold for detections")]
    [Range(0.1f, 0.9f)]
    public float confidenceThreshold = 0.25f;

    [Header("Debug Settings")]
    public bool enableDebugMode = true;

    // Core components
    private Model runtimeModel;
    private Worker worker;
    private Tensor<float> inputTensor;
    private readonly string inputName = "images";

    // Helper components
    private string[] labels;
    private YoloPostProcessor postProcessor;
    private YoloDebugUI debugUI;

    // Performance tracking
    private float inferenceStartTime;
    private bool isInferenceRunning = false;

    void Start()
    {
        if (!ValidateComponents())
            return;

        InitializeModel();
        InitializeLabels();
        InitializePostProcessor();
        InitializeDebugUI();

        Debug.Log("[YoloDetector_IE] Inicialização completa.");
    }

    private bool ValidateComponents()
    {
        if (modelAsset == null)
        {
            Debug.LogError("[YoloDetector_IE] ModelAsset não atribuído no Inspector.");
            if (labelText != null) labelText.text = "Modelo missing";
            return false;
        }

        if (webcamFeed == null)
        {
            Debug.LogError("[YoloDetector_IE] WebcamFeed não atribuído no Inspector.");
            if (labelText != null) labelText.text = "WebcamFeed missing";
            return false;
        }

        if (labelText == null)
        {
            Debug.LogWarning("[YoloDetector_IE] LabelText não atribuído. Detecções não serão exibidas.");
        }

        return true;
    }

    private void InitializeModel()
    {
        try
        {
            // Load ONNX model
            runtimeModel = ModelLoader.Load(modelAsset);
            Debug.Log($"[YoloDetector_IE] Modelo carregado: {modelAsset.name}");

            // Create worker with specified backend
            worker = new Worker(runtimeModel, backendType);
            Debug.Log($"[YoloDetector_IE] Worker criado ({backendType})");

            // Allocate input tensor (NCHW format)
            inputTensor = new Tensor<float>(new TensorShape(1, 3, inputHeight, inputWidth));
            Debug.Log($"[YoloDetector_IE] Tensor de entrada alocado: 1x3x{inputHeight}x{inputWidth}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[YoloDetector_IE] Erro ao inicializar modelo: {e.Message}");
            if (labelText != null) labelText.text = "Erro init modelo";
        }
    }

    private void InitializeLabels()
    {
        YoloLabelLoader.LogEnvironmentInfo();
        labels = YoloLabelLoader.LoadLabels();
        
        if (labels != null && labels.Length > 0)
        {
            Debug.Log($"[YoloDetector_IE] {labels.Length} labels carregadas com sucesso.");
        }
        else
        {
            Debug.LogError("[YoloDetector_IE] Falha ao carregar labels.");
        }
    }

    private void InitializePostProcessor()
    {
        postProcessor = new YoloPostProcessor(labels, confidenceThreshold);
        Debug.Log($"[YoloDetector_IE] Post-processor inicializado (threshold: {confidenceThreshold})");
    }

    private void InitializeDebugUI()
    {
        if (!enableDebugMode) return;

        // Create or get debug UI component
        debugUI = gameObject.AddComponent<YoloDebugUI>();
        debugUI.Initialize();

        // Update initial debug info
        if (debugUI != null)
        {
            string backendType = worker != null ? "GPU" : "CPU";
            debugUI.UpdateModelInfo(modelAsset.name, backendType);
            debugUI.UpdateLabelsInfo(labels?.Length ?? 0, labels != null && labels.Length > 0);
            debugUI.UpdatePathInfo(Application.dataPath);
            debugUI.UpdateAsyncMode(useAsyncReadback, inferenceInterval);
            debugUI.UpdateInferenceState("Idle", Color.gray);
        }
    }

    void Update()
    {
        // Update debug UI
        if (enableDebugMode && debugUI != null)
        {
            debugUI.UpdateFPS();
            
            // Update inference state indicator
            if (isInferenceRunning)
            {
                debugUI.UpdateInferenceState("Running", Color.yellow);
            }
            else
            {
                debugUI.UpdateInferenceState("Idle", Color.green);
            }
            
            if (Time.frameCount % 30 == 0 && webcamFeed != null && webcamFeed.IsPlaying)
            {
                debugUI.UpdateWebcamInfo(webcamFeed.Width, webcamFeed.Height, webcamFeed.VideoRotationAngle);
            }
        }

        // Validate prerequisites
        if (!CanRunInference())
            return;

        // Run inference every N frames to optimize performance
        if (Time.frameCount % inferenceInterval != 0)
            return;

        RunInference();
    }

    private bool CanRunInference()
    {
        if (isInferenceRunning)
        {
            // Skip if previous inference is still running
            return false;
        }

        if (worker == null || runtimeModel == null)
        {
            if (labelText != null) labelText.text = "Modelo não inicializado";
            return false;
        }

        if (webcamFeed == null || !webcamFeed.IsPlaying)
        {
            if (labelText != null) labelText.text = "Aguardando câmera...";
            return false;
        }

        if (labels == null || labels.Length == 0)
        {
            if (labelText != null) labelText.text = "Labels não carregadas";
            return false;
        }

        return true;
    }

    private void RunInference()
    {
        // Mark inference as running
        isInferenceRunning = true;

        // Update debug state
        if (enableDebugMode && debugUI != null)
        {
            debugUI.UpdateInferenceState("Starting", Color.cyan);
        }

        // Start timing
        if (enableDebugMode)
        {
            inferenceStartTime = Time.realtimeSinceStartup;
        }
        
        UnityEngine.Profiling.Profiler.BeginSample("YOLO_ConvertTexture");
        Texture webcamTexture = webcamFeed.WebcamTexture;
        TensorConverter.ConvertTextureToTensor(webcamTexture, inputTensor, inputWidth, inputHeight);
        UnityEngine.Profiling.Profiler.EndSample();

        
        UnityEngine.Profiling.Profiler.BeginSample("YOLO_Schedule");
        // Run inference
        worker.SetInput(inputName, inputTensor);
        worker.Schedule();
        UnityEngine.Profiling.Profiler.EndSample();

        // Update debug state
        if (enableDebugMode && debugUI != null)
        {
            debugUI.UpdateInferenceState("Scheduled", Color.yellow);
        }

        // Use coroutine for delayed processing instead of async readback
        if (useAsyncReadback)
        {
            // Schedule output processing for next frame (allows GPU to finish)
            StartCoroutine(ProcessInferenceDelayed());
        }
        else
        {
            // Synchronous readback (may cause slight lag)
            if (enableDebugMode && debugUI != null)
            {
                debugUI.UpdateInferenceState("Sync Readback", Color.red);
            }

            var outputTensor = worker.PeekOutput() as Tensor<float>;
            if (outputTensor == null)
            {
                if (labelText != null) labelText.text = "Sem output.";
                if (enableDebugMode && debugUI != null)
                {
                    debugUI.SetInferenceError();
                    debugUI.UpdateInferenceState("Error: No Output", Color.red);
                }
                isInferenceRunning = false;
                return;
            }

            using var cpuCopy = outputTensor.ReadbackAndClone();
            ProcessInferenceResult(cpuCopy);
            isInferenceRunning = false;
        }
    }

    private System.Collections.IEnumerator ProcessInferenceDelayed()
    {
        // Update debug state
        if (enableDebugMode && debugUI != null)
        {
            debugUI.UpdateInferenceState("Waiting GPU", Color.magenta);
            yield return null;  // Wait one frame to update Debug UI
        }
        
        // Update debug state
        if (enableDebugMode && debugUI != null)
        {
            debugUI.UpdateInferenceState("Reading Back", Color.cyan);
            yield return null;  // Wait one frame to update Debug UI
        }

        // Now get the output (GPU should be done by now)
        var outputTensor = worker.PeekOutput() as Tensor<float>;
        
        if (outputTensor == null)
        {
            Debug.LogError("[YoloDetector_IE] Output tensor is null after delay!");
            if (labelText != null) labelText.text = "Sem output.";
            if (enableDebugMode && debugUI != null)
            {
                debugUI.SetInferenceError();
                debugUI.UpdateInferenceState("Error: Null Tensor", Color.red);
            }
            isInferenceRunning = false;
            yield break;
        }
        
        // Synchronous readback (but GPU is done, so it's fast)
        Tensor<float> cpuCopy = null;
        try
        {
            // Update debug state
            if (enableDebugMode && debugUI != null)
            {
                debugUI.UpdateInferenceState("Processing", Color.yellow);
            }

            UnityEngine.Profiling.Profiler.BeginSample("YOLO_Readback_SYNC");
            cpuCopy = outputTensor.ReadbackAndClone();
            UnityEngine.Profiling.Profiler.EndSample();
            
            if (cpuCopy != null)
            {
                ProcessInferenceResult(cpuCopy);
                
                // Update debug state on success
                if (enableDebugMode && debugUI != null)
                {
                    debugUI.UpdateInferenceState("Complete", Color.green);
                }
            }
            else
            {
                Debug.LogError("[YoloDetector_IE] Readback returned null!");
                if (labelText != null) labelText.text = "Readback falhou";
                if (enableDebugMode && debugUI != null)
                {
                    debugUI.UpdateInferenceState("Error: Readback Null", Color.red);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[YoloDetector_IE] Error in delayed inference: {e.Message}");
            if (labelText != null) labelText.text = $"Erro: {e.Message}";
            if (enableDebugMode && debugUI != null)
            {
                debugUI.UpdateInferenceState($"Error: {e.Message.Substring(0, System.Math.Min(20, e.Message.Length))}", Color.red);
            }
        }
        finally
        {
            cpuCopy?.Dispose();
            isInferenceRunning = false;
        }
    }

    private void ProcessInferenceResult(Tensor<float> cpuCopy)
    {
        // Update debug info
        UpdateDebugInfo(cpuCopy);

        // Process detections
        var result = postProcessor.ProcessDetections(cpuCopy);

        // Update detection count debug
        if (enableDebugMode && debugUI != null && Time.frameCount % 10 == 0)
        {
            int numBoxes = cpuCopy.shape[2];
            debugUI.UpdateDetectionCount(result.TotalDetections, numBoxes);
        }

        // Display result
        if (labelText != null)
        {
            labelText.text = postProcessor.FormatDetectionText(result);
        }
    }

    private void UpdateDebugInfo(Tensor<float> outputTensor)
    {
        if (!enableDebugMode || debugUI == null)
            return;

        // Update inference time
        float inferenceTime = (Time.realtimeSinceStartup - inferenceStartTime) * 1000f;
        if (Time.frameCount % 10 == 0)
        {
            debugUI.UpdateInferenceTime(inferenceTime);
        }

        // Update tensor info
        if (Time.frameCount % 30 == 0)
        {
            debugUI.UpdateTensorInfo(outputTensor.shape[0], outputTensor.shape[1], outputTensor.shape[2]);
        }
    }

    public void SwitchCamera()
    {
        if (webcamFeed != null)
        {
            webcamFeed.SwitchCamera();
        }
    }

    public void SetConfidenceThreshold(float threshold)
    {
        confidenceThreshold = Mathf.Clamp(threshold, 0.1f, 0.9f);
        postProcessor = new YoloPostProcessor(labels, confidenceThreshold);
        Debug.Log($"[YoloDetector_IE] Confidence threshold atualizado para: {confidenceThreshold}");
        
        // Update debug UI
        if (enableDebugMode && debugUI != null)
        {
            debugUI.UpdateAsyncMode(useAsyncReadback, inferenceInterval);
        }
    }

    void OnDestroy()
    {
        // Dispose of inference resources
        inputTensor?.Dispose();
        worker?.Dispose();

        // Cleanup tensor converter resources
        TensorConverter.Cleanup();

        Debug.Log("[YoloDetector_IE] Recursos liberados.");
    }
}