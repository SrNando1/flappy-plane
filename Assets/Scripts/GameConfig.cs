using UnityEngine;

/// <summary>
/// Todos os números que definem a "sensação" do jogo em um lugar só.
/// Ajuste aqui para deixar mais fácil/difícil sem mexer em lógica.
/// </summary>
[CreateAssetMenu(fileName = "GameConfig", menuName = "Flappy/Game Config")]
public class GameConfig : ScriptableObject
{
    [Header("Avião — dedo no ecrã")]
    [Tooltip("Velocidade vertical máxima ao seguir o dedo.")]
    public float followSpeed = 7.5f;

    [Tooltip("Quão forte o avião puxa para a altura do dedo. Maior = mais colado ao dedo.")]
    public float followResponsiveness = 7f;

    [Tooltip("Aceleração até a velocidade acima. Maior = resposta mais seca.")]
    public float followAccel = 60f;

    [Header("Avião — dedo fora do ecrã")]
    [Tooltip("Peso da queda quando o motor está desligado.")]
    public float gravityScale = 3.2f;
    public float maxFallSpeed = 8f;

    [Header("Avião — inclinação")]
    public float upTilt = 20f;
    public float downTilt = -45f;
    public float tiltSpeed = 8f;

    [Header("Obstáculos")]
    [Tooltip("Velocidade no início da partida, antes de qualquer ponto.")]
    public float obstacleSpeed = 3.2f;
    public float obstacleGap = 2.8f;

    [Tooltip("Distância entre pares, em unidades. O intervalo de spawn é derivado dela " +
             "e da velocidade atual, para o espaçamento não crescer quando o jogo acelera.")]
    public float spawnSpacing = 4.3f;

    public float spawnX = 3.6f;
    public float despawnX = -4.5f;
    public float gapCenterMin = -1.4f;
    public float gapCenterMax = 2f;

    [Tooltip("Até onde o raio tem de chegar por cima. Dentro da faixa de nuvens " +
             "(topo em 5,4), para o raio parecer nascer lá dentro.")]
    public float lightningTopY = 5.6f;

    [Header("Progressão")]
    [Tooltip("Quanto a velocidade sobe por ponto marcado.")]
    public float speedPerPoint = 0.06f;

    [Tooltip("Teto da velocidade. Com os padrões, é atingido por volta dos 55 pontos.")]
    public float maxObstacleSpeed = 6.5f;

    [Tooltip("Suavização: unidades por segundo que a velocidade real leva para alcançar a alvo. " +
             "Sem isto, cada ponto dava um solavanco.")]
    public float speedRamp = 0.6f;

    [Header("Queda")]
    [Tooltip("Quanto tempo a animação de queda tem o ecrã só para si, " +
             "antes de o painel de fim de jogo aparecer por cima.")]
    public float crashDuration = 1.5f;

    [Tooltip("A pausa no instante do embate, com o mundo quase parado e os olhos " +
             "do avião virados para quem joga. Conta-se em tempo real, por isso " +
             "não é apanhada pela própria câmara lenta. Acima de 0,4 s deixa de " +
             "ler-se como susto e passa a parecer que o jogo bloqueou.")]
    public float hitstopDuration = 0.25f;

    [Header("Estendal")]
    [Tooltip("Probabilidade de o prédio 4 trazer estendal, de cada vez que calha. " +
             "Ele é um em cada quatro obstáculos, por isso 0,4 dá mais ou menos um " +
             "estendal a cada dez. Mais raro que isso e há quem nunca o chegue a ver.")]
    [Range(0f, 1f)]
    public float clotheslineChance = 0.4f;

    [Header("Pombo")]
    [Tooltip("Probabilidade de um par de obstáculos trazer um pombo à frente. " +
             "Com a espera abaixo, 0,15 dá mais ou menos um pombo a cada doze pares.")]
    [Range(0f, 1f)]
    public float pigeonChance = 0.15f;

    [Tooltip("Pares que saem sem pombo no início de cada partida. O susto falso só " +
             "tem graça a quem já percebeu o que é um susto a sério.")]
    public int pigeonFirstPair = 4;

    [Tooltip("Pares de espera depois de um pombo. Dois seguidos já não é piada, é praga.")]
    public int pigeonCooldownPairs = 4;

    [Tooltip("Onde o pombo chega ao avião, em fração do espaçamento, contada à frente " +
             "do par com que nasceu. 0,5 é a meio caminho entre dois pares — o sítio " +
             "com mais folga para recuperar do engasgo.")]
    [Range(0.2f, 0.8f)]
    public float pigeonLead = 0.5f;

    [Tooltip("Quanto tempo o motor engasga depois de levar com o pombo.")]
    public float pigeonStallDuration = 0.4f;

    [Tooltip("Peso da queda durante o engasgo. Bem abaixo da gravidade normal: o " +
             "pombo assusta, não mata. Com 1 e 0,4 s o avião desce cerca de 0,8.")]
    public float pigeonStallGravity = 1f;

    [Tooltip("Segundos depois do pombo em que uma morte ainda é culpa dele, " +
             "para a frase do painel de fim de jogo.")]
    public float pigeonBlameWindow = 2f;

    [Header("Moedas")]
    [Tooltip("Probabilidade de cada par de obstáculos trazer uma linha de moedas a seguir.")]
    [Range(0f, 1f)]
    public float coinChance = 0.5f;

    [Tooltip("Mínimo de moedas numa linha.")]
    [Min(1)]
    public int coinCountMin = 1;

    [Tooltip("Máximo de moedas numa linha. Com o espaçamento abaixo, 5 é o que " +
             "cabe no céu aberto entre dois prédios sem encostar em nenhum.")]
    [Min(1)]
    public int coinCountMax = 5;

    [Tooltip("Distância horizontal entre moedas da mesma linha, de centro a centro.")]
    public float coinSpacing = 0.5f;

    [Tooltip("Pares que saem sem moedas no início da partida.")]
    public int coinFirstPair = 1;

    [Header("Mundo")]
    public float groundTopY = -3.88f;
    public float ceilingY = 5.2f;
}
