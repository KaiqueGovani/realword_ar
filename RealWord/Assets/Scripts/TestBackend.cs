using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// DTO for sentence generation request.
/// Matches the backend CreateSentenceDto structure.
/// </summary>
[System.Serializable]
public class SentenceRequestDto
{
    public string @object;
    public string language;

    public SentenceRequestDto(string objectName, string language)
    {
        this.@object = objectName;
        this.language = language;
    }
}

/// <summary>
/// DTO for sentence generation response.
/// Matches the backend SentencesDto structure.
/// </summary>
[System.Serializable]
public class SentencesResponseDto
{
    public string[] phrases;
    public string[] translations;
}

public class TestBackend : MonoBehaviour
{
    private const string API_URL = "http://localhost:3000/sentences";

    void Start()
    {
        StartCoroutine(SendRequest("chair", "portuguese"));
    }

    /// <summary>
    /// Sends a POST request to the backend API to generate sentences for an object.
    /// </summary>
    /// <param name="objectName">The name of the object to generate sentences for</param>
    /// <param name="language">The target language for translations</param>
    IEnumerator SendRequest(string objectName, string language)
    {
        // Create request DTO
        SentenceRequestDto requestDto = new SentenceRequestDto(objectName, language);
        
        // Serialize DTO to JSON
        string jsonBody = JsonUtility.ToJson(requestDto);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);

        using (UnityWebRequest request = new UnityWebRequest(API_URL, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("✅ Request successful!");
                Debug.Log("Raw Response: " + request.downloadHandler.text);

                // Deserialize response to DTO
                SentencesResponseDto responseDto = JsonUtility.FromJson<SentencesResponseDto>(request.downloadHandler.text);

                // Process response data
                for (int i = 0; i < responseDto.phrases.Length; i++)
                {
                    Debug.Log($"EN: {responseDto.phrases[i]}");
                    Debug.Log($"PT: {responseDto.translations[i]}");
                }
            }
            else
            {
                Debug.LogError("❌ Request failed: " + request.error);
            }
        }
    }
}