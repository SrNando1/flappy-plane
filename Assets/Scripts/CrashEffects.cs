using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sorteia e toca uma das animações de queda. Cada variante é só um punhado
/// de <see cref="FxParticle"/> configurados de maneira diferente.
/// </summary>
public class CrashEffects : MonoBehaviour
{
    [SerializeField] GameConfig config;
    [SerializeField] ScreenFlash flash;

    [Header("Sprites")]
    [SerializeField] Sprite spark;
    [SerializeField] Sprite ring;
    [Tooltip("As peças vetorizadas do avião — corpo, hélice, rodas, janelas... " +
             "O SceneBuilder enche isto sozinho com tudo o que saiu da prancheta, " +
             "menos o chapéu, que tem tratamento à parte.")]
    [SerializeField] Sprite[] planeParts;

    [Tooltip("O chapéu do avião. Fica de fora da explosão de propósito: cai " +
             "sozinho, depois de tudo o resto já ter assentado. Vazio: não há gag.")]
    [SerializeField] Sprite hat;

    /// <summary>Um prédio e os cacos em que a fachada dele se parte.</summary>
    [System.Serializable]
    public class TowerDebris
    {
        public Sprite tower;
        public Sprite[] pieces;
    }

    [Tooltip("Que cacos pertencem a que prédio. O SceneBuilder enche isto " +
             "sozinho com as pastas 'Predio<n>Destrocos' que existirem — " +
             "prédio que não esteja nesta lista não desaba.")]
    [SerializeField] TowerDebris[] towerDebris;

    const int OrderBack = 29;
    const int OrderMain = 30;
    const int OrderFront = 31;

    /// <summary>
    /// Com que força o chapéu é atirado ao ar e com que peso volta. Os dois
    /// números só existem um para o outro: juntos dão pouco mais de um segundo
    /// no ar, que é o tempo que os destroços levam a assentar e ainda cabe
    /// dentro do <c>crashDuration</c>, antes de o painel do fim subir.
    /// </summary>
    const float HatLaunch = 4f;
    const float HatGravity = 11f;

    CameraShake shake;
    int lastVariant = -1;

    void Start()
    {
        if (Camera.main != null) shake = Camera.main.GetComponent<CameraShake>();
    }

    public void Play(Vector3 at, Sprite planeSprite, bool keepScreenClear = false)
    {
        int variant = Random.Range(0, 4);
        if (variant == lastVariant) variant = (variant + 1 + Random.Range(0, 3)) % 4;

        // com a fachada a desabar, a bola de fogo do Boom tapava tudo
        if (keepScreenClear && variant == 0) variant = 1 + Random.Range(0, 3);

        lastVariant = variant;

        switch (variant)
        {
            case 0: Boom(at); break;
            case 1: Debris(at); break;
            case 2: Spiral(at, planeSprite); break;
            default: BoltPop(at); break;
        }

        // o chapéu não entra em nenhuma delas: vai sempre por fora, e por fim
        if (hat != null) StartCoroutine(HatDrop(at));
    }

    // ----------------------------------------------------------- o chapéu

    /// <summary>
    /// A lei mais velha do desenho animado: o chapéu cai por último. Sai do
    /// mesmo sítio que o resto, mas atirado ao ar de leve — enquanto os
    /// destroços caem, ressaltam e assentam, ele ainda vai lá em cima. Aterra
    /// no silêncio a seguir, e é a última coisa que se mexe no ecrã.
    /// </summary>
    IEnumerator HatDrop(Vector3 at)
    {
        var piece = Spawn(hat, at, OrderFront);
        piece.velocity = new Vector2(Random.Range(-0.7f, 0.7f), HatLaunch);
        piece.gravity = HatGravity;
        // roda devagar, ao contrário de tudo o resto: um chapéu a girar como
        // um caco perdia o ar de coisa leve que ficou para trás
        piece.angularVelocity = Random.Range(-120f, 120f);
        piece.lifetime = 3.4f;
        piece.fadeStart = 0.92f;
        piece.groundY = config.groundTopY + 0.05f;
        piece.bounce = 0.12f;
        piece.endColor = new Color(1f, 1f, 1f, 0f);

        // esperar que assente em vez de contar o tempo à mão: a altura da
        // batida muda de partida para partida, a queda também. O mesmo
        // padrão do <see cref="TrailThenImpact"/>.
        float ground = config.groundTopY + 0.14f;
        float deadline = Time.time + 3f;

        while (piece != null && piece.transform.position.y > ground && Time.time < deadline)
        {
            yield return null;
        }

        if (piece == null) yield break;

        DustPuffs(piece.transform.position, 4);
    }

