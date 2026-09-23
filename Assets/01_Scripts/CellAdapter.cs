using UnityEngine;

public class CellAdapter : MonoBehaviour
{
    [Header("Atributos de Tamaño")]
    public float minScale = 0.5f;
    public float maxScale = 2.0f;

    [HideInInspector]
    public GameManager gameManager;

    private SpriteRenderer spriteRenderer;
    private Color currentColor;
    private float currentScale;
    private bool isClicked = false;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void InitializeRandom()
    {
        currentScale = Random.Range(minScale, maxScale);
        transform.localScale = new Vector3(currentScale, currentScale, 1f);

        if (spriteRenderer != null)
        {
            currentColor = new Color(Random.Range(0.2f, 1f), Random.Range(0.2f, 1f), Random.Range(0.2f, 1f));
            spriteRenderer.color = currentColor;
        }
    }

    public void InitializeWithLearning(Color parentColor, float parentScale)
    {
        currentScale = Mathf.Clamp(parentScale + Random.Range(-0.2f, 0.2f), minScale, maxScale);
        transform.localScale = new Vector3(currentScale, currentScale, 1f);

        if (spriteRenderer != null)
        {
            float r = Mathf.Clamp01(parentColor.r + Random.Range(-0.15f, 0.15f));
            float g = Mathf.Clamp01(parentColor.g + Random.Range(-0.15f, 0.15f));
            float b = Mathf.Clamp01(parentColor.b + Random.Range(-0.15f, 0.15f));
            currentColor = new Color(r, g, b);
            spriteRenderer.color = currentColor;
        }
    }

    private void OnMouseDown()
    {
        isClicked = true;

        // Notificar al GameManager para sumar puntos al eliminar la célula
        if (gameManager != null)
        {
            gameManager.AddScore();
            Debug.Log("¡Célula destruida y punto sumado!");
        }
        else
        {
            Debug.LogWarning("El gameManager no está asignado en esta célula.");
        }

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        // Si la célula se destruye por tiempo (no por clic del usuario), registra su supervivencia
        if (!isClicked && gameManager != null)
        {
            gameManager.RegisterSurvivingCell(currentColor, currentScale);
        }
    }
}