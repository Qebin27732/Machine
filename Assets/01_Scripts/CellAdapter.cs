using UnityEngine;

public class CellAdapter : MonoBehaviour
{
    private void OnMouseDown()
    {
        // Se ejecuta cuando el jugador hace clic sobre el Collider2D de la célula
        Debug.Log("¡Célula clickeada!");
        Destroy(gameObject);
    }
}