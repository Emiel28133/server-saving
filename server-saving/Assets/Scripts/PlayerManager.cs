using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

[System.Serializable]
public class PlayerData {
    public int money;
    public int level;
}

public class PlayerManager : MonoBehaviour {
    [Header("Server Config")]
    public string baseUrl = "http://localhost:3000/player";
    public string playerName = "Alice";

    [Header("Player Data")]
    public PlayerData currentData;

    void Start() {
        // Haal spelerdata bij start
        StartCoroutine(LoadData());
    }

    // ----- Ophalen -----
    IEnumerator LoadData() {
        UnityWebRequest req = UnityWebRequest.Get($"{baseUrl}/{playerName}");
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success) {
            currentData = JsonUtility.FromJson<PlayerData>(req.downloadHandler.text);
            Debug.Log($"📥 Loaded: Money={currentData.money}, Level={currentData.level}");
        } else {
            Debug.LogError("❌ Load error: " + req.error);
        }
    }

    // ----- Opslaan -----
    public void SaveData() {
        StartCoroutine(SaveDataCoroutine());
    }

    IEnumerator SaveDataCoroutine() {
        string json = JsonUtility.ToJson(currentData);
        UnityWebRequest req = new UnityWebRequest($"{baseUrl}/{playerName}", "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success) {
            Debug.Log($"💾 Saved: Money={currentData.money}, Level={currentData.level}");
        } else {
            Debug.LogError("❌ Save error: " + req.error);
        }
    }
}