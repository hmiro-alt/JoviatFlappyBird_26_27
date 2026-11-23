using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum GameState { Ready, Playing, GameOver }

/// <summary>
/// Controla l'estat de la partida (preparat / jugant / final), la puntuació,
/// el rècord persistent (PlayerPrefs), la UI i els efectes de so.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    private const string BestScoreKey = "BestScore";

    [Header("Referències de l'escena")]
    public GameObject gameOverPanel;
    public GameObject pipeSpawner;
    public TextMeshProUGUI scoreText;

    [Header("Jugabilitat")]
    [Tooltip("Velocitat horitzontal de les canonades (unitats/segon)")]
    public float pipeSpeed = 2.6f;
    [Tooltip("Temps entre que l'ocell xoca i apareix el panell de Game Over")]
    public float gameOverDelay = 0.7f;

    [Header("Efectes de so")]
    public AudioClip flapClip;
    public AudioClip hitClip;
    public AudioClip startClip;
    public AudioClip pointClip;
    public AudioClip newRecordClip;
    [Range(0f, 1f)] public float flapVolume = 0.5f;
    [Range(0f, 1f)] public float hitVolume = 0.9f;
    [Range(0f, 1f)] public float startVolume = 0.6f;
    [Range(0f, 1f)] public float pointVolume = 0.7f;
    [Range(0f, 1f)] public float newRecordVolume = 0.8f;

    public GameState State { get; private set; } = GameState.Ready;
    public bool IsPlaying => State == GameState.Playing;
    public int Score => score;
    public int BestScore => best;

    private int score;
    private int best;
    private bool recordShownThisGame;

    private AudioSource sfx;
    private Player player;
    private PipeSpawner spawner;

    // UI creada per codi
    private TextMeshProUGUI bestText;
    private GameObject startPanel;
    private GameObject recordDialog;
    private TextMeshProUGUI recordScoreText;

    void Awake()
    {
        Instance = this;
        Application.targetFrameRate = 60; // a Android per defecte són 30 fps

        sfx = gameObject.AddComponent<AudioSource>();
        sfx.playOnAwake = false;
        sfx.spatialBlend = 0f;

        // Si no hi ha cap rècord desat, el valor inicial és 0
        best = PlayerPrefs.GetInt(BestScoreKey, 0);
        score = 0;
    }

    void Start()
    {
        player = FindAnyObjectByType<Player>();
        if (pipeSpawner != null) spawner = pipeSpawner.GetComponent<PipeSpawner>();
        if (spawner == null) spawner = FindAnyObjectByType<PipeSpawner>();

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        BuildUI();
        UpdateScoreInfo();
        startPanel.SetActive(true);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ------------------------------------------------------------------
    //  Flux de la partida
    // ------------------------------------------------------------------

    /// <summary>Comença la partida (botó Start o toc a la pantalla).</summary>
    public void StartGame()
    {
        if (State != GameState.Ready) return;

        State = GameState.Playing;
        score = 0;
        recordShownThisGame = false;
        UpdateScoreInfo();

        if (startPanel != null) startPanel.SetActive(false);
        PlaySound(startClip, startVolume);

        if (player != null) player.BeginPlay();
    }

    public void AddScore(int amount)
    {
        if (State != GameState.Playing) return; // després de morir no se sumen punts
        score += amount;
        UpdateScoreInfo();
        PlaySound(pointClip, pointVolume);
    }

    /// <summary>Acaba la partida. Es pot cridar diverses vegades: només actua la primera.</summary>
    public void StopGame()
    {
        if (State != GameState.Playing) return;
        State = GameState.GameOver;

        PlaySound(hitClip, hitVolume);
        if (player != null) player.Die();

        // Igualar el rècord NO compta: cal superar-lo
        bool newRecord = score > best;
        if (newRecord)
        {
            best = score;
            PlayerPrefs.SetInt(BestScoreKey, best);
            PlayerPrefs.Save(); // força l'escriptura a disc (editor i Android)
            UpdateScoreInfo();
        }

        StartCoroutine(GameOverSequence(newRecord));
    }

    private IEnumerator GameOverSequence(bool newRecord)
    {
        yield return new WaitForSeconds(gameOverDelay);

        if (gameOverPanel != null) gameOverPanel.SetActive(true);

        if (newRecord && !recordShownThisGame)
        {
            recordShownThisGame = true;
            recordScoreText.SetText("Score: " + score);
            recordDialog.SetActive(true);
            recordDialog.transform.SetAsLastSibling();
            PlaySound(newRecordClip, newRecordVolume);
        }
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ------------------------------------------------------------------
    //  So
    // ------------------------------------------------------------------

    public void PlayFlap() => PlaySound(flapClip, flapVolume);

    private void PlaySound(AudioClip clip, float volume)
    {
        if (clip != null && sfx != null) sfx.PlayOneShot(clip, volume);
    }

    // ------------------------------------------------------------------
    //  UI
    // ------------------------------------------------------------------

    private void UpdateScoreInfo()
    {
        if (scoreText != null) scoreText.SetText("Score: " + score);
        if (bestText != null) bestText.SetText("Best: " + best);
    }

    private void BuildUI()
    {
        Transform canvas = scoreText.canvas.transform;

        // Marcadors (Current Score i Best Score), sempre visibles a dalt a l'esquerra
        bestText = CloneText(canvas, "BestScoreText");
        StyleCornerText(scoreText, new Vector2(40, -40));
        StyleCornerText(bestText, new Vector2(40, -135));

        // Pantalla d'inici: títol, instruccions i botó Start
        startPanel = CreatePanel(canvas, "StartPanel", Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0), true);
        var title = CloneText(startPanel.transform, "Title");
        StyleCenterText(title, "FLAPPY BIRD", new Vector2(0, 650), 110, Color.white);
        var hint = CloneText(startPanel.transform, "Hint");
        StyleCenterText(hint, "Toca la pantalla o prem START", new Vector2(0, 540), 50, Color.white);
        CreateButton(startPanel.transform, "StartButton", "START", new Vector2(0, -420), new Vector2(420, 150), StartGame);

        // Diàleg de nou rècord
        recordDialog = CreatePanel(canvas, "NewRecordDialog", Vector2.zero, new Vector2(820, 560), new Color(0.08f, 0.08f, 0.15f, 0.92f), false);
        var recTitle = CloneText(recordDialog.transform, "RecordTitle");
        StyleCenterText(recTitle, "NEW RECORD!", new Vector2(0, 160), 100, new Color(1f, 0.82f, 0.1f));
        recordScoreText = CloneText(recordDialog.transform, "RecordScore");
        StyleCenterText(recordScoreText, "Score: 0", new Vector2(0, 20), 80, Color.white);
        CreateButton(recordDialog.transform, "OkButton", "OK", new Vector2(0, -170), new Vector2(300, 130),
            () => recordDialog.SetActive(false));
        recordDialog.SetActive(false);
    }

    private TextMeshProUGUI CloneText(Transform parent, string name)
    {
        GameObject go = Instantiate(scoreText.gameObject, parent, false);
        go.name = name;
        var t = go.GetComponent<TextMeshProUGUI>();
        t.raycastTarget = false;
        t.margin = Vector4.zero; // el text original tenia un marge dret negatiu que el desplaçava
        t.enableAutoSizing = false;
        return t;
    }

    private static void StyleCornerText(TextMeshProUGUI t, Vector2 pos)
    {
        RectTransform rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(700, 95);
        t.margin = Vector4.zero;
        t.enableAutoSizing = false;
        t.fontSize = 80;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.horizontalAlignment = HorizontalAlignmentOptions.Left;
        t.verticalAlignment = VerticalAlignmentOptions.Middle;
        t.color = Color.white;
        t.outlineWidth = 0.25f;
        t.outlineColor = Color.black;
    }

    private static void StyleCenterText(TextMeshProUGUI t, string text, Vector2 pos, float size, Color color)
    {
        RectTransform rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(900, size * 1.3f);
        t.SetText(text);
        t.fontSize = size;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.horizontalAlignment = HorizontalAlignmentOptions.Center;
        t.verticalAlignment = VerticalAlignmentOptions.Middle;
        t.color = color;
        t.outlineWidth = 0.25f;
        t.outlineColor = Color.black;
    }

    private static GameObject CreatePanel(Transform parent, string name, Vector2 pos, Vector2 size, Color color, bool stretch)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        if (stretch)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
        else
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }
        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = color.a > 0f; // el panell d'inici transparent no bloqueja els tocs
        return go;
    }

    private Button CreateButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, UnityAction onClick)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var img = go.GetComponent<Image>();
        img.color = new Color(0.96f, 0.55f, 0.12f);
        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        var text = CloneText(go.transform, "Label");
        RectTransform trt = text.rectTransform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        text.SetText(label);
        text.fontSize = size.y * 0.5f;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.horizontalAlignment = HorizontalAlignmentOptions.Center;
        text.verticalAlignment = VerticalAlignmentOptions.Middle;
        text.color = Color.white;
        text.outlineWidth = 0.2f;
        text.outlineColor = Color.black;
        return btn;
    }
}
