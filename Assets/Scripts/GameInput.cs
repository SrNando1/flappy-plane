using UnityEngine;

/// <summary>
/// Uma única fonte de verdade para o input.
/// Cobre toque, rato (no Editor) e barra de espaço.
/// </summary>
public static class GameInput
{
    /// <summary>
    /// Fração da altura do ecrã, a contar de baixo, reservada ao banner.
    /// É o <see cref="AdsService"/> que a define; zero quando não há banner.
    /// </summary>
    public static float BannerSafeFraction;

    /// <summary>
    /// Toque que conta para iniciar ou reiniciar, ignorando a faixa do banner.
    /// Sem isto, o dedo que reinicia o jogo acerta no anúncio — e cliques
    /// acidentais são motivo de suspensão na AdMob.
    /// </summary>
    public static bool TapDownInPlayArea
    {
        get
        {
            if (Input.GetKeyDown(KeyCode.Space)) return true;
            if (!TapDown) return false;
            return ScreenPosition.y > Screen.height * BannerSafeFraction;
        }
    }

    /// <summary>Primeiro toque deste frame — inicia e reinicia a partida.</summary>
    public static bool TapDown
    {
        get
        {
            if (Input.GetKeyDown(KeyCode.Space)) return true;
            if (Input.GetMouseButtonDown(0)) return true;

            for (int i = 0; i < Input.touchCount; i++)
            {
                if (Input.GetTouch(i).phase == TouchPhase.Began) return true;
            }
            return false;
        }
    }

    /// <summary>Dedo mantido no ecrã — é o motor do avião.</summary>
    public static bool Held
    {
        get
        {
            if (Input.GetKey(KeyCode.Space)) return true;
            if (Input.GetMouseButton(0)) return true;
            return Input.touchCount > 0;
        }
    }

    /// <summary>Posição em pixels do dedo, ou do rato quando não há toque.</summary>
    public static Vector2 ScreenPosition
    {
        get
        {
            if (Input.touchCount > 0) return Input.GetTouch(0).position;
            return Input.mousePosition;
        }
    }
}
