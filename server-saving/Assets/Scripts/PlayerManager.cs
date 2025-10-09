using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Text;

/*public class SecureServerClient : MonoBehaviour
{
    [Header("Server Config")]
    public string baseUrl = "http://192.168.1.100:3000"; // Change to your server!
    [Header("UI Elements")]
    public InputField usernameField;
    public InputField passwordField;
    public InputField playerNameField;
    public InputField moneyField;
    public InputField levelField;
    public Text infoText;

    private string jwtToken = "";
    private string activePlayerName = "";

    // Register a new user
    public void Register()
    {
        StartCoroutine(RegisterCoroutine(usernameField.text, passwordField.text));
    }

    IEnumerator RegisterCoroutine(string username, string password)
    {
        string url = baseUrl + "/auth/register";
        string json = $"{{\"username\":\"{username}\",\"password\":\"{password}\"}}";
        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                infoText.text = "Registered! You can now login.";
            }
            else
            {
                infoText.text = "Register error: " + req.downloadHandler.text;
            }
        }
    }

    // Log in and store JWT token
    public void Login()
    {
        StartCoroutine(LoginCoroutine(usernameField.text, passwordField.text));
    }

    IEnumerator LoginCoroutine(string username, string password)
    {
        string url = baseUrl + "/auth/login";
        string json = $"{{\"username\":\"{username}\",\"password\":\"{password}\"}}";
        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                var resp = JsonUtility.FromJson<LoginResponse>(req.downloadHandler.text);
                jwtToken = resp.token;
                infoText.text = "Login success!";
            }
            else
            {
                infoText.text = "Login error: " + req.downloadHandler.text;
            }
        }
    }

    // Save data for this player profile under current account
    public void SavePlayerData()
    {
        StartCoroutine(SavePlayerDataCoroutine(
            playerNameField.text,
            int.Parse(moneyField.text),
            int.Parse(levelField.text)
        ));
    }

    IEnumerator SavePlayerDataCoroutine(string playerName, int money, int level)
    {
        string url = baseUrl + "/player/" + UnityWebRequest.EscapeURL(playerName);
        string json = $"{{\"money\":{money},\"level\":{level}}}";
        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", "Bearer " + jwtToken);
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                infoText.text = "Data saved!";
            }
            else
            {
                infoText.text = "Save error: " + req.downloadHandler.text;
            }
        }
    }

    // Load data for this player profile under current account
    public void LoadPlayerData()
    {
        StartCoroutine(LoadPlayerDataCoroutine(playerNameField.text));
    }

    IEnumerator LoadPlayerDataCoroutine(string playerName)
    {
        string url = baseUrl + "/player/" + UnityWebRequest.EscapeURL(playerName);
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.SetRequestHeader("Authorization", "Bearer " + jwtToken);
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                PlayerData data = JsonUtility.FromJson<PlayerData>(req.downloadHandler.text);
                moneyField.text = data.money.ToString();
                levelField.text = data.level.ToString();
                infoText.text = $"Loaded: Money={data.money}, Level={data.level}";
            }
            else
            {
                infoText.text = "Load error: " + req.downloadHandler.text;
            }
        }
    }

    // List all player profiles for the current user (optional)
    public void ListPlayerProfiles()
    {
        StartCoroutine(ListPlayerProfilesCoroutine());
    }

    IEnumerator ListPlayerProfilesCoroutine()
    {
        string url = baseUrl + "/players";
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.SetRequestHeader("Authorization", "Bearer " + jwtToken);
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                string json = req.downloadHandler.text;
                json = "{\"names\":" + json + "}";
                PlayerNameList list = JsonUtility.FromJson<PlayerNameList>(json);
                infoText.text = "Profiles: " + string.Join(", ", list.names);
            }
            else
            {
                infoText.text = "List error: " + req.downloadHandler.text;
            }
        }
    }

    [System.Serializable]
    public class LoginResponse { public string token; }
    [System.Serializable]
    public class PlayerData { public int money; public int level; }
    [System.Serializable]
    public class PlayerNameList { public string[] names; }
}*/