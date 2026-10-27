using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// O botão START. Arranca a partida no toque para baixo e não ao soltar, como
/// um Button faria: assim o dedo que carrega já está no ecrã quando a partida
/// começa, e o motor pega logo — soltar primeiro deixava o avião a cair.
/// </summary>
public class StartButton : MonoBehaviour, IPointerDownHandler
{
    public void OnPointerDown(PointerEventData eventData)
    {
        if (GameManager.Instance != null) GameManager.Instance.StartGame();
    }
}
