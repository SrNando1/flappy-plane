using UnityEngine;

/// <summary>
/// Um avião da loja. As medidas vão em fração do sprite, como no SceneBuilder:
/// reexportar a arte noutra resolução não deixa o colisor nem os olhos a flutuar.
/// </summary>
[System.Serializable]
public class PlaneSkin
{
    [Tooltip("Chave de gravação. Não mudar depois de publicado, senão quem já comprou perde o avião.")]
    public string id;

    public string displayName;
    public Sprite sprite;

    [Tooltip("Preço em moedas. Zero é o avião de origem, que já vem na carteira.")]
    [Min(0)]
    public int price;

    [Tooltip("Tamanho da cápsula do colisor, em fração do sprite.")]
    public Vector2 colliderSize = new Vector2(0.87f, 0.41f);

    [Tooltip("Centro da cápsula, em fração do sprite a partir do pivô.")]
    public Vector2 colliderOffset;

    [Tooltip("Onde ficam os olhos do embate — o vidro da cabina.")]
    public Vector2 cockpit = new Vector2(0.17f, 0.3f);

    [Tooltip("Onde a roupa do estendal fica presa — a ponta do nariz.")]
    public Vector2 nose = new Vector2(0.47f, -0.01f);

    [Tooltip("De onde sai o fumo do motor.")]
    public Vector2 nozzle = new Vector2(-0.46f, -0.05f);
}

/// <summary>
/// Os aviões da loja, por ordem de montra. O primeiro é o de origem: é o que
/// voa quando não há nada gravado, ou quando o gravado deixou de existir.
/// </summary>
[CreateAssetMenu(fileName = "PlaneCatalog", menuName = "Flappy/Plane Catalog")]
public class PlaneCatalog : ScriptableObject
{
    public PlaneSkin[] planes;

    public PlaneSkin Default => planes != null && planes.Length > 0 ? planes[0] : null;

    public PlaneSkin Find(string id)
    {
        if (planes == null) return null;

        foreach (var plane in planes)
        {
            if (plane != null && plane.id == id) return plane;
        }
        return null;
    }
}
