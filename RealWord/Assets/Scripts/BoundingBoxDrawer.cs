using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BoundingBoxDrawer : MonoBehaviour
{
    public RectTransform webcamRawImage;
    public GameObject bboxPrefab;

    private readonly List<GameObject> activeBoxes = new List<GameObject>();

    public void DrawBoxes(List<Detection> detections)
    {
        foreach (var b in activeBoxes)
            Destroy(b);
        activeBoxes.Clear();

        if (detections == null || detections.Count == 0)
            return;

        float imgW = webcamRawImage.rect.width;
        float imgH = webcamRawImage.rect.height;

        foreach (var d in detections)
        {
            float x = d.rect.x * imgW;
            float y = d.rect.y * imgH;
            float w = d.rect.width * imgW;
            float h = d.rect.height * imgH;

            GameObject box = Instantiate(bboxPrefab, webcamRawImage);
            RectTransform rt = box.GetComponent<RectTransform>();

            // GARANTE local space
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);

            rt.sizeDelta = new Vector2(w, h);

            // Converte para coordenadas de UI
            float px = x + w / 2f;
            float py = imgH - (y + h / 2f);

            // OBRIGATÓRIO: UI usa anchoredPosition
            rt.anchoredPosition = new Vector2(px, py);

            activeBoxes.Add(box);

            var txt = box.GetComponentInChildren<Text>();
            if (txt) txt.text = $"{d.label} ({d.score * 100f:F1}%)";
        }
    }
}
