using UnityEngine;

/// <summary>
/// A peça de roupa que o avião traz agarrada à hélice depois de atravessar um
/// estendal. Fica até ao fim da partida, a esvoaçar — é a piada que dura, e a
/// razão para ir procurar o próximo estendal. Um estendal novo troca a peça,
/// não acumula: três em cima de um avião de 1,25 unidades eram um borrão.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class PlaneLaundry : MonoBehaviour
{
    [Tooltip("As peças que podem calhar. O SceneBuilder enche isto sozinho com " +
             "tudo o que estiver em Assets/Art/RoupaEstendal.")]
    [SerializeField] Sprite[] garments;

    [Tooltip("Onde a peça fica presa, em coordenadas locais do avião: o cubo da hélice.")]
    [SerializeField] Vector2 pinOffset;

    /// <summary>
    /// Quase deitada para trás: o vento vem de frente e leva a roupa ao longo
    /// do nariz. Deitada de todo parecia colada; dezoito graus de queda é o que
    /// a faz ler-se como pano e não como autocolante.
    /// </summary>
    const float TrailAngle = -72f;

    /// <summary>O esvoaçar: duas ondas de ritmos que não batem certo, para não se ver o ciclo.</summary>
    const float FlapSlow = 17f;
    const float FlapFast = 29f;
    const float FlapAngle = 9f;
    const float FlapStretch = 0.12f;

    /// <summary>Quanto tempo a peça leva a aparecer. Instantânea não se dava por ela.</summary>
    const float PopDuration = 0.14f;

    Transform pin;
    SpriteRenderer worn;
    float popAge;

    /// <summary>Veste uma peça, diferente da que lá estiver, se houver por onde escolher.</summary>
    public void Wear()
    {
        var next = Pick();
        if (next == null) return;

        EnsureRenderer();

        worn.sprite = next;
        // o sprite tem pivô ao centro, para rodar direito quando voar nos
        // destroços. Aqui fica pendurado pela ponta de cima: desce meia altura.
        worn.transform.localPosition = new Vector3(0f, -next.bounds.extents.y, 0f);
        worn.enabled = true;
        popAge = 0f;
    }

    /// <summary>A partida acabou: a peça solta-se e voa com os destroços.</summary>
    public void LetGo(CrashEffects crash)
    {
        if (worn == null || !worn.enabled) return;

        if (crash != null) crash.ThrowLoose(worn.sprite, worn.bounds.center);
        worn.enabled = false;
    }

    void Update()
    {
        if (worn == null || !worn.enabled) return;

        // Time.time e não tempo real: na pausa do embate a roupa congela com o
        // resto do mundo, em vez de continuar a abanar num avião parado
        float t = Time.time;
        float flap = Mathf.Sin(t * FlapSlow) * FlapAngle + Mathf.Sin(t * FlapFast) * FlapAngle * 0.45f;
        pin.localRotation = Quaternion.Euler(0f, 0f, TrailAngle + flap);

        popAge += Time.deltaTime;
        float pop = Mathf.Clamp01(popAge / PopDuration);
        // um nadinha acima de 1 a meio do caminho: a peça "agarra" em vez de só aparecer
        float grow = pop < 1f ? Mathf.Sin(pop * Mathf.PI * 0.62f) / Mathf.Sin(Mathf.PI * 0.62f) : 1f;

        float stretch = 1f + Mathf.Sin(t * (FlapFast - 6f)) * FlapStretch;
        worn.transform.localScale = new Vector3(stretch * grow, grow, 1f);
    }

    /// <summary>Muda o sítio do alfinete — para quando o avião troca de desenho.</summary>
    public void SetPin(Vector2 offset)
    {
        pinOffset = offset;
        if (pin != null) pin.localPosition = pinOffset;
    }

    Sprite Pick()
    {
        if (garments == null || garments.Length == 0) return null;

        var current = worn != null && worn.enabled ? worn.sprite : null;

        // até oito tentativas a fugir da peça atual; com uma só peça na pasta
        // (ou só nulas), fica a que houver
        Sprite pick = null;
        for (int i = 0; i < 8; i++)
        {
            var candidate = garments[Random.Range(0, garments.Length)];
            if (candidate == null) continue;

            pick = candidate;
            if (candidate != current) break;
        }

        return pick;
    }

    /// <summary>
    /// O alfinete e a peça nascem só no primeiro estendal. A maior parte das
    /// partidas nunca chega a um, e ficam sem nada pendurado no avião.
    /// </summary>
    void EnsureRenderer()
    {
        if (worn != null) return;

        pin = new GameObject("LaundryPin").transform;
        pin.SetParent(transform, false);
        pin.localPosition = pinOffset;

        var garment = new GameObject("Garment");
        garment.transform.SetParent(pin, false);

        worn = garment.AddComponent<SpriteRenderer>();
        // à frente do avião e dos olhos do embate
        worn.sortingOrder = GetComponent<SpriteRenderer>().sortingOrder + 2;
    }
}
