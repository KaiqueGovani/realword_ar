using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.InferenceEngine;
using UnityEngine;

/// <summary>
/// Processes YOLO detection output and extracts best detection
/// 
/// PERFORMANCE OPTIMIZATIONS:
/// - Uses DownloadToArray() to download tensor data once instead of individual indexing
///   (reduces ~672,000 individual tensor accesses to 1 bulk copy for YOLOv8: 8400 boxes × 80 classes)
/// - Provides async processing to spread computation across multiple frames without blocking
/// - Expected improvement: 500ms blocking → 5-10ms per frame for incremental processing
/// </summary>
public class YoloPostProcessor
{
    public struct DetectionResult
    {
        public int BestClassIndex;
        public float BestScore;
        public string BestLabel;
        public int TotalDetections;

        public List<Detection> ValidDetections; // todas as detecções válidas
        public bool HasDetection => BestClassIndex >= 0;
    }

    private readonly string[] labels;
    private readonly float confidenceThreshold;
    private readonly int boxesPerFrame;

    /// <summary>
    /// Initialize post-processor
    /// </summary>
    /// <param name="labels">Class labels array</param>
    /// <param name="confidenceThreshold">Minimum confidence for detections</param>
    /// <param name="boxesPerFrame">Number of boxes to process per frame before yielding (default: 2000)</param>
    public YoloPostProcessor(string[] labels, float confidenceThreshold = 0.25f, int boxesPerFrame = 2000)
    {
        this.labels = labels;
        this.confidenceThreshold = confidenceThreshold;
        this.boxesPerFrame = boxesPerFrame > 0 ? boxesPerFrame : 2000;
    }

    /// <summary>
    /// Processes YOLO output tensor asynchronously, spreading computation across multiple frames
    /// This method yields control back to Unity every 'boxesPerFrame' boxes processed
    /// 
    /// Usage: var result = await postProcessor.ProcessDetectionsAsync(tensor);
    /// 
    /// NOTE: The input tensor should already be a CPU copy (from ReadbackAndClone/Async)
    /// </summary>
    public async Task<DetectionResult> ProcessDetectionsAsync(Tensor<float> outputTensor)
    {
        DetectionResult result = new DetectionResult
        {
            BestClassIndex = -1,
            BestScore = 0f,
            BestLabel = "",
            TotalDetections = 0,
            ValidDetections = new List<Detection>()
        };

        int numAttrs = outputTensor.shape[1];  // Number of attributes (x, y, w, h, class scores...)
        int numBoxes = outputTensor.shape[2];  // Number of detection boxes

        // Download entire tensor to CPU array once (much faster than individual indexing)
        // Tensor is in shape [1, numAttrs, numBoxes]
        float[] tensorData = outputTensor.DownloadToArray();

        // Calculate stride for accessing data
        // Data layout: [batch=0][attr=0..numAttrs-1][box=0..numBoxes-1]
        int attrStride = numBoxes;

        // Process all boxes, yielding every boxesPerFrame iterations
        for (int startBox = 0; startBox < numBoxes; startBox += boxesPerFrame)
        {
            int endBox = UnityEngine.Mathf.Min(startBox + boxesPerFrame, numBoxes);

            // Process this chunk of boxes
            for (int i = startBox; i < endBox; i++)
            {
                // ---> LEITURA DAS COORDENADAS
                float x = tensorData[0 * attrStride + i];
                float y = tensorData[1 * attrStride + i];
                float w = tensorData[2 * attrStride + i];
                float h = tensorData[3 * attrStride + i];

                float nx = x / 640f;   // já bate com seu modelo
                float ny = y / 640f;
                float nw = w / 640f;
                float nh = h / 640f;

                // Find class with highest confidence for this box
                // Classes start at index 4 (after x, y, w, h)
                int bestClassIdx = -1;
                float maxScore = 0f;

                // Iterate through all class scores for this box
                for (int c = 4; c < numAttrs; c++)
                {
                    // Calculate flat array index: [0, c, i] = c * attrStride + i
                    int idx = c * attrStride + i;
                    float score = tensorData[idx];
                    
                    if (score > maxScore)
                    {
                        maxScore = score;
                        bestClassIdx = c - 4; // Subtract 4 because first 4 are bbox coords
                    }
                }

                // Check if detection meets confidence threshold
                if (maxScore > confidenceThreshold)
                {
                    result.TotalDetections++;

                    // Track the best (highest confidence) detection
                    if (maxScore > result.BestScore)
                    {
                        result.BestScore = maxScore;
                        result.BestClassIndex = bestClassIdx;
                        result.BestLabel = GetLabelForClass(bestClassIdx);
                    }

                    // ---> ADICIONA A DETECÇÃO COMPLETA
                    Detection det = new Detection();
                    det.label = GetLabelForClass(bestClassIdx);
                    det.score = maxScore;

                    // YOLO coord center → RECT top-left
                    det.rect = new Rect(
                        nx - nw / 2f,
                        ny - nh / 2f,
                        nw,
                        nh
                        );

                    result.ValidDetections.Add(det);

                }
            }

            // Yield control back to Unity after processing this chunk
            // This prevents blocking the main thread
            if (endBox < numBoxes)
            {
                await Task.Yield();
            }
        }

        return result;
    }

    /// <summary>
    /// Synchronous version - processes all boxes immediately (may cause frame drops)
    /// Use ProcessDetectionsAsync() instead for better performance
    /// </summary>
    public DetectionResult ProcessDetections(Tensor<float> outputTensor)
    {
        // Simple wrapper that runs async version synchronously
        return ProcessDetectionsAsync(outputTensor).Result;
    }

    private string GetLabelForClass(int classIndex)
    {
        if (labels != null && classIndex >= 0 && classIndex < labels.Length)
        {
            return labels[classIndex];
        }
        return $"cls {classIndex}";
    }

    public string FormatDetectionText(DetectionResult result)
    {
        if (result.HasDetection)
        {
            return $"Detectado: {result.BestLabel} ({result.BestScore * 100f:F1}%)";
        }
        return "Nenhum objeto detectado.";
    }
}
