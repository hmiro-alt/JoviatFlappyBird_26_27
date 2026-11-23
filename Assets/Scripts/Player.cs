using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Ocell: impuls amb toc/clic/espai, física ajustada, inclinació segons
/// la velocitat vertical i animacions (vol / impacte) lligades a l'estat real del joc.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour
{
    [Header("Física")]
    [Tooltip("Velocitat vertical que rep l'ocell a cada impuls (unitats/segon)")]
    public float flapVelocity = 6.2f;
    [Tooltip("Multiplicador de la gravetat del projecte (-9.81)")]
    public float gravityScale = 2.4f;
    [Tooltip("Velocitat màxima de caiguda (unitats/segon)")]
    public float maxFallSpeed = 10f;

    [Header("Inclinació")]
    public float maxUpAngle = 25f;
    public float maxDownAngle = -80f;
    [Tooltip("Graus/segon quan gira cap amunt (ràpid, com l'original)")]
    public float rotateUpSpeed = 500f;
    [Tooltip("Graus/segon quan gira cap avall (més lent, caiguda progressiva)")]
    public float rotateDownSpeed = 250f;

    [Header("Pantalla d'inici")]
    public float hoverAmplitude = 0.15f;
    public float hoverFrequency = 2f;

    private Rigidbody2D rb;
    private Animator anim;
    private Vector3 startPos;
    private float angle;
    private bool dead;
    private int lastFlapFrame = -1;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        rb.freezeRotation = true;                              // la rotació la controlem nosaltres
        rb.interpolation = RigidbodyInterpolation2D.Interpolate; // moviment suau a qualsevol fps
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.gravityScale = gravityScale;
        rb.simulated = false;                                  // quiet fins que comença la partida

        startPos = transform.position;
        transform.rotation = Quaternion.identity;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        switch (gm.State)
        {
            case GameState.Ready:
                // L'ocell flota esperant el primer toc
                transform.position = startPos + Vector3.up * (Mathf.Sin(Time.time * hoverFrequency * Mathf.PI) * hoverAmplitude);
                if (TapThisFrame()) gm.StartGame();
                break;

            case GameState.Playing:
                if (!dead && TapThisFrame()) Flap();
                break;
        }
    }

    void FixedUpdate()
    {
        if (!rb.simulated) return;

        // Limitar la velocitat de caiguda
        Vector2 v = rb.linearVelocity;
        if (v.y < -maxFallSpeed)
        {
            v.y = -maxFallSpeed;
            rb.linearVelocity = v;
        }

        // Inclinació: cap amunt mentre puja, cap avall progressivament mentre cau
        float target = dead ? maxDownAngle
            : v.y > 0f ? maxUpAngle
            : Mathf.Lerp(maxUpAngle, maxDownAngle, Mathf.InverseLerp(0f, -maxFallSpeed * 0.7f, v.y));
        float rotSpeed = target > angle ? rotateUpSpeed : rotateDownSpeed;
        angle = Mathf.MoveTowards(angle, target, rotSpeed * Time.fixedDeltaTime);
        angle = Mathf.Clamp(angle, maxDownAngle, maxUpAngle); // angles limitats: mai fa un gir complet
        rb.rotation = angle;
    }

    /// <summary>La crida el GameManager quan comença la partida.</summary>
    public void BeginPlay()
    {
        dead = false;
        transform.position = startPos;
        rb.simulated = true;
        rb.gravityScale = gravityScale;
        rb.linearVelocity = Vector2.zero;
        Flap(); // el toc d'inici també fa pujar l'ocell, com a l'original
    }

    private void Flap()
    {
        if (lastFlapFrame == Time.frameCount) return; // mai dos impulsos (ni dos sons) en el mateix frame
        lastFlapFrame = Time.frameCount;
        // Impuls immediat i consistent: fixem la velocitat en lloc de sumar força,
        // així cada toc fa exactament el mateix salt independentment de la caiguda.
        rb.linearVelocity = new Vector2(0f, flapVelocity);
        GameManager.Instance.PlayFlap();
    }

    /// <summary>La crida el GameManager en xocar.</summary>
    public void Die()
    {
        if (dead) return;
        dead = true;
        if (rb.linearVelocity.y > 0f) rb.linearVelocity = Vector2.zero; // cau directament
        if (anim != null) anim.SetTrigger("die");                         // animació d'impacte
    }

    private static bool TapThisFrame()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) return true;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
        if (Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
                if (touch.press.wasPressedThisFrame) return true;
        }
        return false;
    }
}
