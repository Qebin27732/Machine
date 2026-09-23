using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI scoreText; // Referencia al texto de puntuación
    public float timeRemaining = 30f;
    private bool isGameActive = true;
    private int score = 0; // Puntuación actual

    [Header("Spawner")]
    public GameObject cellPrefab;
    public float spawnInterval = 5f;
    private float spawnTimer = 0f;

    [Header("Datos de Adaptación (Machine Learning Simple)")]
    public Color bestColor = Color.white;
    public float bestScale = 1.0f;
    private bool hasLearned = false;

    void Start()
    {
        UpdateScoreUI();
    }

    void Update()
    {
        if (!isGameActive) return;

        if (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            UpdateTimerUI();
        }
        else
        {
            timeRemaining = 0;
            isGameActive = false;
            UpdateTimerUI();
            Debug.Log("¡Fin de la ronda! Evaluando adaptación de células supervivientes...");
        }

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            SpawnCell();
            spawnTimer = 0f;
        }
    }

    void UpdateTimerUI()
    {
        if (timerText != null)
        {
            timerText.text = "Tiempo: " + Mathf.CeilToInt(timeRemaining).ToString() + "s";
        }
    }

    void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Eliminadas: " + score.ToString();
        }
    }

    // Método para sumar puntos cuando el jugador elimina una célula
    public void AddScore()
    {
        score++;
        UpdateScoreUI();
    }

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
            {
                cellAdapter.InitializeWithLearning(bestColor, bestScale);
            }
            else
            {
                cellAdapter.InitializeRandom();
            }
        }
    }

    public void RegisterSurvivingCell(Color survivingColor, float survivingScale)
    {
        bestColor = survivingColor;
        bestScale = survivingScale;
        hasLearned = true;
        Debug.Log($"Célula superviviente registrada. Nuevo patrón base aprendido -> Color: {bestColor}, Escala: {bestScale}");
    }
}