using UnityEngine;
using TMPro;

public class TimerManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private GameObject scorePanel;

    [Header("References")]
    [SerializeField] private PlayerMovement player;
    [SerializeField] private AuthHandler authHandler;

    [Header("Score Settings")]
    [SerializeField] private int maxScore = 1000;
    [SerializeField] private int minScore = 10;
    [SerializeField] private float maxTime = 90f;

    [SerializeField] private ScoreManager scoreManager;

    private float timer;
    private bool running;

    private Vector3 playerStartPosition;

    void Start()
    {
        playerStartPosition = player.transform.position;
        UpdateTimerUI();
    }

    void Update()
    {
        if (!running) return;

        timer += Time.deltaTime;
        UpdateTimerUI();
    }

    void UpdateTimerUI()
    {
        int minutes = Mathf.FloorToInt(timer / 60);
        int seconds = Mathf.FloorToInt(timer % 60);

        timerText.text = $"{minutes:00}:{seconds:00}";
    }

 

    public void StartTimer()
    {
        timer = 0f;
        running = true;
    }



    public void FinishTimer()
    {
        running = false;

        int score = CalculateScore();

        Debug.Log("Score obtenido: " + score);

    
        authHandler.SendScore(score);

        player.transform.position = playerStartPosition;
        player.canMove = false;

        scorePanel.SetActive(true);

        string username = PlayerPrefs.GetString("Username", "Player");

        scoreManager.SetCurrentScore(username, score);

     
        scoreManager.LoadLeaderboardFromAPI();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {

        FinishTimer();
    }



    int CalculateScore()
    {
        float t = Mathf.Clamp01(timer / maxTime);

        int score = Mathf.RoundToInt(Mathf.Lerp(maxScore, minScore, t));

        return score;
    }
}