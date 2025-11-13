using UnityEngine;
using UnityEngine.UI;

public class WebcamFeed : MonoBehaviour
{
    [SerializeField] public RawImage display;
    [SerializeField] private AspectRatioFitter aspectFitter;



    private WebCamTexture camTex;
    public Texture WebcamTexture => camTex;

    void Start()
    {
        if (display == null) Debug.LogError("WebcamFeed: arraste o RawImage no Inspector.");
        if (WebCamTexture.devices.Length == 0) { Debug.LogError("Nenhuma webcam"); return; }

        camTex = new WebCamTexture(WebCamTexture.devices[0].name);
        camTex.Play();
        display.texture = camTex;

        if (aspectFitter != null && camTex.width > 0)
            aspectFitter.aspectRatio = (float)camTex.width / camTex.height;
    }

    void OnDestroy()
    {
        if (camTex != null && camTex.isPlaying) camTex.Stop();
    }
}
