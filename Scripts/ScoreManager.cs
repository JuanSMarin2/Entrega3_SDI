using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using UnityEngine.Networking;

public class ScoreManager : MonoBehaviour
{
    [Header("Current")]
    [SerializeField] private TMP_Text currentScoreText;
    [SerializeField] private TMP_Text currentPlayerText;

    [Header("Leaderboard UI")]
    [SerializeField] private TMP_Text[] leaderboardNames;
    [SerializeField] private TMP_Text[] leaderboardScores;
    [SerializeField] private AuthHandler authHandler;

    private bool leaderboardLoaded = false;

    private List<PlayerScore> scores = new List<PlayerScore>();

    private const int MAX_PLAYERS = 5;

    private string apiUrl = "https://sid-restapi.onrender.com";

    void Start()
    {
      
    }
    private void Update()
    {
        if(authHandler.IsLoggedIn && !leaderboardLoaded)
        {
            LoadLeaderboardFromAPI();
            leaderboardLoaded = true;
        }


    }

    // =========================
    // CURRENT SCORE
    // =========================

    public void SetCurrentScore(string playerName, int score)
    {
        currentScoreText.text = score.ToString();
        currentPlayerText.text = playerName;
    }

    // =========================
    // API
    // =========================

    public void LoadLeaderboardFromAPI()
    {
        StartCoroutine(GetLeaderboardCoroutine());
    }

    IEnumerator GetLeaderboardCoroutine()
    {
        string token = PlayerPrefs.GetString("Token");

        UnityWebRequest request = UnityWebRequest.Get(apiUrl + "/api/usuarios");
        request.SetRequestHeader("x-token", token);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error leaderboard: " + request.error);
            yield break;
        }

        string json = request.downloadHandler.text;

        Debug.Log("Leaderboard raw: " + json);

        UserList users = JsonUtility.FromJson<UserList>(json);

        scores.Clear();

        foreach (var user in users.usuarios)
        {
            scores.Add(new PlayerScore
            {
                name = user.username,
                score = (user.data != null) ? user.data.score : 0
            });

            Debug.Log("RAW JSON: " + json);
        }

        scores = scores
            .OrderByDescending(s => s.score)
            .Take(MAX_PLAYERS)
            .ToList();

        UpdateLeaderboardUI();
    }

    // =========================
    // UI
    // =========================

    void UpdateLeaderboardUI()
    {
        for (int i = 0; i < leaderboardNames.Length; i++)
        {
            if (i < scores.Count)
            {
                leaderboardNames[i].text = scores[i].name;
                leaderboardScores[i].text = scores[i].score.ToString();
            }
            else
            {
                leaderboardNames[i].text = "---";
                leaderboardScores[i].text = "---";
            }
        }
    }
}

[System.Serializable]
public class PlayerScore
{
    public string name;
    public int score;
}

[System.Serializable]
public class ScoreList
{
    public List<PlayerScore> list;
}