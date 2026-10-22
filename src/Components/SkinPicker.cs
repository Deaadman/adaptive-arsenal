using AdaptiveArsenal.Core;
using AdaptiveArsenal.Utilities;

namespace AdaptiveArsenal.Components;

/// <summary>
/// A row on the item description page, beside the inventory, for picking a weapon's skin: the skin's name in the
/// middle, with arrows either side to step through the skins for that weapon and its own look.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Patches.ItemDescriptionPagePatches"/> adds it to each page the first time the page shows an item, and
/// calls <see cref="Show"/> whenever the page shows one. The row only shows for weapons that have skins.
/// </para>
/// <para>
/// The labels are copies of the page's favourite label, so they match the game's font, and the arrows are plain
/// <c>&lt;</c> and <c>&gt;</c> labels, so they need no sprites. Where the row sits is a guess from the page's layout:
/// <see cref="OffsetAboveFavorite"/> and <see cref="ArrowSpacing"/> move it.
/// </para>
/// </remarks>
[RegisterTypeInIl2Cpp(false)]
public class SkinPicker : MonoBehaviour
{
    /// <summary>How far above the page's favourite checkbox the row sits, in NGUI pixels.</summary>
    private const float OffsetAboveFavorite = 45f;

    /// <summary>How far each arrow is from the middle of the row, in NGUI pixels.</summary>
    private const float ArrowSpacing = 130f;

    /// <summary>How wide the skin's name can be before it's shrunk to fit, in NGUI pixels.</summary>
    private const int TitleWidth = 200;

    /// <summary>What the row says for a weapon's own look.</summary>
    private const string DefaultName = "Default";

    /// <summary>The colour of an arrow under the mouse.</summary>
    private static readonly Color s_HoverColor = Color.white;

    private GameObject? m_Row;
    private UILabel? m_Title;
    private UILabel? m_LeftArrow;
    private UILabel? m_RightArrow;

    /// <summary>The colour of an arrow that isn't under the mouse: the label's own.</summary>
    private Color m_ArrowColor;

    /// <summary>The weapon the row is for, or <see langword="null"/> when it's hidden.</summary>
    private GearItem? m_GearItem;

    /// <summary>The skins the row steps through: <see langword="null"/> first, for the weapon's own look.</summary>
    private readonly List<SkinDefinition?> m_Options = [];

    /// <summary>Which of <see cref="m_Options"/> the weapon wears.</summary>
    private int m_Index;

    /// <summary>Builds the row on a description page. Returns <see langword="false"/> if the page has nothing to copy.</summary>
    /// <param name="page">The page.</param>
    internal bool Build(ItemDescriptionPage page)
    {
        var template = page.m_FavoriteLabel != null ? page.m_FavoriteLabel : page.m_ItemNameLabel;
        if (template == null || page.m_FavoriteObject == null)
            return false;

        m_Row = NGUITools.AddChild(page.gameObject);
        m_Row.name = "AdaptiveArsenal_SkinPicker";

        // In the middle of the page, above the favourite checkbox.
        var favoritePosition = page.transform.InverseTransformPoint(page.m_FavoriteObject.transform.position);
        var centreX = page.m_BGNormal != null
            ? page.transform.InverseTransformPoint(page.m_BGNormal.transform.position).x
            : favoritePosition.x;
        m_Row.transform.localPosition = new Vector3(centreX, favoritePosition.y + OffsetAboveFavorite, 0f);

        m_Title = CopyLabel(template, m_Row, 0f);
        m_Title.overflowMethod = UILabel.Overflow.ShrinkContent;
        m_Title.width = TitleWidth;

        m_LeftArrow = CopyLabel(template, m_Row, -ArrowSpacing);
        m_LeftArrow.text = "<";
        m_RightArrow = CopyLabel(template, m_Row, ArrowSpacing);
        m_RightArrow.text = ">";
        m_ArrowColor = template.color;

        MakeButton(m_LeftArrow, -1);
        MakeButton(m_RightArrow, 1);

        m_Row.SetActive(false);
        return true;
    }

