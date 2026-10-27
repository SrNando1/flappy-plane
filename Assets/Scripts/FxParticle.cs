using UnityEngine;

/// <summary>
/// Partícula descartável movida por código: velocidade, gravidade, rotação,
/// escala e cor ao longo da vida. Serve de faísca, fumo, destroço e onda de choque.
/// Auto-destrói-se no fim.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class FxParticle : MonoBehaviour
{
    public Vector2 velocity;

    [Tooltip("Positivo puxa para baixo. Negativo faz subir, como o fogo e o fumo.")]
    public float gravity;

    public float drag;

    [Tooltip("Velocidade constante somada à de cima, que o arrasto não come — " +
             "o cenário a passar. Sem ela, uma pena travada pelo ar ficava parada " +
             "no ecrã enquanto os prédios corriam por baixo.")]
    public Vector2 drift;

    public float angularVelocity;
    public float lifetime = 1f;

    public float startScale = 1f;
    public float endScale = 1f;

    public Color startColor = Color.white;
    public Color endColor = new Color(1f, 1f, 1f, 0f);

    [Tooltip("Fração da vida em que começa a desvanecer. 0 = desvanece desde o início.")]
    public float fadeStart;

    [Tooltip("Altura do chão. Deixe em -infinito para atravessar.")]
    public float groundY = float.NegativeInfinity;

    public float bounce = 0.35f;

    SpriteRenderer sr;
    float age;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        transform.localScale = Vector3.one * startScale;
        sr.color = startColor;
    }

    void Update()
    {
        float dt = Time.deltaTime;

        age += dt;
        if (age >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        velocity.y -= gravity * dt;
        velocity *= Mathf.Clamp01(1f - drag * dt);

        var p = transform.position + (Vector3)((velocity + drift) * dt);

        if (p.y <= groundY)
        {
            p.y = groundY;
            velocity.y = -velocity.y * bounce;
            velocity.x *= 0.7f;
            angularVelocity *= 0.5f;
        }

        transform.position = p;
        transform.Rotate(0f, 0f, angularVelocity * dt);

        float t = age / lifetime;
        transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, t);
        sr.color = Color.Lerp(startColor, endColor, Mathf.InverseLerp(fadeStart, 1f, t));
    }
}
