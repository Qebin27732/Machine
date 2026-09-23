using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class GameManager : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI roundText;

    [Header("Configuración de Rondas")]
    [Tooltip("Duración de cada ronda en segundos. Las rondas son infinitas.")]
    public float roundDuration = 10f;
    private int currentRound = 1;
    private float timeRemaining;
    private int score = 0;

    [Header("Spawner")]
    public GameObject cellPrefab;
    public float spawnInterval = 1.5f;
    private float spawnTimer = 0f;

    [Header("Fondo")]
    [Tooltip("Color de referencia del entorno. Si asignás una fuente abajo, se actualiza solo.")]
    public Color targetBackgroundColor = new Color(0.2f, 0.3f, 0.4f);
    [Tooltip("Opcional: SpriteRenderer del fondo. Su color se usa como referencia.")]
    public SpriteRenderer backgroundSprite;
    [Tooltip("Opcional: cámara cuyo color de fondo se usa como referencia (si no hay sprite).")]
    public Camera backgroundCamera;

    [Header("Datos de Adaptación")]
    [Range(0f, 1f)]
    public float backgroundBiasWeight = 0.3f;
    public float initialColorSpread = 0.3f;
    public Color bestColor;
    public float bestScale = 1.0f;
    private bool hasLearned = false;

    [Header("Adaptación Perfecta")]
    [Tooltip("Cantidad de rondas (desde el inicio o desde el último cambio de fondo) hasta igualar el fondo exacto")]
    public int roundsToPerfectAdaptation = 10;
    private bool isPerfectlyAdapted = false;
    private int adaptationStartRound = 1;
    private Color lastKnownBackground;

    public bool IsPerfectlyAdapted => isPerfectlyAdapted;

    [Header("Historial (ventana móvil)")]
    public int historySize = 20;
    private List<Color> successfulColors = new List<Color>();
    private List<float> successfulScales = new List<float>();
    private List<Color> failedColors = new List<Color>();

    void Start()
    {
        SyncBackgroundColor();
        lastKnownBackground = targetBackgroundColor;
        bestColor = targetBackgroundColor;
        timeRemaining = roundDuration;
        UpdateScoreUI();
        UpdateRoundUI();
    }

    void Update()
    {
        DetectBackgroundChange();

        if (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            UpdateTimerUI();
        }
        else
        {
            NextRound();
        }

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            SpawnCell();
            spawnTimer = 0f;
        }
    }

    // ---------- Fondo ----------

    void SyncBackgroundColor()
    {
        if (backgroundSprite != null)
        {
            targetBackgroundColor = backgroundSprite.color;
        }
        else if (backgroundCamera != null)
        {
            Color c = backgroundCamera.backgroundColor;
            targetBackgroundColor = new Color(c.r, c.g, c.b, 1f); // alfa siempre 1
        }
    }

    void DetectBackgroundChange()
    {
        SyncBackgroundColor();

        if (ColorDistance(targetBackgroundColor, lastKnownBackground) > 0.001f)
        {
            lastKnownBackground = targetBackgroundColor;
            ResetAdaptation();
        }
    }

    // El fondo cambió: se descarta lo aprendido y se vuelve a adaptar desde cero
    void ResetAdaptation()
    {
        successfulColors.Clear();
        successfulScales.Clear();
        failedColors.Clear();

        hasLearned = false;
        isPerfectlyAdapted = false;
        adaptationStartRound = currentRound;
        bestColor = targetBackgroundColor;

        Debug.Log($"[Adaptación] El fondo cambió. Reiniciando adaptación en la Ronda {currentRound}.");
    }

    // ---------- Rondas ----------

    void NextRound()
    {
        currentRound++;
        timeRemaining = roundDuration;
        UpdateRoundUI();
        Debug.Log($"¡Iniciando Ronda {currentRound}!");

        // Se recalcula cada ronda para que la adaptación avance aunque no haya sobrevivientes
        RecalculateBestPattern();
    }

    void UpdateTimerUI()
    {
        if (timerText != null)
            timerText.text = "Tiempo: " + Mathf.CeilToInt(Mathf.Max(timeRemaining, 0f)) + "s";
    }

    void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = "Eliminadas: " + score;
    }

    void UpdateRoundUI()
    {
        if (roundText != null)
            roundText.text = "Ronda: " + currentRound;
    }

    public void AddScore()
    {
        score++;
        UpdateScoreUI();
    }

    // ---------- Spawner ----------

    void SpawnCell()
    {
        if (cellPrefab == null) return;

        Vector2 randomPos = new Vector2(Random.Range(-6f, 6f), Random.Range(-3f, 3f));
        GameObject newCell = Instantiate(cellPrefab, randomPos, Quaternion.identity);

        CellAdapter cellAdapter = newCell.GetComponent<CellAdapter>();
        if (cellAdapter != null)
        {
            cellAdapter.gameManager = this;

            if (hasLearned)
                cellAdapter.InitializeWithLearning(bestColor, bestScale);
            else
                cellAdapter.InitializeNearBackground(targetBackgroundColor, initialColorSpread);
        }
    }

    // ---------- Registro de resultados ----------

    public void RegisterSurvivingCell(Color survivingColor, float survivingScale)
    {
        successfulColors.Add(survivingColor);
        successfulScales.Add(survivingScale);
        TrimList(successfulColors, historySize);
        TrimList(successfulScales, historySize);

        RecalculateBestPattern();

        Debug.Log($"[Éxito] Célula sobrevivió en Ronda {currentRound}. Color: {survivingColor}, Escala: {survivingScale}");
    }

    public void RegisterFailedCell(Color failedColor, float failedScale)
    {
        failedColors.Add(failedColor);
        TrimList(failedColors, historySize);

        Debug.Log($"[Fallo] Célula eliminada en Ronda {currentRound}. Color: {failedColor}");
    }

    // ---------- Aprendizaje ----------

    void RecalculateBestPattern()
    {
        // Rondas transcurridas desde el inicio o desde el último cambio de fondo
        int roundsSinceReset = currentRound - adaptationStartRound;
        float adaptationProgress = roundsToPerfectAdaptation > 0
            ? Mathf.Clamp01((float)roundsSinceReset / roundsToPerfectAdaptation)
            : 1f;

        if (!isPerfectlyAdapted && adaptationProgress >= 1f)
        {
            isPerfectlyAdapted = true;
            Debug.Log($"[Adaptación] ¡Camuflaje perfecto alcanzado en la Ronda {currentRound}!");
        }

        // Adaptación perfecta: el color es exactamente el del fondo, haya o no datos de éxito
        if (isPerfectlyAdapted)
        {
            bestColor = targetBackgroundColor;
            if (successfulScales.Count > 0) bestScale = successfulScales.Average();
            hasLearned = true;
            return;
        }

        if (successfulColors.Count == 0) return;

        float avgR = successfulColors.Average(c => c.r);
        float avgG = successfulColors.Average(c => c.g);
        float avgB = successfulColors.Average(c => c.b);
        Color avgSuccess = new Color(avgR, avgG, avgB);

        // El sesgo hacia el fondo crece de 'backgroundBiasWeight' a 100%
        float dynamicBias = Mathf.Lerp(backgroundBiasWeight, 1f, adaptationProgress);
        avgSuccess = Color.Lerp(avgSuccess, targetBackgroundColor, dynamicBias);

        if (failedColors.Count > 0)
        {
            float favgR = failedColors.Average(c => c.r);
            float favgG = failedColors.Average(c => c.g);
            float favgB = failedColors.Average(c => c.b);
            Color avgFail = new Color(favgR, favgG, favgB);

            float distance = ColorDistance(avgSuccess, avgFail);
            if (distance < 0.15f)
            {
                Vector3 dir = new Vector3(avgSuccess.r - avgFail.r, avgSuccess.g - avgFail.g, avgSuccess.b - avgFail.b);
                if (dir.magnitude < 0.001f) dir = Random.insideUnitSphere;
                dir.Normalize();
                avgSuccess = new Color(
                    Mathf.Clamp01(avgSuccess.r + dir.x * 0.1f),
                    Mathf.Clamp01(avgSuccess.g + dir.y * 0.1f),
                    Mathf.Clamp01(avgSuccess.b + dir.z * 0.1f)
                );
            }
        }

        bestColor = avgSuccess;
        bestScale = successfulScales.Average();
        hasLearned = true;
    }

    float ColorDistance(Color a, Color b)
    {
        return Mathf.Sqrt(Mathf.Pow(a.r - b.r, 2) + Mathf.Pow(a.g - b.g, 2) + Mathf.Pow(a.b - b.b, 2));
    }

    void TrimList<T>(List<T> list, int maxSize)
    {
        while (list.Count > maxSize)
            list.RemoveAt(0);
    }
}