    /// <summary>Shows the row for an item, if it's a weapon with skins, or hides it.</summary>
    /// <param name="gearItem">The item the page shows, or <see langword="null"/> for none.</param>
    internal void Show(GearItem? gearItem)
    {
        if (m_Row == null)
            return;

        // The page updates often while it's open, so only look the skins up when the item changes.
        if (gearItem == m_GearItem && gearItem != null)
            return;

        m_GearItem = null;
        m_Options.Clear();

        if (gearItem != null)
        {
            var skins = WeaponSkins.GetSkinsFor(gearItem);
            if (skins.Count > 0)
            {
                m_GearItem = gearItem;
                m_Options.Add(null);
                m_Options.AddRange(skins);

                var skinId = WeaponSkins.GetSkinId(gearItem);
                m_Index = Math.Max(0, m_Options.FindIndex(skin => skin?.Id == skinId));
            }
        }

        m_Row.SetActive(m_GearItem != null);
        UpdateTitle();
    }

    /// <summary>Hides the row, like when the page shows something that isn't an item.</summary>
    internal void Hide() => Show(null);

    /// <summary>Puts the next or previous skin on the weapon, going round past either end.</summary>
    /// <param name="step">1 for the next skin, -1 for the previous one.</param>
    private void Step(int step)
    {
        if (m_GearItem == null || m_Options.Count < 2)
            return;

        m_Index = (m_Index + step + m_Options.Count) % m_Options.Count;
        WeaponSkins.SetSkin(m_GearItem, m_Options[m_Index]?.Id);
        UpdateTitle();
        GameAudioManager.PlayGuiConfirm();

        // The inventory's grid keeps each item's icon until it's refreshed.
        var inventory = InterfaceManager.GetPanel<Panel_Inventory>();
        if (inventory != null && inventory.gameObject.activeInHierarchy)
            inventory.RefreshTable();
    }

    private void UpdateTitle()
    {
        if (m_Title != null)
            m_Title.text = m_Options.Count > 0 ? m_Options[m_Index]?.Name ?? DefaultName : string.Empty;
    }

    /// <summary>Makes an arrow step through the skins when clicked, and light up under the mouse.</summary>
    /// <param name="arrow">The arrow's label.</param>
    /// <param name="step">1 for the next skin, -1 for the previous one.</param>
    private void MakeButton(UILabel arrow, int step)
    {
        NGUITools.AddWidgetCollider(arrow.gameObject);

        var listener = UIEventListener.Get(arrow.gameObject);
        listener.onClick = new Action<GameObject>(_ => Step(step));
        listener.onHover = new Action<GameObject, bool>((_, isOver) => arrow.color = isOver ? s_HoverColor : m_ArrowColor);
    }

    /// <summary>Copies a label into the row, without its anchors or translation, so the copy can be moved and set.</summary>
    /// <param name="template">The label to copy.</param>
    /// <param name="parent">The row.</param>
    /// <param name="x">How far from the middle of the row it goes.</param>
    private static UILabel CopyLabel(UILabel template, GameObject parent, float x)
    {
        var copy = Instantiate(template.gameObject, parent.transform);
        copy.name = "Label";
        copy.SetActive(true);

        // The favourite label's text comes from its translation key, which would replace the text set here.
        foreach (var localize in copy.GetComponentsInChildren<UILocalize>(true))
            Destroy(localize);

        // Anything under the copied label, like a checkmark, isn't wanted.
        for (var i = copy.transform.childCount - 1; i >= 0; i--)
            Destroy(copy.transform.GetChild(i).gameObject);

        var label = copy.GetComponent<UILabel>();
        label.SetAnchor((Transform)null!);
        label.pivot = UIWidget.Pivot.Center;
        label.alignment = NGUIText.Alignment.Center;
        label.transform.localPosition = new Vector3(x, 0f, 0f);
        return label;
    }
}
