using UnityEngine;

/// <summary>
/// Parella de canonades: es mou a velocitat constant (unitats/segon, independent dels fps)
/// i suma exactament un punt quan el seu centre supera l'ocell.
/// </summary>
public class Pipe : MonoBehaviour
{
    private Transform player;
    private bool scored;

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || !gm.IsPlaying) return; // en acabar la partida tot queda aturat

        transform.position += Vector3.left * gm.pipeSpeed * Time.deltaTime;

        if (!scored && player != null && transform.position.x < player.position.x)
        {
            scored = true;
            gm.AddScore(1);
        }

        if (transform.position.x < -15f) Destroy(gameObject);
    }
}
