using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using DG.Tweening;
using YG;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

[Serializable]
public class GameSaveData
{
    public int bestScore;
    public int currentScore;
    public int[] boardState; // Изменено на одномерный массив
    public int boardSize; // Добавлен размер доски
    public bool hasActiveGame;
}

public class GameManager : MonoBehaviour
{
    //показывать рекламу внутри игрового процесса
    public bool ShowAdsInsideTheHameplay = false;
    //показывать рекламу по нажатию на кнопку продолжить игру и при новой игре
    public bool ShowAdsForButton= false;
    public static GameManager Instance { get; private set; }

    [Header("Platform SDK")]
    [SerializeField] private PlatformType currentSDKType;

    public enum PlatformType
    {
        VK,
        Yandex
    }
    // [SerializeField] private PlatformSDKManager platformSDKManager;
    [SerializeField] private VKPlatformSDK vkPlatformSDK;
    [SerializeField] private YandexPlatformSDK yandexPlatformSDK;
    private IPlatformSDK currentPltform;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI bestScoreText;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject StartPanel;
    [SerializeField] private Button continueGameButton;
    [SerializeField] private Button exitButton;

    [Header("Game Settings")]
    [SerializeField] private GameBoard gameBoard;
    [SerializeField] private int movesBeforeAd = 500;
    [SerializeField] private float scoreAnimationDuration = 0.5f;
    

    private int currentScore;
    private int bestScore;
    private bool isGameOver;
    private int movesSinceLastAd;
    private const string BestScoreKey = "BestScore";
    private const string SaveDataKey = "SavedGameData";
    private bool isWaitingForRewardedAd;
    private Sequence scoreAnimationSequence;
    private bool hasActiveGame;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        Application.targetFrameRate = 90;
        QualitySettings.vSyncCount = 0;
        
        if (exitButton != null)
        {
            #if UNITY_WEBGL
            exitButton.gameObject.SetActive(false);
            #endif
        }
        
       if (currentSDKType == PlatformType.VK)
        {
            currentPltform = vkPlatformSDK;
        }
        else
        {
            currentPltform = yandexPlatformSDK;
        }

        currentPltform.Initialize();
        
