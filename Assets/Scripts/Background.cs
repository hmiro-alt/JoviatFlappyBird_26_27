using UnityEngine;

/// <summary>
/// Fons amb efecte de paral·laxi: es desplaça cap a l'esquerra més lent que les canonades.
/// Crea còpies del sprite una al costat de l'altra i, quan una surt per l'esquerra de la càmera,
/// la torna a col·locar darrere de la més a la dreta, de manera que la repetició és contínua.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class Background : MonoBehaviour
{
    [Tooltip("Velocitat del fons (unitats/segon). Ha de ser menor que GameManager.pipeSpeed")]
    public float speed = 0.6f;
    [Tooltip("Petit solapament entre còpies per evitar línies d'1 píxel")]
    public float overlap = 0.01f;

    private Transform[] tiles;
    private float width;
    private Camera cam;

    void Start()
    {
        cam = Camera.main;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        sr.enabled = true;
        width = sr.bounds.size.x - overlap;

        float camWidth = cam.orthographicSize * 2f * cam.aspect;
        int count = Mathf.Max(2, Mathf.CeilToInt(camWidth / width) + 1);

        tiles = new Transform[count];
        tiles[0] = transform;
        for (int i = 1; i < count; i++)
        {
            var go = new GameObject(name + "_copy" + i);
            go.layer = gameObject.layer;
            go.transform.SetParent(transform.parent, false);
            go.transform.localScale = transform.localScale;
            go.transform.rotation = transform.rotation;
            go.transform.position = transform.position + Vector3.right * width * i;

            var copy = go.AddComponent<SpriteRenderer>();
            copy.sprite = sr.sprite;
            copy.color = sr.color;
            copy.sharedMaterial = sr.sharedMaterial;
            copy.sortingLayerID = sr.sortingLayerID;
            copy.sortingOrder = sr.sortingOrder;
            tiles[i] = go.transform;
        }
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm != null && gm.State == GameState.GameOver) return; // s'atura en acabar la partida

        float dx = speed * Time.deltaTime;
        float camLeft = cam.transform.position.x - cam.orthographicSize * cam.aspect;

        foreach (Transform t in tiles)
            t.position += Vector3.left * dx;

        foreach (Transform t in tiles)
        {
            if (t.position.x + width * 0.5f < camLeft)
            {
                float rightmost = float.MinValue;
                foreach (Transform o in tiles) rightmost = Mathf.Max(rightmost, o.position.x);
                t.position = new Vector3(rightmost + width, t.position.y, t.position.z);
            }
        }
    }
}
