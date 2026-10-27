using UnityEngine;

/// <summary>
/// Chuva em três camadas de profundidade. Cada camada é um ParticleSystem
/// próprio, montado em código: basta largar este componente num objeto vazio
/// da cena e dar-lhe o sprite da gota.
///
/// São partículas e não FxParticle de propósito. O rasto do motor larga uns
/// sopros por segundo e cada um vive um instante; a chuva tem centenas de
/// gotas vivas ao mesmo tempo, o tempo todo. Um GameObject por gota daria
/// milhares de Instantiate e Destroy por minuto.
///
/// A gota do sprite é branca também de propósito: a cor é aplicada aqui, o
/// que deixa a mesma arte servir de chuva cinzenta, chuva de noite ou
/// respiga azulada quando cai o relâmpago.
///
/// A chuva não pára no fim de jogo — só endireita. A inclinação vem da
/// velocidade do cenário, e o cenário é que pára.
/// </summary>
public class Rain : MonoBehaviour
{
    [System.Serializable]
    public class Layer
    {
        public string name = "Chuva";

        [Tooltip("Gotas por segundo nesta camada.")]
        public float rate = 80f;

        [Tooltip("Tamanho da gota em unidades de mundo. A largura sai daqui pela " +
                 "proporção do sprite, para a gota nunca ficar espalmada.")]
        public float length = 0.26f;

        [Tooltip("Estica a gota só ao comprido. 1 = proporção original do sprite.")]
        public float stretch = 1.6f;

        [Tooltip("Velocidade de queda. As duas pontas dão o intervalo sorteado por gota — " +
                 "é a variação que impede a chuva de parecer uma grelha.")]
        public float fallSpeedMin = 9f;
        public float fallSpeedMax = 11f;

        [Range(0f, 1f)] public float alpha = 0.3f;

        [Tooltip("Quanto esta camada acompanha a velocidade do cenário. É isto que faz a " +
                 "profundidade: 0 cai a direito, 1 inclina tanto como as torres se mexem.")]
        public float parallax = 0.4f;

        [Tooltip("Onde entra na pilha. Referências da cena: arbustos 0, torres 5, " +
                 "nuvens 8, chão 10, rasto do motor 19, avião 20.")]
        public int sortingOrder = 11;

        [HideInInspector] public ParticleSystem system;
        [HideInInspector] public float lifetime;
    }

    [Header("Arte")]
    [Tooltip("A gota. Branca, ponta para cima — o bojo vai à frente na queda.")]
    [SerializeField] Sprite drop;

    [Tooltip("Cor da chuva. Multiplica o sprite, por isso só escurece: com a gota " +
             "branca dá para ir do branco a qualquer tom.")]
    [SerializeField] Color tint = new Color(0.74f, 0.86f, 0.95f);

    [Tooltip("Deixa vazio para ser criado sozinho. Preenche só se a chuva sumir " +
             "numa build — quer dizer que o shader Sprites/Default não foi incluído.")]
    [SerializeField] Material dropMaterial;

    [SerializeField] string sortingLayerName = "Default";

    [Header("Vento")]
    [Tooltip("Inclinação que existe mesmo com o cenário parado. Positivo empurra " +
             "para a esquerda, no sentido em que o mundo corre.")]
    [SerializeField] float baseWind = 0.8f;

    [Tooltip("Quão depressa a inclinação acompanha as mudanças de velocidade. " +
             "Sem isto, o fim de jogo endireitava a chuva de um frame para o outro.")]
    [SerializeField] float windResponse = 3f;

    [Header("Intensidade")]
    [Tooltip("Multiplica o caudal de todas as camadas. Serve para adensar a chuva " +
             "num pico de tempestade sem mexer nos números de cada camada.")]
    [Range(0f, 3f)]
    [SerializeField] float intensity = 1f;

    [SerializeField]
    Layer[] layers =
    {
        new Layer { name = "Fundo",  rate = 55f, length = 0.17f, stretch = 1.3f,
                    fallSpeedMin = 6f,  fallSpeedMax = 7.5f, alpha = 0.16f,
                    parallax = 0.15f, sortingOrder = 2 },

        new Layer { name = "Média",  rate = 80f, length = 0.26f, stretch = 1.6f,
                    fallSpeedMin = 9f,  fallSpeedMax = 11f,  alpha = 0.3f,
                    parallax = 0.4f,  sortingOrder = 11 },

        new Layer { name = "Frente", rate = 42f, length = 0.46f, stretch = 2.2f,
                    fallSpeedMin = 13f, fallSpeedMax = 16f,  alpha = 0.5f,
                    parallax = 0.85f, sortingOrder = 30 },
    };

    GameManager game;
    Material runtimeMaterial;
    float wind;

    /// <summary>
    /// Adensa ou alivia a chuva sem tocar nas camadas. Útil para a tempestade
    /// apertar quando cai um relâmpago e voltar ao normal a seguir.
    /// </summary>
    public float Intensity
    {
        get => intensity;
        set
        {
            intensity = Mathf.Max(0f, value);
            foreach (var layer in layers) ApplyRate(layer);
        }
    }

    void Awake()
    {
        if (drop == null)
        {
            Debug.LogWarning("Rain: falta o sprite da gota.", this);
            enabled = false;
            return;
        }

        // Cópia própria: escrever a textura no material do Inspector alterava o
        // asset no disco, e a alteração ficava lá depois de sair do play.
        runtimeMaterial = dropMaterial != null
            ? new Material(dropMaterial)
            : new Material(Shader.Find("Sprites/Default"));
        runtimeMaterial.name = "RainDrop (runtime)";
        runtimeMaterial.mainTexture = drop.texture;

        // A chuva já vem inclinada no primeiro frame, senão via-se a inclinação
        // a instalar-se durante o primeiro segundo de jogo.
        wind = baseWind;

        foreach (var layer in layers) Build(layer);
    }

