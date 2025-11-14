using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Handles loading YOLO class labels from various file locations
/// </summary>
public class YoloLabelLoader
{
    private const string LABEL_FILENAME = "classes.txt";

    public static string[] LoadLabels()
    {
        string[] labels = null;

        // Try primary path: Resources folder
        labels = TryLoadFromResources();
        if (labels != null && labels.Length > 0)
            return labels;

        // Try secondary path: Application.dataPath/Resources
        labels = TryLoadFromDataPath();
        if (labels != null && labels.Length > 0)
            return labels;

        // Try tertiary path: StreamingAssets
        labels = TryLoadFromStreamingAssets();
        if (labels != null && labels.Length > 0)
            return labels;

        // Fallback to default labels
        Debug.LogWarning("[YoloLabelLoader] Nenhum arquivo de labels encontrado. Usando labels padrão.");
        return GetDefaultLabels();
    }

    private static string[] TryLoadFromResources()
    {
        try
        {
            TextAsset labelAsset = Resources.Load<TextAsset>("classes");
            if (labelAsset != null)
            {
                string[] labels = labelAsset.text.Split('\n');
                labels = CleanLabels(labels);
                Debug.Log($"[YoloLabelLoader] {labels.Length} labels carregadas via Resources.Load");
                LogSampleLabels(labels);
                return labels;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[YoloLabelLoader] Erro ao carregar via Resources.Load: {ex.Message}");
        }
        return null;
    }

    private static string[] TryLoadFromDataPath()
    {
        try
        {
            string path = Path.Combine(Application.dataPath, "Resources", LABEL_FILENAME);
            Debug.Log($"[YoloLabelLoader] Tentando carregar de: {path}");

            if (File.Exists(path))
            {
                string[] labels = File.ReadAllLines(path);
                labels = CleanLabels(labels);
                Debug.Log($"[YoloLabelLoader] {labels.Length} labels carregadas de DataPath");
                LogSampleLabels(labels);
                return labels;
            }
            else
            {
                Debug.LogWarning($"[YoloLabelLoader] Arquivo não encontrado: {path}");
                LogResourcesDirectory();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[YoloLabelLoader] Erro ao carregar de DataPath: {ex.Message}");
        }
        return null;
    }

    private static string[] TryLoadFromStreamingAssets()
    {
        try
        {
            string streamingPath = Path.Combine(Application.streamingAssetsPath, LABEL_FILENAME);
            Debug.Log($"[YoloLabelLoader] Tentando StreamingAssets: {streamingPath}");

            if (File.Exists(streamingPath))
            {
                string[] labels = File.ReadAllLines(streamingPath);
                labels = CleanLabels(labels);
                Debug.Log($"[YoloLabelLoader] {labels.Length} labels carregadas de StreamingAssets");
                LogSampleLabels(labels);
                return labels;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[YoloLabelLoader] Erro ao carregar de StreamingAssets: {ex.Message}");
        }
        return null;
    }

    private static string[] CleanLabels(string[] rawLabels)
    {
        // Remove empty lines and trim whitespace
        string[] cleaned = Array.FindAll(rawLabels, s => !string.IsNullOrWhiteSpace(s));
        for (int i = 0; i < cleaned.Length; i++)
        {
            cleaned[i] = cleaned[i].Trim();
        }
        return cleaned;
    }

    private static void LogSampleLabels(string[] labels)
    {
        int samplesToLog = Math.Min(5, labels.Length);
        for (int i = 0; i < samplesToLog; i++)
        {
            Debug.Log($"[YoloLabelLoader] Label {i}: {labels[i]}");
        }
    }

    private static void LogResourcesDirectory()
    {
        try
        {
            string resourcesPath = Path.Combine(Application.dataPath, "Resources");
            if (Directory.Exists(resourcesPath))
            {
                Debug.Log("[YoloLabelLoader] Arquivos em Resources:");
                string[] files = Directory.GetFiles(resourcesPath);
                foreach (string file in files)
                {
                    Debug.Log($"[YoloLabelLoader] - {Path.GetFileName(file)}");
                }
            }
            else
            {
                Debug.LogWarning($"[YoloLabelLoader] Diretório Resources não existe: {resourcesPath}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[YoloLabelLoader] Erro ao listar arquivos: {ex.Message}");
        }
    }

    private static string[] GetDefaultLabels()
    {
        // COCO dataset default classes (first 9 for fallback)
        return new string[]
        {
            "person", "bicycle", "car", "motorcycle", "airplane",
            "bus", "train", "truck", "boat"
        };
    }

    public static void LogEnvironmentInfo()
    {
        Debug.Log($"[YoloLabelLoader] Platform: {Application.platform}");
        Debug.Log($"[YoloLabelLoader] Application.dataPath = {Application.dataPath}");
        Debug.Log($"[YoloLabelLoader] Application.streamingAssetsPath = {Application.streamingAssetsPath}");
        Debug.Log($"[YoloLabelLoader] Application.persistentDataPath = {Application.persistentDataPath}");
    }
}
