using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI timerText;
    public float timeRemaining = 30f;
    private bool isGameActive = true;

    [Header("Spawner")]
    public GameObject cellPrefab;
    public float spawnInterval = 10f;
    private float spawnTimer = 0f;

    void Update()
    {
        if (!isGameActive) return;

        // Temporizador de ronda
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
            Debug.Log("¡Fin de la ronda!");
        }

        // Spawner automático de células
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

    void SpawnCell()
    {
        if (cellPrefab == null) return;

        // Posición aleatoria dentro de la vista de la cámara
        Vector2 randomPos = new Vector2(Random.Range(-6f, 6f), Random.Range(-3f, 3f));
        Instantiate(cellPrefab, randomPos, Quaternion.identity);
    }
}