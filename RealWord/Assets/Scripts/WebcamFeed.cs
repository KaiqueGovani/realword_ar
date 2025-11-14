using UnityEngine;
using UnityEngine.UI;

public class WebcamFeed : MonoBehaviour
{
    [Header("Display Settings")]
    [SerializeField] public RawImage display;
    [SerializeField] private AspectRatioFitter aspectFitter;

    [Header("Camera Settings")]
    [SerializeField] private bool useFrontCamera = false;
    [SerializeField] private int cameraWidth = 720;
    [SerializeField] private int cameraHeight = 1440;
    [SerializeField] private int targetFPS = 30;

    [Header("Auto Create Display")]
    [SerializeField] private bool autoCreateDisplay = true;
    [SerializeField] private int displaySortingOrder = -1;

    private WebCamTexture camTex;
    private Canvas cameraCanvas;

    public Texture WebcamTexture => camTex;
    public bool IsPlaying => camTex != null && camTex.isPlaying;
    public int Width => camTex != null ? camTex.width : 0;
    public int Height => camTex != null ? camTex.height : 0;
    public int VideoRotationAngle => camTex != null ? camTex.videoRotationAngle : 0;
    public bool VideoVerticallyMirrored => camTex != null && camTex.videoVerticallyMirrored;

    void Start()
    {
        InitializeCamera();
    }

    void InitializeCamera()
    {
        try
        {
            Debug.Log("[WebcamFeed] Initializing camera...");

            // Find the desired camera (rear or front)
            WebCamDevice[] devices = WebCamTexture.devices;
            
            if (devices.Length == 0)
            {
                Debug.LogError("[WebcamFeed] Nenhuma webcam encontrada");
                return;
            }

            string selectedDevice = "";
            foreach (var device in devices)
            {
                if (device.isFrontFacing == useFrontCamera)
                {
                    selectedDevice = device.name;
                    Debug.Log($"[WebcamFeed] Selected camera: {selectedDevice} (Front: {device.isFrontFacing})");
                    break;
                }
            }

            // Create camera texture
            if (!string.IsNullOrEmpty(selectedDevice))
            {
                camTex = new WebCamTexture(selectedDevice, cameraWidth, cameraHeight, targetFPS);
            }
            else
            {
                camTex = new WebCamTexture(cameraWidth, cameraHeight, targetFPS);
                Debug.LogWarning("[WebcamFeed] Using default camera");
            }

            // Start camera
            camTex.Play();

            // Setup display
            if (autoCreateDisplay || display != null)
            {
                SetupCameraDisplay();
            }

            Debug.Log($"[WebcamFeed] Camera initialized: {camTex.width}x{camTex.height} @ {camTex.requestedFPS}fps");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[WebcamFeed] Camera initialization failed: {e.Message}");
        }
    }

    void SetupCameraDisplay()
    {
        // Create canvas for camera display if auto-creating
        if (autoCreateDisplay && cameraCanvas == null)
        {
            GameObject canvasGO = new GameObject("CameraCanvas");
            cameraCanvas = canvasGO.AddComponent<Canvas>();
            cameraCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cameraCanvas.sortingOrder = displaySortingOrder;
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        // Create or configure camera display
        if (display == null && autoCreateDisplay)
        {
            GameObject displayGO = new GameObject("CameraDisplay");
            displayGO.transform.SetParent(cameraCanvas.transform, false);

            display = displayGO.AddComponent<RawImage>();
            display.texture = camTex;

            // Fill entire screen
            RectTransform rect = displayGO.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            ConfigureDisplayRotation(rect);
        }
        else if (display != null)
        {
            display.texture = camTex;

            if (aspectFitter != null && camTex.width > 0)
            {
                aspectFitter.aspectRatio = (float)camTex.width / camTex.height;
            }

            // Configure rotation for manually assigned display
            RectTransform rect = display.GetComponent<RectTransform>();
            if (rect != null)
            {
                ConfigureDisplayRotation(rect);
            }
        }
    }

    void ConfigureDisplayRotation(RectTransform rect)
    {
        if (camTex == null) return;

        int rotation = camTex.videoRotationAngle;
        bool verticallyMirrored = camTex.videoVerticallyMirrored;

        Debug.Log($"[WebcamFeed] Camera rotation: {rotation}, mirrored: {verticallyMirrored}");

        // Adjust rotation based on camera orientation
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
    }

    public void SwitchCamera()
    {
        useFrontCamera = !useFrontCamera;

        if (camTex != null)
        {
            camTex.Stop();
            camTex = null;
        }

        InitializeCamera();
    }

    public void SetCameraPreference(bool frontCamera)
    {
        if (useFrontCamera != frontCamera)
        {
            SwitchCamera();
        }
    }

    void Update()
    {
        // Update display texture if needed
        if (display != null && camTex != null && camTex.isPlaying)
        {
            display.texture = camTex;
        }
    }

    void OnDestroy()
    {
        if (camTex != null && camTex.isPlaying)
        {
            camTex.Stop();
        }

        if (cameraCanvas != null && cameraCanvas.gameObject != null)
        {
            Destroy(cameraCanvas.gameObject);
        }
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (camTex != null)
        {
            if (pauseStatus)
            {
                camTex.Pause();
            }
            else
            {
                camTex.Play();
            }
        }
    }
}
