using Unity.InferenceEngine;

/// <summary>
/// Processes YOLO detection output and extracts best detection
/// </summary>
public class YoloPostProcessor
{
    public struct DetectionResult
    {
        public int BestClassIndex;
        public float BestScore;
        public string BestLabel;
        public int TotalDetections;
        public bool HasDetection => BestClassIndex >= 0;
    }

    private readonly string[] labels;
    private readonly float confidenceThreshold;

    public YoloPostProcessor(string[] labels, float confidenceThreshold = 0.25f)
    {
        this.labels = labels;
        this.confidenceThreshold = confidenceThreshold;
    }

    /// <summary>
    /// Processes YOLO output tensor and returns the best detection
    /// </summary>
    public DetectionResult ProcessDetections(Tensor<float> outputTensor)
    {
        DetectionResult result = new DetectionResult
        {
            BestClassIndex = -1,
            BestScore = 0f,
            BestLabel = "",
            TotalDetections = 0
        };

        int numAttrs = outputTensor.shape[1];  // Number of attributes (x, y, w, h, class scores...)
        int numBoxes = outputTensor.shape[2];  // Number of detection boxes

        // Loop over all detection boxes
        for (int i = 0; i < numBoxes; i++)
        {
            // Get bounding box coordinates (not used for now, but available)
            // float x = outputTensor[0, 0, i]; // center x (normalized)
            // float y = outputTensor[0, 1, i]; // center y (normalized)
            // float w = outputTensor[0, 2, i]; // width
            // float h = outputTensor[0, 3, i]; // height

            // Find class with highest confidence
            int bestClassIdx = -1;
            float maxScore = 0f;

            for (int c = 4; c < numAttrs; c++)
            {
                float score = outputTensor[0, c, i];
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

                string label = GetLabelForClass(bestClassIdx);

                // Track the best (highest confidence) detection
                if (maxScore > result.BestScore)
                {
                    result.BestScore = maxScore;
                    result.BestClassIndex = bestClassIdx;
                    result.BestLabel = label;
                }
            }
        }

        return result;
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
