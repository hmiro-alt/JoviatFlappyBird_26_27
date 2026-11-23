using UnityEngine;

/// <summary>Límits superior i inferior: si l'ocell els toca, s'acaba la partida.</summary>
public class Border : MonoBehaviour
{
    public GameManager gm;

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (!other.gameObject.CompareTag("Player")) return;
        GameManager manager = gm != null ? gm : GameManager.Instance;
        if (manager != null) manager.StopGame();
    }
}
