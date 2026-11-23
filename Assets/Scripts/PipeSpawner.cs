using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Crea parelles de canonades a intervals regulars mentre es juga.
/// Col·loca la canonada de dalt i la de baix perquè el pas tingui sempre la mida "gapSize".
/// </summary>
public class PipeSpawner : MonoBehaviour
{
    [Tooltip("Segons entre canonades")]
    public float timeToSpawn = 1.6f;
    [Tooltip("Alçada del forat entre canonades (unitats)")]
    public float gapSize = 2.7f;
    public GameObject pipePrefab;
    public Transform spawnTop;
    public Transform spawnBottom;

    private float timer;

    void Start()
    {
        timer = timeToSpawn * 0.7f; // la primera canonada surt aviat
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || !gm.IsPlaying) return;

        timer += Time.deltaTime;
        if (timer >= timeToSpawn)
        {
            timer -= timeToSpawn;
            CreateNewPipe();
        }
    }

    void CreateNewPipe()
    {
        // Centre del forat: entre spawnBottom i spawnTop, limitat perquè quedi dins la pantalla
        Camera cam = Camera.main;
        float camTop = cam.transform.position.y + cam.orthographicSize;
        float camBottom = cam.transform.position.y - cam.orthographicSize;
        float minY = Mathf.Max(spawnBottom.position.y, camBottom + gapSize * 0.5f + 1.0f);
        float maxY = Mathf.Min(spawnTop.position.y, camTop - gapSize * 0.5f - 0.6f);
        if (minY > maxY) minY = maxY = (minY + maxY) * 0.5f;
        float gapCenter = Random.Range(minY, maxY);

        Vector3 pos = new Vector3(spawnTop.position.x, 0f, spawnTop.position.z);
        GameObject o = Instantiate(pipePrefab, pos, Quaternion.identity, transform);

        // Ajustar cada tub (els que tenen PipeCollider) segons els límits reals del seu collider
        Physics2D.SyncTransforms();
        foreach (PipeCollider part in o.GetComponentsInChildren<PipeCollider>())
        {
            Collider2D col = part.GetComponent<Collider2D>();
            if (col == null) continue;
            Bounds b = col.bounds;
            float delta = b.center.y > 0f
                ? (gapCenter + gapSize * 0.5f) - b.min.y   // tub de dalt: la seva vora inferior al límit superior del forat
                : (gapCenter - gapSize * 0.5f) - b.max.y;  // tub de baix: la seva vora superior al límit inferior del forat
            part.transform.position += Vector3.up * delta;
        }
    }
}
