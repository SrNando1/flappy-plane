using UnityEngine;

/// <summary>
/// Rola os filhos para a esquerda e recicla o que sai da tela,
/// criando a ilusão de chão/fundo infinito.
/// </summary>
public class ScrollingTiles : MonoBehaviour
{
    [SerializeField] float speedMultiplier = 1f;
    [SerializeField] float tileWidth = 6.72f;
    [SerializeField] bool stopOnGameOver = true;

    Transform[] tiles;
    GameManager game;

    void Awake()
    {
        tiles = new Transform[transform.childCount];
        for (int i = 0; i < tiles.Length; i++) tiles[i] = transform.GetChild(i);
    }

    void Start()
    {
        game = GameManager.Instance;
    }

    void Update()
    {
        if (game == null) return;
        if (stopOnGameOver && game.State == GameState.GameOver) return;

        float dx = game.CurrentSpeed * speedMultiplier * Time.deltaTime;
        float leftEdge = -tileWidth;

        foreach (var tile in tiles)
        {
            tile.Translate(Vector3.left * dx, Space.World);

            if (tile.localPosition.x <= leftEdge)
            {
                tile.localPosition += new Vector3(tileWidth * tiles.Length, 0f, 0f);
            }
        }
    }
}