    // ------------------------------------------------------------ desabamento

    /// <summary>
    /// A fachada do prédio desaba. Só desabam os prédios que têm cacos
    /// cortados; os outros nem sequer se apagam da tela.
    /// </summary>
    /// <param name="impact">Onde o avião bateu, em coordenadas de mundo.</param>
    /// <param name="tower">Os limites do prédio na tela, já com o esticão aplicado.</param>
    /// <param name="towerSprite">O prédio que calhou a este obstáculo.</param>
    /// <returns>Falso se este prédio não é dos que se desfazem.</returns>
    public bool PlayTowerCollapse(Vector2 impact, Bounds tower, Sprite towerSprite)
    {
        var pieces = DebrisFor(towerSprite);
        if (pieces == null) return false;

        Shake(0.45f, 0.7f);

        float top = tower.max.y;
        float bottom = Mathf.Max(config.groundTopY, tower.min.y);

        for (int i = 0; i < pieces.Length; i++)
        {
            var sprite = pieces[i];
            if (sprite == null) continue;

            // cada caco nasce à sua altura, escalonado do telhado até ao chão.
            // Largá-los todos no ponto da batida dava uma baforada; é a coluna
            // inteira a ceder que se lê como prédio a ruir.
            float t = (i + 0.5f) / pieces.Length;
            var at = new Vector3(
                tower.center.x + Random.Range(-0.18f, 0.18f),
                Mathf.Lerp(top, bottom, t),
                0f);

            CollapsePiece(sprite, at, impact);
        }

        SmokePuffs(new Vector3(impact.x, impact.y, 0f), 12);
        return true;
    }

    /// <summary>
    /// Os cacos deste prédio, ou nulo se não é dos que se desfazem. A chave é a
    /// referência do sprite — comparar nomes partia-se assim que alguém
    /// renomeasse um ficheiro.
    /// </summary>
    Sprite[] DebrisFor(Sprite towerSprite)
    {
        if (towerSprite == null || towerDebris == null) return null;

        foreach (var entry in towerDebris)
        {
            if (entry != null && entry.tower == towerSprite &&
                entry.pieces != null && entry.pieces.Length > 0)
            {
                return entry.pieces;
            }
        }

        return null;
    }

    /// <summary>
    /// Atira um caco da fachada. O empurrão sai do ponto da batida e perde
    /// força com a distância: quem estava ao pé do impacto é cuspido, quem
    /// estava lá em baixo mais desaba do que voa. O peso vem do tamanho do
    /// próprio caco, como no <see cref="ThrowPart"/>.
    /// </summary>
    void CollapsePiece(Sprite sprite, Vector3 at, Vector2 impact)
    {
        float heft = Mathf.Clamp01(sprite.bounds.size.magnitude / 0.9f);

        var away = (Vector2)at - impact;
        float reach = Mathf.Clamp01(1f - away.magnitude / 3f);
        var dir = away.sqrMagnitude < 0.0001f
            ? Random.insideUnitCircle.normalized
            : away.normalized;

        var piece = Spawn(sprite, at, OrderMain);
        piece.velocity = dir * Mathf.Lerp(1f, 7f, reach) * Mathf.Lerp(1.3f, 0.75f, heft)
                       + Vector2.up * Random.Range(0.5f, 2f);
        piece.gravity = 15f;
        piece.angularVelocity = Random.Range(-380f, 380f) * Mathf.Lerp(1.4f, 0.6f, heft);
        piece.lifetime = Random.Range(2.4f, 3.2f);
        piece.fadeStart = 0.78f;
        piece.groundY = config.groundTopY + 0.08f;
        piece.bounce = 0.32f;
        piece.endColor = new Color(1f, 1f, 1f, 0f);
    }

