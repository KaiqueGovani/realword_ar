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
    public WebcamFeed webcamFeed;
    public Text labelText;

    [Header("Input")]
    public int inputWidth = 640;
    public int inputHeight = 640;


    private Model runtimeModel;
    private Worker worker;
    private Tensor<float> inputTensor;
    private readonly string inputName = "images";

    private string[] labels;

    void Start()
    {
        if (modelAsset == null)
        {
            Debug.LogError("Arraste o ModelAsset do .onnx no Inspector.");
            labelText.text = "Modelo missing";
            return;
        }
        if (webcamFeed == null)
        {
            Debug.LogError("Arraste WebcamFeed no Inspector.");
            return;
        }

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
        }
        catch (Exception e)
        {
            Debug.LogError("[YoloDetector_IE] Erro de inicialização: " + e);
            labelText.text = "Erro init";
        }
    }

    void LoadLabelsFromFile()
    {
        try
        {
            string path = Path.Combine(Application.dataPath, "Resources/classes.txt");
            if (File.Exists(path))
            {
                labels = File.ReadAllLines(path);
                Debug.Log($"[YoloDetector_IE] {labels.Length} labels carregadas.");
            }
            else
            {
                Debug.LogWarning($"Arquivo de labels não encontrado em: {path}");
                labels = new string[0];
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[YoloDetector_IE] Falha ao ler labels: " + ex.Message);
            labels = new string[0];
        }
    }

    void Update()
    {
        if (worker == null || runtimeModel == null) return;
        if (webcamFeed.WebcamTexture == null) return;
        if (labels == null || labels.Length == 0)
        {
            labelText.text = "Labels não carregadas.";
            return;
        }

        // roda a cada 6 frames para não sobrecarregar
        if (Time.frameCount % 6 != 0) return;

        try
        {
            TextureConverter.ToTensor(webcamFeed.WebcamTexture, inputTensor,
                new TextureTransform().SetTensorLayout(TensorLayout.NCHW));
        }
        catch
        {
            CopyWebcamToTensorFallback(webcamFeed.WebcamTexture, inputTensor);
        }

        // roda inferência
        worker.SetInput(inputName, inputTensor);
        worker.Schedule();

        // lê saída
        var outTensor = worker.PeekOutput() as Tensor<float>;
        if (outTensor == null)
        {
            labelText.text = "Sem output.";
            return;
        }

        using var cpuCopy = outTensor.ReadbackAndClone();

        int numAttrs = cpuCopy.shape[1];
        int numBoxes = cpuCopy.shape[2];
        float confThreshold = 0.25f;

        int bestClass = -1;
        float bestScore = 0f;
        string bestLabel = "";

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

        // Mostra o label do objeto mais confiante
        if (bestClass >= 0)
            labelText.text = $"Detectado: {bestLabel} ({bestScore * 100f:F1}%)";
        else
            labelText.text = "Nenhum objeto detectado.";
    }

    void CopyWebcamToTensorFallback(Texture webTex, Tensor<float> tensor)
    {
        var wtex = webcamFeed.WebcamTexture;
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

    void OnDestroy()
    {
        inputTensor?.Dispose();
        worker?.Dispose();
    }
}