        LoadBestScore();
        InitializeUI();
        LoadProgress();
    }


    private void Start()
    {
        if (continueGameButton != null)
        {
            continueGameButton.onClick.AddListener(ContinueGame);
            UpdateContinueButtonState();
        }
        
        UpdateScoreUI(false);
        gameOverPanel.SetActive(false);
        StartPanel.SetActive(true);
        Time.timeScale = 0f;
        movesSinceLastAd = 0;
    }

    private void InitializeUI()
    {
        if (scoreText) scoreText.text = "0";
        if (bestScoreText) bestScoreText.text = bestScore.ToString();
    }

    private void UpdateContinueButtonState()
    {
        continueGameButton.interactable = hasActiveGame && !isGameOver;
    }

    public void ContinueGame()
    {
        LoadProgress();
        gameOverPanel.SetActive(false);
        ResumeGame();
        Time.timeScale = 1f;
    }

    public void AddScore(int points)
    {
        if (!ShowAdsInsideTheHameplay) return;

        int oldScore = currentScore;
        currentScore += points;
        
        if (currentScore > bestScore)
        {
            bestScore = currentScore;
        }
        
        UpdateScoreUI(true, oldScore);
        
        movesSinceLastAd++;
        if (movesSinceLastAd >= movesBeforeAd)
        {
            ShowInterstitialAd();
            movesSinceLastAd = 0;
        }
    }

    private void UpdateScoreUI(bool animate = false, int oldScore = 0)
    {
        if (scoreAnimationSequence != null)
        {
            scoreAnimationSequence.Kill();
        }

        if (animate && scoreText != null)
        {
            scoreAnimationSequence = DOTween.Sequence();
            scoreAnimationSequence.Append(scoreText.transform.DOScale(1.2f, scoreAnimationDuration * 0.5f));
            
            float startScore = oldScore;
            scoreAnimationSequence.Join(
                DOTween.To(() => startScore, x => {
                    startScore = x;
                    scoreText.text = Mathf.FloorToInt(x).ToString();
                }, currentScore, scoreAnimationDuration)
                .SetEase(Ease.OutQuad)
            );
            
            scoreAnimationSequence.Append(scoreText.transform.DOScale(1f, scoreAnimationDuration * 0.5f));
        }
        else
        {
            if (scoreText) scoreText.text = currentScore.ToString();
        }

        if (bestScoreText) bestScoreText.text = bestScore.ToString();
    }

    public void GameOver()
    {
        if (isGameOver) return;
        
        isGameOver = true;
        hasActiveGame = false;
        UpdateContinueButtonState();
        
        gameOverPanel.SetActive(true);
        gameOverPanel.transform.localScale = Vector3.zero;
        gameOverPanel.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);

        ShowInterstitialAd();
        
        SaveProgress();
    }

    public void RestartGame()
    {
        isGameOver = false;
        hasActiveGame = true;
        currentScore = 0;
        UpdateScoreUI(false);
        UpdateContinueButtonState();
        gameOverPanel.SetActive(false);
        gameBoard.RestartGame();
        movesSinceLastAd = 0;

        ResumeGame();
    }

    public void PauseGame()
    {
        SaveProgress();
        Time.timeScale = 0f;
        StartPanel.SetActive(true);
        StartPanel.transform.localScale = Vector3.zero;
        StartPanel.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack).SetUpdate(true);
    }

    public void ResumeGame()
    {    
        StartPanel.transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack).SetUpdate(true)
            .OnComplete(() => {
                StartPanel.SetActive(false);
                Time.timeScale = 1f;
            });

        if(ShowAdsForButton)
        {
            ShowInterstitialAd();
        }
    }

    private void ShowInterstitialAd()
    {
        currentPltform.FullscreenShow();
    }

    public void GiveReward()
    {
        if (isWaitingForRewardedAd)
        {
            isWaitingForRewardedAd = false;
            isGameOver = false;
            gameOverPanel.SetActive(false);
        }
    }

    private void LoadBestScore()
    {
        bestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
    }

    public void SaveProgress()
    {
        int[,] boardState = gameBoard.GetBoardState();
        int size = boardState.GetLength(0);
        int[] flatState = new int[size * size];

        // Преобразуем двумерный массив в одномерный
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                flatState[x * size + y] = boardState[x, y];
            }
        }

        GameSaveData saveData = new GameSaveData
        {
            bestScore = bestScore,
            currentScore = currentScore,
            boardState = flatState,
            boardSize = size,
            hasActiveGame = hasActiveGame
        };

        string json = JsonUtility.ToJson(saveData);
        Debug.Log("Сохранение данных: " + json);
        currentPltform.SaveProgress(SaveDataKey, json);

    }

    public void Load() => YandexGame.LoadLocal();

    public void LoadProgress()
    {
        currentPltform.LoadProgress(SaveDataKey, OnProgressLoaded);
    }

    private void OnProgressLoaded(string data)
    {
        Debug.Log("Запуск метода OnProgressLoaded!!! Данные:" + data);
        GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(data);
        Debug.Log("процесс извлечения:" + saveData);

        if(saveData == null)
        {
            Debug.Log("Данные не загружены");
            return;
        }

        bestScore = saveData.bestScore;
        currentScore = saveData.currentScore;
        hasActiveGame = saveData.hasActiveGame;
        
        // Debug.Log((saveData.boardState != null ) + " !!! " + saveData.boardState.Length);
        if (saveData.boardState != null && saveData.boardState.Length > 0)
        {
            // Преобразуем одномерный массив обратно в двумерный
            int size = saveData.boardSize;
            int[,] boardState = new int[size, size];
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    boardState[x, y] = saveData.boardState[x * size + y];
                }
            }
            gameBoard.LoadBoardState(boardState);
        }
        
        UpdateScoreUI(false);
        UpdateContinueButtonState();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            // SaveProgress();
        }
    }

    private void OnApplicationQuit()
    {
        SaveProgress();
    }

    private void OnDestroy()
    {
        if (scoreAnimationSequence != null)
        {
            scoreAnimationSequence.Kill();
        }

        if (continueGameButton != null)
        {
            continueGameButton.onClick.RemoveListener(ContinueGame);
        }
    }

    public void ExitGame()
    {
        SaveProgress();

        #if UNITY_ANDROID
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                activity.Call("finishAndRemoveTask");
            }
        }
        catch (Exception)
        {
            Application.Quit();
        }
        #elif !UNITY_WEBGL
        Application.Quit();
        #endif
    }

    [Tooltip("Как часто обновлять FPS")]
    [SerializeField] [Range(0.1f, 1f)] private float _updateInterval = 0.5f;

    [Tooltip("Текстовое поле для отображения FPS")]
    [SerializeField] private TMP_Text _fpsText;

    [Tooltip("Текстовое поле для отображения среднего FPS")]
    [SerializeField] private TMP_Text _averageFPSText;
    private float _timeleft;
    private float _currentFPS;
    private float _averageFPS;
    private void Update()
    {
        // _timeleft -= Time.deltaTime;

        // if (_timeleft <= 0.0)
        // {
        //     _currentFPS = 1.0f / Time.smoothDeltaTime;
        //     _averageFPS = Time.frameCount / Time.time;
        //     _timeleft = _updateInterval;

        //     _fpsText.text = $"FPS: {_currentFPS:0.}";
        //     _averageFPSText.text = $"Средний FPS: {_averageFPS:0.}";
        // }
    }
}