    // ------------------------------------------------------------- variantes

    /// <summary>Bola de fogo que engole o ecrã.</summary>
    void Boom(Vector3 at)
    {
        Flash(new Color(1f, 0.96f, 0.84f, 0.95f), 0.4f);
        Shake(0.6f, 1f);

        // onda de choque
        var wave = Spawn(ring, at, OrderFront);
        wave.startScale = 0.3f;
        wave.endScale = 18f;
        wave.startColor = new Color(1f, 0.92f, 0.62f, 0.9f);
        wave.endColor = new Color(1f, 0.7f, 0.3f, 0f);
        wave.lifetime = 0.6f;

        // três bolas enormes cobrem o ecrã
        for (int i = 0; i < 3; i++)
        {
            var blast = Spawn(spark, at + (Vector3)(Random.insideUnitCircle * 0.5f), OrderMain);
            blast.startScale = 3f;
            blast.endScale = Random.Range(20f, 27f);
            blast.lifetime = Random.Range(0.5f, 0.7f);
            blast.startColor = new Color(1f, 0.82f, 0.28f, 0.95f);
            blast.endColor = new Color(0.85f, 0.25f, 0.08f, 0f);
        }

        // brasas a voar
        for (int i = 0; i < 34; i++)
        {
            var dir = Random.insideUnitCircle.normalized;
            var ember = Spawn(spark, at + (Vector3)(dir * Random.Range(0f, 0.3f)), OrderFront);
            ember.velocity = dir * Random.Range(3f, 12f);
            ember.drag = 2.2f;
            ember.gravity = -1.2f;
            ember.lifetime = Random.Range(0.45f, 0.95f);
            ember.startScale = Random.Range(1.2f, 3.2f);
            ember.endScale = 0.2f;
            ember.startColor = FireColor();
            ember.endColor = new Color(0.35f, 0.08f, 0.04f, 0f);
        }

        SmokePuffs(at, 18);
    }

    /// <summary>O avião desmancha-se e as peças caem e ressaltam no chão.</summary>
    void Debris(Vector3 at)
    {
        Flash(new Color(1f, 1f, 1f, 0.4f), 0.16f);
        Shake(0.3f, 0.6f);

        // o avião inteiro: sai tudo o que o compõe, peça a peça
        foreach (var sprite in AllParts())
        {
            ThrowPart(sprite, at + (Vector3)(Random.insideUnitCircle * 0.18f));
        }

        for (int i = 0; i < 14; i++)
        {
            var dir = Random.insideUnitCircle.normalized;
            var ember = Spawn(spark, at, OrderFront);
            ember.velocity = dir * Random.Range(2f, 6f);
            ember.drag = 3f;
            ember.gravity = 2f;
            ember.lifetime = Random.Range(0.25f, 0.5f);
            ember.startScale = Random.Range(0.8f, 1.8f);
            ember.endScale = 0.1f;
            ember.startColor = FireColor();
            ember.endColor = new Color(0.4f, 0.1f, 0.05f, 0f);
        }

        SmokePuffs(at, 8);
    }

    /// <summary>Perde o controlo, roda e mergulha deixando rasto de fumo.</summary>
    void Spiral(Vector3 at, Sprite planeSprite)
    {
        Shake(0.18f, 0.35f);

        var wreck = Spawn(planeSprite, at, OrderMain);
        wreck.velocity = new Vector2(-1.6f, 3.6f);
        wreck.gravity = 10f;
        wreck.angularVelocity = Random.value < 0.5f ? -960f : 960f;
        wreck.lifetime = 3f;
        wreck.fadeStart = 0.95f;
        wreck.groundY = config.groundTopY + 0.15f;
        wreck.bounce = 0f;

        StartCoroutine(TrailThenImpact(wreck.transform));
    }

