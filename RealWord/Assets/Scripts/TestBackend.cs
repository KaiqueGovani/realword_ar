using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

[System.Serializable]
public class SentenceResponse
{
    public string[] phrases;
    public string[] translations;
}

public class TestBackend : MonoBehaviour
{
    private string apiUrl = "http://localhost:3000/sentences";

    void Start()
    {
        StartCoroutine(SendRequest("chair", "portuguese"));
    }

    IEnumerator SendRequest(string objectName, string language)
    {
        string jsonBody = $"{{\"object\":\"{objectName}\",\"language\":\"{language}\"}}";
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);

        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("✅ Request successful!");
                Debug.Log("Raw Response: " + request.downloadHandler.text);

                SentenceResponse response = JsonUtility.FromJson<SentenceResponse>(request.downloadHandler.text);

                for (int i = 0; i < response.phrases.Length; i++)
                {
                    Debug.Log($"EN: {response.phrases[i]}");
                    Debug.Log($"PT: {response.translations[i]}");
                }
            }
            else
            {
                Debug.LogError("❌ Request failed: " + request.error);
            }
        }
    }
}