    void OnDestroy()
    {
        if (runtimeMaterial != null) Destroy(runtimeMaterial);
    }

    void Start()
    {
        game = GameManager.Instance;
    }

    void Update()
    {
        // Com o cenário parado sobra só o vento de base, e a chuva vai
        // endireitando em vez de dar um esticão.
        float scroll = game != null && game.State != GameState.GameOver ? game.CurrentSpeed : 0f;
        float target = baseWind + scroll;

        if (!Mathf.Approximately(wind, target))
        {
            wind = Mathf.MoveTowards(wind, target, windResponse * Time.deltaTime);
            foreach (var layer in layers) ApplyWind(layer);
        }
    }

    void Build(Layer layer)
    {
        var cam = Camera.main;
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;
        float dropLength = layer.length * layer.stretch;

        // Nasce acima do topo e morre abaixo do chão, para nunca se ver uma gota
        // a aparecer nem a desaparecer dentro do enquadramento.
        float spawnY = cam.transform.position.y + halfHeight + dropLength;
        float travel = 2f * halfHeight + 2f * dropLength;

        float meanSpeed = 0.5f * (layer.fallSpeedMin + layer.fallSpeedMax);
        layer.lifetime = travel / meanSpeed;

        // Inclinada, a gota sai de lado enquanto cai. A faixa de nascimento tem de
        // ser mais larga do que o ecrã e estar puxada para o lado de onde ela vem,
        // senão abre-se uma coluna sem chuva junto a uma das margens.
        float maxWind = baseWind + MaxScrollSpeed();
        float drift = maxWind * layer.parallax * layer.lifetime;
        float bandWidth = 2f * halfWidth + drift + 1f;
        float bandCenterX = cam.transform.position.x + drift * 0.5f;

        var go = new GameObject("Rain — " + layer.name);
        go.transform.SetParent(transform, false);
        go.transform.position = new Vector3(bandCenterX, spawnY, 0f);

        var ps = go.AddComponent<ParticleSystem>();
        layer.system = ps;
        ps.Stop();

        var main = ps.main;
        main.loop = true;
        main.prewarm = true;              // já está a chover no primeiro frame
        main.playOnAwake = false;
        main.duration = layer.lifetime;
        main.startLifetime = layer.lifetime;
        main.startSpeed = 0f;             // o movimento vem todo do velocityOverLifetime
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = Mathf.CeilToInt(layer.rate * layer.lifetime * 3f) + 32;

        var color = tint;
        color.a = layer.alpha;
        main.startColor = color;

        // O ParticleSystem desenha num quadrado e ignora a proporção da textura.
        // Sem estes dois tamanhos separados, a gota sairia esmagada num quadrado.
        float aspect = drop.rect.width / drop.rect.height;
        main.startSize3D = true;
        main.startSizeX = layer.length * aspect;
        main.startSizeY = dropLength;
        main.startSizeZ = 1f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(bandWidth, 0.01f, 0.01f);
        shape.rotation = Vector3.zero;

        // Os três eixos têm de estar no mesmo modo de curva, senão o Unity
        // recusa o módulo inteiro com "Particle Velocity curves must all be in
        // the same mode" e as gotas ficam paradas. O z não serve para nada num
        // jogo 2D, mas tem de ser declarado na mesma forma que os outros.
        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.y = new ParticleSystem.MinMaxCurve(-layer.fallSpeedMax, -layer.fallSpeedMin);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        var psRenderer = ps.GetComponent<ParticleSystemRenderer>();
        psRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        psRenderer.material = runtimeMaterial;
        psRenderer.sortingLayerName = sortingLayerName;
        psRenderer.sortingOrder = layer.sortingOrder;
        psRenderer.alignment = ParticleSystemRenderSpace.View;

        ApplyRate(layer);
        ApplyWind(layer);
        ps.Play();
    }

    void ApplyRate(Layer layer)
    {
        if (layer.system == null) return;

        var emission = layer.system.emission;
        emission.rateOverTime = layer.rate * intensity;
    }

    void ApplyWind(Layer layer)
    {
        if (layer.system == null) return;

        float sideways = -wind * layer.parallax;

        // Também em TwoConstants, para acompanhar os outros eixos. A folga de
        // 15% dá de borla uma variação de vento por gota, que ajuda a que a
        // chuva não caia toda em linhas paralelas perfeitas.
        float slow = sideways * 0.85f;
        float fast = sideways * 1.15f;

        var velocity = layer.system.velocityOverLifetime;
        velocity.x = new ParticleSystem.MinMaxCurve(Mathf.Min(slow, fast), Mathf.Max(slow, fast));

        // A gota tem de apontar para onde vai: o bojo à frente e a ponta atrás.
        // A rotação sai da própria velocidade, por isso a inclinação acerta-se
        // sozinha sempre que o vento ou a velocidade de queda mudam.
        float fall = 0.5f * (layer.fallSpeedMin + layer.fallSpeedMax);
        var main = layer.system.main;
        main.startRotation = Mathf.Atan2(-sideways, fall);
    }

    float MaxScrollSpeed()
    {
        var config = GameManager.Instance != null ? GameManager.Instance.Config : null;
        return config != null ? config.maxObstacleSpeed : 6.5f;
    }
}