    IEnumerator TrailThenImpact(Transform wreck)
    {
        float deadline = Time.time + 2.5f;
        float groundY = config.groundTopY + 0.25f;

        while (wreck != null && wreck.position.y > groundY && Time.time < deadline)
        {
            var puff = Spawn(spark, wreck.position, OrderBack);
            puff.velocity = new Vector2(Random.Range(-0.4f, 0.4f), Random.Range(0.3f, 0.9f));
            puff.lifetime = Random.Range(0.6f, 1.1f);
            puff.startScale = Random.Range(1f, 1.8f);
            puff.endScale = Random.Range(3f, 4.5f);
            puff.startColor = new Color(0.42f, 0.42f, 0.48f, 0.75f);
            puff.endColor = new Color(0.6f, 0.6f, 0.66f, 0f);

            yield return new WaitForSeconds(0.045f);
        }

        if (wreck == null) yield break;
        var at = wreck.position;

        Shake(0.35f, 0.6f);
        Flash(new Color(1f, 0.9f, 0.7f, 0.5f), 0.2f);

        for (int i = 0; i < 18; i++)
        {
            var dir = new Vector2(Random.Range(-1f, 1f), Random.Range(0.1f, 1f)).normalized;
            var ember = Spawn(spark, at, OrderFront);
            ember.velocity = dir * Random.Range(2.5f, 7f);
            ember.drag = 2.5f;
            ember.gravity = 4f;
            ember.lifetime = Random.Range(0.35f, 0.7f);
            ember.startScale = Random.Range(1.2f, 2.6f);
            ember.endScale = 0.2f;
            ember.startColor = FireColor();
            ember.endColor = new Color(0.35f, 0.08f, 0.04f, 0f);
        }

        SmokePuffs(at, 10);
    }

    /// <summary>Cómica: o avião simplesmente desmonta-se numa chuva de miudezas.</summary>
    void BoltPop(Vector3 at)
    {
        Flash(new Color(1f, 1f, 1f, 0.55f), 0.13f);
        Shake(0.22f, 0.45f);

        var puff = Spawn(ring, at, OrderMain);
        puff.startScale = 0.4f;
        puff.endScale = 7f;
        puff.lifetime = 0.4f;
        puff.startColor = new Color(1f, 1f, 1f, 0.9f);
        puff.endColor = new Color(1f, 1f, 1f, 0f);

        // rodas, farolim, antena... aos molhos e a repetir, que é o que faz
        // a piada: o avião era feito daquilo e desatou tudo ao mesmo tempo
        var bits = SmallestParts();

        for (int i = 0; i < 28 && bits.Count > 0; i++)
        {
            var dir = Random.insideUnitCircle.normalized;
            var nut = Spawn(bits[Random.Range(0, bits.Count)], at, OrderFront);
            nut.velocity = dir * Random.Range(2.5f, 8f) + Vector2.up * 2.5f;
            nut.gravity = 17f;
            nut.angularVelocity = Random.Range(-760f, 760f);
            nut.lifetime = Random.Range(2f, 2.8f);
            nut.fadeStart = 0.82f;
            nut.groundY = config.groundTopY + 0.05f;
            nut.bounce = 0.45f;
            nut.endColor = new Color(1f, 1f, 1f, 0f);
        }

        // e três peças ao acaso por cima, para se perceber que era um avião
        foreach (var sprite in SomeParts(3))
        {
            ThrowPart(sprite, at);
        }
    }

    /// <summary>Todas as peças, saltando buracos na lista.</summary>
    IEnumerable<Sprite> AllParts()
    {
        if (planeParts == null) yield break;

        foreach (var sprite in planeParts)
        {
            if (sprite != null) yield return sprite;
        }
    }

    /// <summary>
    /// A metade mais miúda das peças — rodas, farolim, antena e companhia.
    /// Sai da mediana em vez de uma lista escrita à mão, para continuar a
    /// acertar se a prancheta ganhar ou perder peças.
    /// </summary>
    List<Sprite> SmallestParts()
    {
        var all = new List<Sprite>(AllParts());
        if (all.Count == 0) return all;

        all.Sort((a, b) => a.bounds.size.sqrMagnitude.CompareTo(b.bounds.size.sqrMagnitude));
        return all.GetRange(0, Mathf.Max(1, all.Count / 2));
    }

    /// <summary>Sorteia peças diferentes umas das outras, sem repetir.</summary>
    IEnumerable<Sprite> SomeParts(int count)
    {
        var bag = new List<Sprite>(AllParts());

        for (int i = 0; i < count && bag.Count > 0; i++)
        {
            int pick = Random.Range(0, bag.Count);
            yield return bag[pick];
            bag.RemoveAt(pick);
        }
    }

