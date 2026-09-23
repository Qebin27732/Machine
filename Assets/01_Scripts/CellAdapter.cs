using UnityEngine;

public class CellAdapter : MonoBehaviour
{
    [Header("Límites de Tamaño")]
    public float minScale = 0.3f;
    public float maxScale = 1.8f;

    [Header("Configuración de Mutación (Machine Learning)")]
    public float scaleMutationRate = 0.15f;
    public float colorMutationRate = 0.08f;

    [Header("Camuflaje Activo (mientras la célula está viva)")]
    [Tooltip("Si está activo, la célula ajusta su color hacia el fondo en tiempo real")]
    public bool enableActiveCamouflage = true;
    [Tooltip("Velocidad con la que la célula se acerca al color de fondo (por segundo)")]
    public float camouflageSpeed = 0.5f;

    [HideInInspector]
    public GameManager gameManager;

    private SpriteRenderer spriteRenderer;
    private Color currentColor;
    private float currentScale;
    private bool isClicked = false;
    private bool isInitialized = false;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (!isInitialized || !enableActiveCamouflage || isClicked || gameManager == null) return;
        if (spriteRenderer == null) return;

        Color target = gameManager.targetBackgroundColor;

        if (gameManager.IsPerfectlyAdapted)
            currentColor = target; // adaptación completa: igual al fondo, exacto
        else
            currentColor = Color.Lerp(currentColor, target, camouflageSpeed * Time.deltaTime);

        spriteRenderer.color = currentColor;
    }

    public void InitializeNearBackground(Color backgroundColor, float spread)
    {
        currentScale = Random.Range(minScale, maxScale);
        transform.localScale = new Vector3(currentScale, currentScale, 1f);

        if (spriteRenderer != null)
        {
            float r = Mathf.Clamp01(backgroundColor.r + Random.Range(-spread, spread));
            float g = Mathf.Clamp01(backgroundColor.g + Random.Range(-spread, spread));
            float b = Mathf.Clamp01(backgroundColor.b + Random.Range(-spread, spread));

            currentColor = new Color(r, g, b, 1f);
            spriteRenderer.color = currentColor;
        }

        ScheduleSelfDestruction();
        isInitialized = true;
    }

    // Inicialización totalmente aleatoria (solo para pruebas)
    public void InitializeRandom()
    {
        currentScale = Random.Range(minScale, maxScale);
        transform.localScale = new Vector3(currentScale, currentScale, 1f);

        if (spriteRenderer != null)
        {
            currentColor = new Color(Random.Range(0.2f, 1f), Random.Range(0.2f, 1f), Random.Range(0.2f, 1f), 1f);
            spriteRenderer.color = currentColor;
        }

        ScheduleSelfDestruction();
        isInitialized = true;
    }

    public void InitializeWithLearning(Color parentColor, float parentScale)
    {
        currentScale = Mathf.Clamp(parentScale + Random.Range(-scaleMutationRate, scaleMutationRate), minScale, maxScale);
        transform.localScale = new Vector3(currentScale, currentScale, 1f);

        if (spriteRenderer != null)
        {
            float r = Mathf.Clamp01(parentColor.r + Random.Range(-colorMutationRate, colorMutationRate));
            float g = Mathf.Clamp01(parentColor.g + Random.Range(-colorMutationRate, colorMutationRate));
            float b = Mathf.Clamp01(parentColor.b + Random.Range(-colorMutationRate, colorMutationRate));

            currentColor = new Color(r, g, b, 1f);
            spriteRenderer.color = currentColor;
        }

        ScheduleSelfDestruction();
        isInitialized = true;
    }

    private void ScheduleSelfDestruction()
    {
        float lifeTime = gameManager != null ? gameManager.roundDuration : 10f;
        Destroy(gameObject, lifeTime);
    }

    private void OnMouseDown()
    {
        isClicked = true;

        if (gameManager != null)
        {
            gameManager.AddScore();
            gameManager.RegisterFailedCell(currentColor, currentScale);
        }

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (isInitialized && !isClicked && gameManager != null)
        {
            gameManager.RegisterSurvivingCell(currentColor, currentScale);
        }
    }
}