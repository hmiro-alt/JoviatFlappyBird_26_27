using UnityEngine;

/// <summary>Cada tub: si l'ocell el toca, s'acaba la partida.</summary>
public class PipeCollider : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Player") && GameManager.Instance != null)
            GameManager.Instance.StopGame();
    }
}
