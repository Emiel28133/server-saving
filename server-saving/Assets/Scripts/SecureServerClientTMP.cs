using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class SecureServerClientTMP : MonoBehaviour
{
    [Header("Server Config")]
    [Tooltip("Example: http://192.168.1.100:3000 or https://your-domain.com")]
    public string baseUrl = "http://127.0.0.1:3000";

    [Header("UI (TextMeshPro)")]
    public TMP_InputField usernameField;
    public TMP_InputField passwordField; // Set Content Type = Password in Inspector
    public TMP_InputField playerNameField;
    public TMP_InputField moneyField;
    public TMP_InputField levelField;
    public TextMeshProUGUI infoText;

    [Header("Buttons")]
    public Button registerButton;
    public Button loginButton;
    public Button saveButton;
    public Button loadButton;
    public Button listProfilesButton; // optional

    private string jwtToken = "";

    void Start()
    {
        // Disable actions until login
        SetActionsInteractable(false);
        AppendInfo("Ready. Please login or register.");
    }

    private void SetActionsInteractable(bool on)
    {
        if (saveButton) saveButton.interactable = on;
        if (loadButton) loadButton.interactable = on;
        if (listProfilesButton) listProfilesButton.interactable = on;
    }

    private void SetInfo(string msg)
    {
        if (infoText) infoText.text = msg;
        Debug.Log(msg);
    }

    private void AppendInfo(string msg)
    {
        if (!infoText) { Debug.Log(msg); return; }
        infoText.text = (infoText.text ?? string.Empty) + "\n" + msg;
        Debug.Log(msg);
    }

    // Hook these to buttons in the Inspector
    public void OnClickRegister()
    {
        StartCoroutine(RegisterCoroutine(usernameField?.text, passwordField?.text));
    }

    public void OnClickLogin()
    {
        StartCoroutine(LoginCoroutine(usernameField?.text, passwordField?.text));
    }

    public void OnClickSave()
    {
        int money = int.TryParse(moneyField?.text, out var m) ? m : 0;
        int level = int.TryParse(levelField?.text, out var l) ? l : 0;
        StartCoroutine(SavePlayerDataCoroutine(playerNameField?.text, money, level));
    }

    public void OnClickLoad()
    {
        StartCoroutine(LoadPlayerDataCoroutine(playerNameField?.text));
    }

    public void OnClickListProfiles()
    {
        StartCoroutine(ListPlayerProfilesCoroutine());
    }

    IEnumerator RegisterCoroutine(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            SetInfo("Register: username/password required.");
            yield break;
        }

        string url = baseUrl.TrimEnd('/') + "/auth/register";
        string json = $"{{\"username\":\"{username}\",\"password\":\"{password}\"}}";
        AppendInfo($"POST {url}");

        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
                SetInfo("Registered! You can now login.");
            else
                SetInfo($"Register error: {req.responseCode} {req.error} {req.downloadHandler.text}");
        }
    }

    IEnumerator LoginCoroutine(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            SetInfo("Login: username/password required.");
            yield break;
        }

        string url = baseUrl.TrimEnd('/') + "/auth/login";
        string json = $"{{\"username\":\"{username}\",\"password\":\"{password}\"}}";
        AppendInfo($"POST {url}");

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
                jwtToken = resp?.token ?? "";
                if (string.IsNullOrEmpty(jwtToken))
                {
                    SetInfo("Login failed: empty token.");
                    SetActionsInteractable(false);
                }
                else
                {
                    SetInfo("Login success!");
                    SetActionsInteractable(true);
                }
            }
            else
            {
                SetInfo($"Login error: {req.responseCode} {req.error} {req.downloadHandler.text}");
                SetActionsInteractable(false);
            }
        }
    }

    IEnumerator SavePlayerDataCoroutine(string playerName, int money, int level)
    {
        if (string.IsNullOrWhiteSpace(jwtToken))
        {
            SetInfo("Save: please login first.");
            yield break;
        }
        if (string.IsNullOrWhiteSpace(playerName))
        {
            SetInfo("Save: player/profile name required.");
            yield break;
        }

        string url = baseUrl.TrimEnd('/') + "/player/" + UnityWebRequest.EscapeURL(playerName);
        string json = $"{{\"money\":{money},\"level\":{level}}}";
        AppendInfo($"POST {url} body={json}");

        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", "Bearer " + jwtToken);

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
                SetInfo("Data saved!");
            else
                SetInfo($"Save error: {req.responseCode} {req.error} {req.downloadHandler.text}");
        }
    }

    IEnumerator LoadPlayerDataCoroutine(string playerName)
    {
        if (string.IsNullOrWhiteSpace(jwtToken))
        {
            SetInfo("Load: please login first.");
            yield break;
        }
        if (string.IsNullOrWhiteSpace(playerName))
        {
            SetInfo("Load: player/profile name required.");
            yield break;
        }

        string url = baseUrl.TrimEnd('/') + "/player/" + UnityWebRequest.EscapeURL(playerName);
        AppendInfo($"GET {url}");

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Authorization", "Bearer " + jwtToken);

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                PlayerData data = JsonUtility.FromJson<PlayerData>(req.downloadHandler.text);
                if (moneyField) moneyField.text = data.money.ToString();
                if (levelField) levelField.text = data.level.ToString();
                SetInfo($"Loaded: Money={data.money}, Level={data.level}");
            }
            else
            {
                SetInfo($"Load error: {req.responseCode} {req.error} {req.downloadHandler.text}");
            }
        }
    }

    IEnumerator ListPlayerProfilesCoroutine()
    {
        if (string.IsNullOrWhiteSpace(jwtToken))
        {
            SetInfo("List: please login first.");
            yield break;
        }

        string url = baseUrl.TrimEnd('/') + "/players";
        AppendInfo($"GET {url}");

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Authorization", "Bearer " + jwtToken);

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                string json = "{\"names\":" + req.downloadHandler.text + "}";
                PlayerNameList list = JsonUtility.FromJson<PlayerNameList>(json);
                SetInfo("Profiles: " + string.Join(", ", list.names));
            }
            else
            {
                SetInfo($"List error: {req.responseCode} {req.error} {req.downloadHandler.text}");
            }
        }
    }

    [System.Serializable] public class LoginResponse { public string token; }
    [System.Serializable] public class PlayerData { public int money; public int level; }
    [System.Serializable] public class PlayerNameList { public string[] names; }
}