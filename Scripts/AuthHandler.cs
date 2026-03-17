using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SocialPlatforms.Impl;

public class AuthHandler : MonoBehaviour
{
    private string Token;
    private string Username;

    private string apiUrl = "https://sid-restapi.onrender.com";

    [Header("Inputs")]
    [SerializeField] private TMP_InputField usernameInputField;
    [SerializeField] private TMP_InputField passwordInputField;

    [Header("UI")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text usernameLabel;

    [SerializeField] private GameObject panelLogin;
    [SerializeField] private GameObject gamePanel;
    [SerializeField] private GameObject loadingAnim;

    public bool IsLoggedIn => !string.IsNullOrEmpty(Token) && !string.IsNullOrEmpty(Username);

    void Start()
    {
        Token = PlayerPrefs.GetString("Token", null);
        Username = PlayerPrefs.GetString("Username", null);

        if (!string.IsNullOrEmpty(Token) && !string.IsNullOrEmpty(Username))
        {
            StartCoroutine(GetProfile());
        }
        else
        {
            Logout();
        }
    }
  

    void StartLoading()
    {
        loadingAnim.SetActive(true);
        statusText.text = "Cargando...";
    }

    void StopLoading(string message)
    {
        loadingAnim.SetActive(false);
        statusText.text = message;
    }


    // =========================
    // LOGIN
    // =========================

    public void LoginButtonHandler()
    {
        string username = usernameInputField.text;
        string password = passwordInputField.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            statusText.text = "Ingrese usuario y contraseña";
            return;
        }

        StartCoroutine(LoginCoroutine(username, password));
    }

    IEnumerator LoginCoroutine(string username, string password)
    {
        StartLoading();

        AuthData authData = new AuthData
        {
            username = username,
            password = password
        };

        string jsonData = JsonUtility.ToJson(authData);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);

        UnityWebRequest www = new UnityWebRequest(apiUrl + "/api/auth/login", "POST");
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            int errorCode = (int)www.responseCode;
            StopLoading("Error " + errorCode + " Usuario o contraseña incorrectos");

            Debug.LogError(www.error);
            Debug.Log(www.downloadHandler.text);
        }
        else
        {
            AuthResponse authResponse = JsonUtility.FromJson<AuthResponse>(www.downloadHandler.text);

            Token = authResponse.token;
            Username = authResponse.usuario.username;

            PlayerPrefs.SetString("Token", Token);
            PlayerPrefs.SetString("Username", Username);

            StopLoading("Login exitoso");

            SetUIForUserLogged();
        }
    }

    // =========================
    // REGISTER
    // =========================

    public void RegisterButtonHandler()
    {
        string username = usernameInputField.text;
        string password = passwordInputField.text;

        StartCoroutine(RegisterCoroutine(username, password));
    }

    IEnumerator RegisterCoroutine(string username, string password)
    {
        StartLoading();

        AuthData authData = new AuthData
        {
            username = username,
            password = password
        };

        string jsonData = JsonUtility.ToJson(authData);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);

        UnityWebRequest www = new UnityWebRequest(apiUrl + "/api/usuarios", "POST");
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            int errorCode = (int)www.responseCode;
            StopLoading("Error " + errorCode + " No se pudo registrar");

            Debug.LogError(www.error);
            Debug.Log(www.downloadHandler.text);
        }
        else
        {
            StopLoading("Usuario registrado correctamente");

            FindObjectOfType<ScoreManager>().LoadLeaderboardFromAPI();
        }
    }

    // =========================
    // VALIDAR TOKEN
    // =========================

    IEnumerator GetProfile()
    {
        UnityWebRequest www = UnityWebRequest.Get(apiUrl + "/api/usuarios/" + Username);
        www.SetRequestHeader("x-token", Token);

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log("Token inválido");
            Logout();
        }
        else
        {
            Debug.Log("Usuario autenticado con token");
            SetUIForUserLogged();
        }
    }

    // =========================
    // LOGOUT
    // =========================

    public void Logout()
    {
        PlayerPrefs.DeleteKey("Token");
        PlayerPrefs.DeleteKey("Username");

        Token = null;
        Username = null;

        panelLogin.SetActive(true);
        gamePanel.SetActive(false);

        statusText.text = "Sesión cerrada";
    }

    // =========================
    // ENVIAR SCORE
    // =========================

    public void SendScore(int score)
    {
        StartCoroutine(SendScoreCoroutine(score));
    }

    IEnumerator SendScoreCoroutine(int score)
    {
        if (string.IsNullOrEmpty(Token))
        {
            statusText.text = "Debes iniciar sesión";
            yield break;
        }

        StartLoading();

        UpdateScoreData data = new UpdateScoreData
        {
            username = Username,
            data = new ScoreWrapper
            {
                score = score
            }
        };

        string jsonData = JsonUtility.ToJson(data);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);

        UnityWebRequest www = new UnityWebRequest(apiUrl + "/api/usuarios", "PATCH");
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();

        www.SetRequestHeader("Content-Type", "application/json");
        www.SetRequestHeader("x-token", Token);

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            StopLoading("Error enviando score");
            Debug.LogError(www.error);
            Debug.Log(www.downloadHandler.text);
        }
        else
        {
            StopLoading("Score guardado: " + score);
            Debug.Log(www.downloadHandler.text);
        }
    }

    // =========================
    // LEADERBOARD
    // =========================

    public void GetLeaderboard()
    {
        StartCoroutine(GetLeaderboardCoroutine());
    }

    IEnumerator GetLeaderboardCoroutine()
    {
        UnityWebRequest www = UnityWebRequest.Get(apiUrl + "/api/scores");

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error leaderboard: " + www.error);
        }
        else
        {
            Debug.Log("Leaderboard: " + www.downloadHandler.text);
        }
    }

    // =========================
    // UI
    // =========================

    void SetUIForUserLogged()
    {
        panelLogin.SetActive(false);
        gamePanel.SetActive(true);

        usernameLabel.text = "Jugador: " + Username;
    }
}

[System.Serializable]
public class AuthData
{
    public string username;
    public string password;
}

[System.Serializable]
public class ScoreData
{
    public int score;
}

[System.Serializable]
public class User
{
    public string username;
    public UserData data;
}

[System.Serializable]
public class UserData
{
    public int score;
}
[System.Serializable]
public class UserList
{
    public List<User> usuarios; 
}


[System.Serializable]
public class AuthResponse
{
    public User usuario;
    public string token;
}
[System.Serializable]
public class UpdateScoreData
{
    public string username;
    public ScoreWrapper data;
}

[System.Serializable]
public class ScoreWrapper
{
    public int score;
}
