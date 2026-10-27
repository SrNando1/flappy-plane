using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A loja: um avião de cada vez, com setas para passar de um para o outro.
/// O botão grande muda conforme o avião — preço se ainda não é teu, SELECT se
/// já é, SELECTED se é o que está a voar.
/// </summary>
public class StorePanel : MonoBehaviour
{
    [Tooltip("O painel inicial, que a loja tapa enquanto está aberta.")]
    [SerializeField] GameObject readyPanel;

    [SerializeField] Image preview;
    [SerializeField] Text nameText;
    [SerializeField] Text walletText;
    [SerializeField] Text pageText;
    [SerializeField] Button actionButton;
    [SerializeField] Text actionLabel;
    [SerializeField] Button prevButton;
    [SerializeField] Button nextButton;

    [Tooltip("O aviso de moedas a menos, por baixo do botão.")]
    [SerializeField] Text hintText;

    static readonly Color Buyable = new Color32(255, 214, 66, 255);
    static readonly Color Owned = new Color32(120, 220, 120, 255);
    static readonly Color Current = new Color32(200, 200, 200, 255);

    GameManager game;
    int index;

    PlaneSkin[] Planes => game != null && game.Planes != null && game.Planes.planes != null
        ? game.Planes.planes
        : new PlaneSkin[0];

    PlaneSkin Shown => Planes.Length > 0 ? Planes[index] : null;

    public void Open()
    {
        game = GameManager.Instance;
        if (game == null || game.State != GameState.Ready) return;

        // abre no avião que está a voar, não no primeiro da lista
        index = Mathf.Max(0, System.Array.IndexOf(Planes, game.SelectedPlane));

        readyPanel.SetActive(false);
        gameObject.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        gameObject.SetActive(false);
        if (game != null && game.State == GameState.Ready) readyPanel.SetActive(true);
    }

    public void Next() => Step(1);
    public void Prev() => Step(-1);

    void Step(int delta)
    {
        if (Planes.Length == 0) return;
        index = (index + delta + Planes.Length) % Planes.Length;
        Refresh();
    }

    /// <summary>Comprar e escolher são o mesmo toque: quem paga quer voar nele.</summary>
    public void OnAction()
    {
        var plane = Shown;
        if (plane == null) return;

        if (!game.Owns(plane) && !game.Buy(plane))
        {
            hintText.text = "NOT ENOUGH COINS";
            hintText.enabled = true;
            return;
        }

        game.Select(plane);
        Refresh();
    }

    void Refresh()
    {
        var plane = Shown;
        hintText.enabled = false;
        walletText.text = "COINS  " + game.TotalCoins;

        bool several = Planes.Length > 1;
        prevButton.gameObject.SetActive(several);
        nextButton.gameObject.SetActive(several);
        pageText.text = several ? (index + 1) + " / " + Planes.Length : "";

        if (plane == null)
        {
            nameText.text = "";
            preview.enabled = false;
            actionButton.interactable = false;
            actionLabel.text = "";
            return;
        }

        nameText.text = plane.displayName;
        preview.sprite = plane.sprite;
        preview.enabled = plane.sprite != null;
        preview.preserveAspect = true;

        var image = actionButton.targetGraphic as Image;

        if (plane == game.SelectedPlane)
        {
            actionLabel.text = "SELECTED";
            actionButton.interactable = false;
            if (image != null) image.color = Current;
        }
        else if (game.Owns(plane))
        {
            actionLabel.text = "SELECT";
            actionButton.interactable = true;
            if (image != null) image.color = Owned;
        }
        else
        {
            actionLabel.text = "BUY  " + plane.price;
            actionButton.interactable = true;
            if (image != null) image.color = Buyable;
        }
    }
}