    /// <summary>
    /// Atira uma peça ao ar. As peças vetorizadas têm tamanhos muito
    /// diferentes — a fuselagem ao lado de um farolim — por isso o peso sai do
    /// próprio sprite: o que é miúdo voa mais longe e roda mais depressa,
    /// o que é grande tomba pesado.
    /// </summary>
    void ThrowPart(Sprite sprite, Vector3 at)
    {
        float heft = Mathf.Clamp01(sprite.bounds.size.magnitude / 0.9f);

        var piece = Spawn(sprite, at, OrderMain);
        piece.velocity = new Vector2(Random.Range(-4f, 4f), Random.Range(2.5f, 7f))
                       * Mathf.Lerp(1.35f, 0.8f, heft);
        piece.gravity = 15f;
        piece.angularVelocity = Random.Range(-620f, 620f) * Mathf.Lerp(1.5f, 0.6f, heft);
        piece.lifetime = 2.8f;
        piece.fadeStart = 0.8f;
        piece.groundY = config.groundTopY + 0.08f;
        piece.bounce = 0.38f;
        piece.endColor = new Color(1f, 1f, 1f, 0f);
    }

    /// <summary>
    /// Atira ao ar uma coisa solta que não é peça do avião — a roupa que ia na
    /// hélice, o estendal de um prédio que desabou. Voa com as regras das peças:
    /// o peso sai do tamanho do próprio sprite.
    /// </summary>
    public void ThrowLoose(Sprite sprite, Vector3 at)
    {
        if (sprite != null) ThrowPart(sprite, at);
    }

    // -------------------------------------------------------------- pedaços

    void SmokePuffs(Vector3 at, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var smoke = Spawn(spark, at + (Vector3)(Random.insideUnitCircle * 0.6f), OrderBack);
            smoke.velocity = new Vector2(Random.Range(-1.2f, 1.2f), Random.Range(0.6f, 2.2f));
            smoke.drag = 1.2f;
            smoke.lifetime = Random.Range(0.9f, 1.6f);
            smoke.startScale = Random.Range(2f, 4f);
            smoke.endScale = Random.Range(6f, 9f);
            smoke.startColor = new Color(0.35f, 0.35f, 0.4f, 0.7f);
            smoke.endColor = new Color(0.62f, 0.62f, 0.68f, 0f);
        }
    }

    /// <summary>
    /// O pozinho que salta quando o chapéu assenta. É a versão pequena do
    /// <see cref="SmokePuffs"/>: as baforadas de lá tapavam um chapéu inteiro.
    /// </summary>
    void DustPuffs(Vector3 at, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var dust = Spawn(spark, at + (Vector3)(Random.insideUnitCircle * 0.12f), OrderBack);
            dust.velocity = new Vector2(Random.Range(-0.7f, 0.7f), Random.Range(0.15f, 0.5f));
            dust.drag = 2.4f;
            dust.lifetime = Random.Range(0.4f, 0.7f);
            dust.startScale = Random.Range(0.5f, 0.9f);
            dust.endScale = Random.Range(1.4f, 2.1f);
            dust.startColor = new Color(0.66f, 0.63f, 0.56f, 0.55f);
            dust.endColor = new Color(0.72f, 0.7f, 0.64f, 0f);
        }
    }

    static Color FireColor()
    {
        switch (Random.Range(0, 3))
        {
            case 0: return new Color(1f, 0.92f, 0.4f, 1f);
            case 1: return new Color(1f, 0.62f, 0.16f, 1f);
            default: return new Color(0.95f, 0.32f, 0.12f, 1f);
        }
    }

    FxParticle Spawn(Sprite sprite, Vector3 at, int sortingOrder)
    {
        var go = new GameObject("Fx");
        go.transform.SetParent(transform, false);
        go.transform.position = at;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = sortingOrder;

        return go.AddComponent<FxParticle>();
    }

    void Flash(Color color, float duration)
    {
        if (flash != null) flash.Flash(color, duration);
    }

    void Shake(float strength, float duration)
    {
        if (shake != null) shake.Shake(strength, duration);
    }
}
