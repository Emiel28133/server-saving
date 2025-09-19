/*using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

[System.Serializable]
public class PlayerData {
    public int id;
    public string name;
    public int money;
}

// Wrapper om array te kunnen parsen
[System.Serializable]
public class PlayerDataList {
    public List<PlayerData> players;
}

public class ServerTest : MonoBehaviour {
    string baseUrl = "http://localhost:3000/player";
    string listUrl = "http://localhost:3000/players";

    string playerName = "Alice"; // kun je aanpassen per speler

    void Start() {
        // Opslaan en ophalen van 1 speler
        StartCoroutine(SaveMoney(250));
        StartCoroutine(LoadMoney());

        // Ophalen van ALLE spelers
        StartCoroutine(LoadAllPlayers());
    }

    IEnumerator SaveMoney(int amount) {
        PlayerData data = new PlayerData { name = playerName, money = amount };
        string json = JsonUtility.ToJson(data);

        UnityWebRequest req = new UnityWebRequest(baseUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success) {
            Debug.Log($"💾 Money saved for {playerName}: {amount}");
        } else {
            Debug.LogError("❌ Save error: " + req.error);
        }
    }

    IEnumerator LoadMoney() {
        UnityWebRequest req = UnityWebRequest.Get($"{baseUrl}/{playerName}");
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success) {
            PlayerData data = JsonUtility.FromJson<PlayerData>(req.downloadHandler.text);
            Debug.Log($"📥 Money loaded for {data.name}: {data.money}");
        } else {
            Debug.LogError("❌ Load error: " + req.error);
        }
    }

    IEnumerator LoadAllPlayers() {
        UnityWebRequest req = UnityWebRequest.Get(listUrl);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success) {
            string json = req.downloadHandler.text;

            // Omdat JsonUtility geen lijsten direct ondersteunt → wrapper gebruiken
            json = "{\"players\":" + json + "}"; 
            PlayerDataList dataList = JsonUtility.FromJson<PlayerDataList>(json);

            Debug.Log("📜 All players:");
            foreach (var p in dataList.players) {
                Debug.Log($" - {p.name}: {p.money}");
            }
        } else {
            Debug.LogError("❌ Load all error: " + req.error);
        }
    }
}
*/