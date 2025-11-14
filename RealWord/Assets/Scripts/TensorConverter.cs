using UnityEngine;
using Unity.InferenceEngine;

/// <summary>
/// Converts Unity textures to tensors for neural network inference
/// OPTIMIZED: Uses GPU-based conversion to avoid CPU stalls
/// </summary>
public static class TensorConverter
{
    // Reusable RenderTexture to avoid allocations
    private static RenderTexture resizedRT;
    private static int currentWidth = -1;
    private static int currentHeight = -1;

    /// <summary>
    /// Converts a texture to a tensor using GPU-accelerated conversion (NO CPU READBACK)
    /// This prevents webcam freezing by keeping everything on GPU
    /// </summary>
    public static void ConvertTextureToTensor(Texture sourceTexture, Tensor<float> targetTensor, int width, int height)
    {
        if (sourceTexture == null)
        {
            Debug.LogError("[TensorConverter] Source texture is null!");
            return;
        }

        if (targetTensor == null)
        {
            Debug.LogError("[TensorConverter] Target tensor is null!");
            return;
        }

        try
        {
            // First resize the texture on GPU if needed
            RenderTexture resized = GetOrCreateResizedTexture(sourceTexture, width, height);
            
            if (resized == null)
            {
                Debug.LogError("[TensorConverter] Failed to create/get resized RenderTexture!");
                return;
            }
            
            // Use built-in GPU converter (no CPU readback = smooth webcam)
            TextureConverter.ToTensor(resized, targetTensor,
                new TextureTransform().SetTensorLayout(TensorLayout.NCHW));
            
            // Debug: Log successful conversion occasionally
            if (Time.frameCount % 100 == 0)
            {
                Debug.Log($"[TensorConverter] GPU conversion successful (frame {Time.frameCount})");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[TensorConverter] GPU conversion failed: {e.Message}\n{e.StackTrace}");
            Debug.LogWarning("[TensorConverter] Falling back to CPU conversion...");
            
            // Fallback to manual conversion (will cause some lag)
            ConvertTextureToTensorFallback(sourceTexture, targetTensor, width, height);
        }
    }

    /// <summary>
    /// Resize texture on GPU without CPU readback
    /// </summary>
    private static RenderTexture GetOrCreateResizedTexture(Texture source, int targetWidth, int targetHeight)
    {
        // Reuse RenderTexture if dimensions match (avoids allocations)
        if (resizedRT == null || currentWidth != targetWidth || currentHeight != targetHeight)
        {
            if (resizedRT != null)
            {
                resizedRT.Release();
            }
            
            resizedRT = new RenderTexture(targetWidth, targetHeight, 0, RenderTextureFormat.ARGB32);
            resizedRT.enableRandomWrite = true;
            resizedRT.Create();
            currentWidth = targetWidth;
            currentHeight = targetHeight;
        }

        // GPU-only resize (no CPU involvement = no freezing)
        Graphics.Blit(source, resizedRT);
        return resizedRT;
    }

    /// <summary>
    /// Cleanup method to release resources
    /// </summary>
    public static void Cleanup()
    {
        if (resizedRT != null)
        {
            resizedRT.Release();
            resizedRT = null;
        }
    }


    /// <summary>
    /// Manual texture to tensor conversion (fallback method)
    /// WARNING: This causes webcam freezing due to CPU readback!
    /// Only used if GPU conversion fails
    /// </summary>
    private static void ConvertTextureToTensorFallback(Texture sourceTexture, Tensor<float> targetTensor, int width, int height)
    {
        Debug.LogWarning("[TensorConverter] Using slow CPU fallback - webcam may freeze!");
        
        WebCamTexture webcamTex = sourceTexture as WebCamTexture;
        if (webcamTex == null)
        {
            Debug.LogWarning("[TensorConverter] Source texture is not a WebCamTexture. Conversion may fail.");
            return;
        }

        // Create temporary Texture2D (expensive!)
        Texture2D tempTexture = new Texture2D(width, height, TextureFormat.RGB24, false);

        // Use RenderTexture to copy webcam texture
        RenderTexture rt = RenderTexture.GetTemporary(width, height);
        Graphics.Blit(webcamTex, rt);
        
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture.active = rt;
        
        // THIS CAUSES THE FREEZE - synchronous GPU->CPU transfer
        tempTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tempTexture.Apply();
        
        RenderTexture.active = previousActive;
        RenderTexture.ReleaseTemporary(rt);

        // Convert pixels to tensor format (NCHW)
        Color[] pixels = tempTexture.GetPixels(); // Another expensive CPU operation
        
        // NCHW format: all R, then all G, then all B
        int pixelIndex = 0;
        for (int c = 0; c < 3; c++) // RGB channels
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color pixel = pixels[y * width + x];
                    float value = (c == 0) ? pixel.r : (c == 1) ? pixel.g : pixel.b;
                    targetTensor[pixelIndex++] = value;
                }
            }
        }

        // Clean up temporary texture
        Object.Destroy(tempTexture);
    }

    /// <summary>
    /// Converts texture with automatic resizing/scaling to match target dimensions
    /// DEPRECATED: Use ConvertTextureToTensor instead (now handles resizing automatically)
    /// </summary>
    public static void ConvertTextureToTensorWithResize(Texture sourceTexture, Tensor<float> targetTensor, int targetWidth, int targetHeight)
    {
        ConvertTextureToTensor(sourceTexture, targetTensor, targetWidth, targetHeight);
    }
}

