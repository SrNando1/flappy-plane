using UnityEngine;

/// <summary>
/// O estendal no telhado de um prédio. Não mata nem dá pontos: quem voa rente
/// ao telhado atravessa-o e sai de lá com uma peça de roupa agarrada à hélice
/// (ver <see cref="PlaneLaundry"/>). É um prémio por voar ganancioso.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class Clothesline : MonoBehaviour
{
    /// <summary>
    /// O puxão. O pivô do estendal é a base, por isso rodar é o estendal
    /// inteiro a ser arrastado para a frente pelo avião e a voltar ao sítio.
    /// </summary>
    const float WobbleAngle = 12f;
    const float WobbleFrequency = 26f;
    const float WobbleDamping = 6f;
    const float WobbleDuration = 0.8f;

    bool snagged;
    float wobbleAge = -1f;

    /// <summary>
    /// Verdadeiro só da primeira vez: cada estendal dá uma peça. Sem isto,
    /// voltar a cair para dentro dele trocava a roupa a meio da passagem.
    /// </summary>
    public bool Snag()
    {
        if (snagged) return false;

        snagged = true;
        wobbleAge = 0f;
        return true;
    }

    void Update()
    {
        if (wobbleAge < 0f) return;

        wobbleAge += Time.deltaTime;

        if (wobbleAge >= WobbleDuration)
        {
            transform.localRotation = Quaternion.identity;
            wobbleAge = -1f;
            return;
        }

        // negativo é para a direita: o avião leva o estendal consigo e ele
        // volta atrás a abanar, cada vez menos
        float amount = Mathf.Exp(-wobbleAge * WobbleDamping) * Mathf.Cos(wobbleAge * WobbleFrequency);
        transform.localRotation = Quaternion.Euler(0f, 0f, -WobbleAngle * amount);
    }
}
