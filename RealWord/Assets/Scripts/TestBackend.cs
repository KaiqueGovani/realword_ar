using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class TestBackend : MonoBehaviour
{
    [Header("Server")]
    [SerializeField] private string serverUrl = "http://localhost:3000/sentences";

    [Header("Test data")]
    [SerializeField] private string testObject = "mug";

    public void SendTestRequest()
    {
        StartCoroutine(SendRequestCoroutine());
    }

    private IEnumerator SendRequestCoroutine()
    {
        string jsonBody = "{\"object\":\"" + EscapeJsonString(testObject) + "\"}";

        UnityWebRequest request = new UnityWebRequest(serverUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        Debug.Log("[TestBackend] Sending: " + jsonBody);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string respText = request.downloadHandler.text;
            Debug.Log("✅ Response raw: " + respText);

            ResponseData resp = JsonUtility.FromJson<ResponseData>(respText);
            if (resp != null && resp.sentences != null && resp.sentences.Length > 0)
            {
                Debug.Log("[TestBackend] Parsed sentences:");
                foreach (var s in resp.sentences)
                    Debug.Log(" - " + s);
            }
            else
            {
                Debug.LogWarning("[TestBackend] No sentences parsed (check server JSON format).");
            }
        }
        else
        {
            Debug.LogError("❌ Request error: " + request.error + " | httpCode: " + request.responseCode);
        }
    }

    private string EscapeJsonString(string s)
    {
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    [System.Serializable]
    private class ResponseData
    {
        public string[] sentences;
    }
}