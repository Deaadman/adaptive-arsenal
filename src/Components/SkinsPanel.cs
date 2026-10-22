using AdaptiveArsenal.Core;
using AdaptiveArsenal.Utilities;

using Il2CppInterop.Runtime.Attributes;

namespace AdaptiveArsenal.Components;

/// <summary>
/// The skins view of the inventory: a SKINS button among the item details' buttons that swaps the inventory's grid
/// for a grid of the selected weapon's skins, with its own look first. Clicking a skin shows its icon and name in the
/// details, and SAVE puts it on the weapon, or CANCEL leaves the weapon as it was.
/// </summary>
/// <remarks>
/// <para>
/// It goes on the inventory's <see cref="ItemDescriptionPage"/>, added by <see cref="Patches.ItemDescriptionPagePatches"/>
/// the first time the page shows an item. The SKINS, CANCEL and SAVE buttons are copies of the page's DROP button, and
/// the skin slots are copies of the inventory's own slot, placed where its slots are, so they all look like the game's.
/// With SKINS showing, the page's buttons are scaled down to fit four in the row the three took.
/// </para>
/// <para>
/// While it's open, <see cref="Patches.PanelInventoryPatches"/> stops the inventory refilling the details and acting on
/// its keys, and makes Escape, the BACK button and the inventory key cancel. Only as many skins as the inventory has
/// slots are shown, and it's for the mouse only: there's no controller navigation.
/// </para>
/// </remarks>
[RegisterTypeInIl2Cpp(false)]
public class SkinsPanel : MonoBehaviour
{
    /// <summary>What the panel calls a weapon's own look.</summary>
    private const string DefaultName = "Default";

    private const string SkinsText = "SKINS";
    private const string CancelText = "CANCEL";
    private const string SaveText = "SAVE";

    private Panel_Inventory? m_Inventory;
    private ItemDescriptionPage? m_Page;

    /// <summary>The page's USE, ACTIONS and DROP buttons, left to right.</summary>
    private readonly List<GameObject> m_GameButtons = [];

    private GameObject? m_SkinsButton;
    private GameObject? m_CancelButton;
    private GameObject? m_SaveButton;

    /// <summary>Where each button goes, and how big, with three buttons in the row and with four.</summary>
    private readonly List<ButtonLayout> m_Layouts = [];

    /// <summary>Whether the buttons are laid out four to the row, with SKINS among them.</summary>
    private bool m_FourButtons;

    /// <summary>Whether <see cref="Build"/> found everything it needs.</summary>
    private bool m_Built;

    /// <summary>The item the page shows, or <see langword="null"/>.</summary>
    private GearItem? m_ShownGearItem;

    /// <summary>Whether <see cref="m_ShownGearItem"/> has skins.</summary>
    private bool m_ShownHasSkins;

    /// <summary>The weapon being skinned, or <see langword="null"/> when the panel is closed.</summary>
    private GearItem? m_GearItem;

    /// <summary>The skins in the grid: <see langword="null"/> first, for the weapon's own look.</summary>
    private readonly List<SkinDefinition?> m_Options = [];

    /// <summary>Which of <see cref="m_Options"/> is selected.</summary>
    private int m_SelectedIndex;

    private GameObject? m_Grid;
    private readonly List<Slot> m_Slots = [];

    /// <summary>What the panel hid of the inventory, with whether each was showing, to put back.</summary>
    private readonly List<(GameObject GameObject, bool WasActive)> m_Hidden = [];

    /// <summary>Whether the skins view is open.</summary>
    internal bool IsOpen => m_GearItem != null;

    /// <summary>
    /// The skins panel on a description page, adding it if the page is the inventory's. Returns
    /// <see langword="null"/> for other pages, like the clothing panel's, or if it couldn't be built.
    /// </summary>
    /// <param name="page">The page.</param>
    internal static SkinsPanel? GetOrAdd(ItemDescriptionPage page)
    {
        var panel = page.GetComponent<SkinsPanel>();
        if (panel != null)
            return panel.m_Built ? panel : null;

        var inventory = InterfaceManager.GetPanel<Panel_Inventory>();
        if (inventory == null || inventory.m_ItemDescriptionPage != page)
            return null;

        panel = page.gameObject.AddComponent<SkinsPanel>();
        panel.m_Built = panel.Build(inventory, page);
        if (!panel.m_Built)
            Mod.Logger.Log("The inventory's item details have nothing to build the skins panel from.", FlaggedLoggingLevel.Warning);

        return panel.m_Built ? panel : null;
    }

    /// <summary>The skins panel on a description page, if it has one.</summary>
    /// <param name="page">The page.</param>
    internal static SkinsPanel? Find(ItemDescriptionPage? page)
    {
        if (page == null)
            return null;

        var panel = page.GetComponent<SkinsPanel>();
        return panel != null && panel.m_Built ? panel : null;
    }

    /// <summary>The inventory's skins panel, if it has one.</summary>
    /// <param name="inventory">The inventory.</param>
    internal static SkinsPanel? Find(Panel_Inventory inventory) => Find(inventory.m_ItemDescriptionPage);

    /// <summary>Notes the item the page now shows, so SKINS only shows for weapons with skins.</summary>
    /// <param name="gearItem">The item, or <see langword="null"/> for none or a decoration.</param>
    internal void ShowItem(GearItem? gearItem)
    {
        if (gearItem != m_ShownGearItem || gearItem == null)
        {
            m_ShownGearItem = gearItem;
            m_ShownHasSkins = gearItem != null && WeaponSkins.GetSkinsFor(gearItem).Count > 0;
        }

        UpdateButtons();
    }

    /// <summary>
    /// Shows the right buttons: the game's and SKINS normally, CANCEL and SAVE while the panel is open. Called after
    /// the game sets its buttons, which it does often.
    /// </summary>
    internal void UpdateButtons()
    {
        if (IsOpen)
        {
            foreach (var button in m_GameButtons)
                button.SetActive(false);

            SetActive(m_SkinsButton, false);
            SetActive(m_CancelButton, true);
            SetActive(m_SaveButton, true);
            HideInventoryAgain();
            return;
        }

        SetActive(m_CancelButton, false);
        SetActive(m_SaveButton, false);

        var showSkins = m_ShownHasSkins && m_ShownGearItem != null;
        if (showSkins != m_FourButtons)
        {
            m_FourButtons = showSkins;
            foreach (var layout in m_Layouts)
                layout.Apply(showSkins);
        }

        SetActive(m_SkinsButton, showSkins);
    }

    /// <summary>Hides again whatever of the inventory the game showed while the panel is open.</summary>
    internal void HideInventoryAgain()
    {
        if (!IsOpen)
            return;

        foreach (var (gameObject, _) in m_Hidden)
        {
            if (gameObject != null)
                gameObject.SetActive(false);
        }
    }

    /// <summary>Closes the panel without saving, like when Escape is pressed.</summary>
    internal void Cancel()
    {
        if (!IsOpen)
            return;

        GameAudioManager.PlayGUIButtonBack();
        Close(true);
    }

    /// <summary>
    /// Closes the panel without saving or refreshing the inventory, like when the inventory itself closes.
    /// </summary>
    internal void CloseQuietly() => Close(false);

    /// <summary>Builds the buttons from the page's own. Returns <see langword="false"/> if the page has none to copy.</summary>
    /// <param name="inventory">The inventory the page belongs to.</param>
    /// <param name="page">The page.</param>
    private bool Build(Panel_Inventory inventory, ItemDescriptionPage page)
    {
        m_Inventory = inventory;
        m_Page = page;

        GameObject?[] buttons = [page.m_MouseButtonEquip, page.m_MouseButtonExamine, page.m_MouseButtonDrop];
        if (buttons.Any(button => button == null))
            return false;

        var pageTransform = page.transform;
        var bounds = new Dictionary<IntPtr, Bounds>();
        foreach (var button in buttons)
            bounds[button!.Pointer] = NGUIMath.CalculateRelativeWidgetBounds(pageTransform, button.transform, true, false);

        m_GameButtons.AddRange(buttons.Select(button => button!).OrderBy(button => bounds[button.Pointer].center.x));
        var drop = page.m_MouseButtonDrop;

        m_SkinsButton = CopyButton(drop, "AdaptiveArsenal_SkinsButton", SkinsText, Open);
        m_CancelButton = CopyButton(drop, "AdaptiveArsenal_CancelButton", CancelText, Cancel);
        m_SaveButton = CopyButton(drop, "AdaptiveArsenal_SaveButton", SaveText, Save);

        // CANCEL and SAVE take the places of the two right-hand buttons, which are hidden while the panel is open.
        m_CancelButton.transform.position = m_GameButtons[1].transform.position;
        m_SaveButton.transform.position = m_GameButtons[2].transform.position;

        var first = bounds[m_GameButtons[0].Pointer];
        var second = bounds[m_GameButtons[1].Pointer];
        var last = bounds[m_GameButtons[2].Pointer];
        var gap = Math.Max(0f, second.min.x - first.max.x);
        var slotWidth = (last.max.x - first.min.x - 3f * gap) / 4f;

        if (slotWidth <= 0f || first.size.x <= 0f)
        {
            // With nothing to measure, SKINS goes above DROP and the row is left as it is.
            Mod.Logger.Log("Couldn't measure the item details' buttons, so SKINS goes above them.", FlaggedLoggingLevel.Warning);
            m_SkinsButton.transform.position = m_GameButtons[2].transform.position
                + pageTransform.TransformVector(new Vector3(0f, last.size.y + gap, 0f));
            return true;
        }

        // USE, ACTIONS, SKINS, DROP, so DROP stays at the end.
        GameObject[] fourButtons = [m_GameButtons[0], m_GameButtons[1], m_SkinsButton, m_GameButtons[2]];
        for (var i = 0; i < fourButtons.Length; i++)
        {
            var button = fourButtons[i];
            var width = button == m_SkinsButton ? last.size.x : bounds[button.Pointer].size.x;
            var centreX = first.min.x + slotWidth / 2f + i * (slotWidth + gap);
            m_Layouts.Add(new ButtonLayout(button, pageTransform, slotWidth / width, centreX));
        }

        return true;
    }

    /// <summary>Opens the panel for the weapon the page shows.</summary>
    private void Open()
    {
        if (IsOpen || m_ShownGearItem == null || m_Inventory == null)
            return;

        var skins = WeaponSkins.GetSkinsFor(m_ShownGearItem);
        if (skins.Count == 0)
            return;

        m_GearItem = m_ShownGearItem;
        m_Options.Add(null);
        m_Options.AddRange(skins);

        var skinId = WeaponSkins.GetSkinId(m_GearItem);
        m_SelectedIndex = Math.Max(0, m_Options.FindIndex(skin => skin?.Id == skinId));

        if (!BuildGrid(m_SelectedIndex))
        {
            Close(false);
            return;
        }

        Hide(m_Inventory.TableObject);
        Hide(m_Inventory.m_ScrollbarObject);
        Hide(m_Inventory.m_SelectedSpriteObj);
        Hide(m_Inventory.m_SelectedEffect);
        Hide(m_Inventory.m_LabelNoItems != null ? m_Inventory.m_LabelNoItems.gameObject : null);
        Hide(m_Inventory.m_GearItemCoverflow != null && m_Inventory.m_GearItemCoverflow.m_TextureWithDamage != null
            ? m_Inventory.m_GearItemCoverflow.m_TextureWithDamage.gameObject
            : null);

        GameAudioManager.PlayGUIButtonClick();
        Select(m_SelectedIndex);
        UpdateButtons();
    }

    /// <summary>Puts the selected skin on the weapon and closes the panel.</summary>
    private void Save()
    {
        if (!IsOpen)
            return;

        WeaponSkins.SetSkin(m_GearItem!, m_Options[m_SelectedIndex]?.Id);
        GameAudioManager.PlayGuiConfirm();
        Close(true);
    }

    /// <summary>Closes the panel and puts the inventory back.</summary>
    /// <param name="refresh">Whether to refresh the inventory's grid and details, which needs it to be open.</param>
    private void Close(bool refresh)
    {
        m_GearItem = null;
        m_Options.Clear();
        m_Slots.Clear();

        if (m_Grid != null)
            Destroy(m_Grid);
        m_Grid = null;

        foreach (var (gameObject, wasActive) in m_Hidden)
        {
            if (gameObject != null)
                gameObject.SetActive(wasActive);
        }

        m_Hidden.Clear();

        // Refreshing shows the selected item in the details again, with its buttons, and its icon in the grid.
        if (refresh && m_Inventory != null)
            m_Inventory.RefreshTable();

        UpdateButtons();
    }

    /// <summary>Selects a skin: highlights its slot and shows it in the details.</summary>
    /// <param name="index">Its index in <see cref="m_Options"/>.</param>
    private void Select(int index)
    {
        if (!IsOpen || index < 0 || index >= m_Slots.Count)
            return;

        m_SelectedIndex = index;
        for (var i = 0; i < m_Slots.Count; i++)
            SetActive(m_Slots[i].Selected, i == index);

        var skin = m_Options[index];
        var coverflow = m_Inventory != null ? m_Inventory.m_GearItemCoverflow : null;
        if (coverflow != null && coverflow.m_Texture != null)
            coverflow.m_Texture.mainTexture = IconFor(skin);

        if (m_Page != null && m_Page.m_ItemNameLabel != null)
            m_Page.m_ItemNameLabel.text = (skin?.Name ?? DefaultName).ToUpperInvariant();

        if (m_Page != null && m_Page.m_ItemDescLabel != null)
        {
            m_Page.m_ItemDescLabel.text = skin == null
                ? $"The {m_GearItem!.DisplayName}'s own look."
                : $"A skin for the {m_GearItem!.DisplayName}.";
        }
    }

    /// <summary>
    /// Builds the grid of skins where the inventory's grid is, from copies of its slot. Returns
    /// <see langword="false"/> if the inventory has no slots to copy.
    /// </summary>
    /// <param name="wornIndex">The skin the weapon wears, which is marked like an equipped item.</param>
    private bool BuildGrid(int wornIndex)
    {
        var inventory = m_Inventory!;
        var table = inventory.TableObject;
        var prefab = inventory.InventoryItemPrefab;
        var tableItems = inventory.m_TableItems;
        if (table == null || prefab == null || tableItems == null || tableItems.Length == 0)
        {
            Mod.Logger.Log("The inventory has no grid to copy, so the skins panel can't open.", FlaggedLoggingLevel.Warning);
            return false;
        }

        // Laid over the inventory's own grid, so the copied slots go where its slots are.
        m_Grid = new GameObject("AdaptiveArsenal_SkinsGrid") { layer = table.layer };
        m_Grid.transform.SetParent(table.transform.parent, false);
        m_Grid.transform.localPosition = table.transform.localPosition;
        m_Grid.transform.localRotation = table.transform.localRotation;
        m_Grid.transform.localScale = table.transform.localScale;

        var count = Math.Min(m_Options.Count, tableItems.Length);
        if (count < m_Options.Count)
            Mod.Logger.Log($"{m_GearItem!.name} has {m_Options.Count - 1} skins, but only the first {count - 1} fit in the inventory.", FlaggedLoggingLevel.Warning);

        for (var i = 0; i < count; i++)
        {
            var tableItem = tableItems[i];
            if (tableItem == null)
                break;

            m_Slots.Add(BuildSlot(prefab, tableItem.transform, i, i == wornIndex));
        }

        return m_Slots.Count > 0;
    }

    /// <summary>Builds one skin's slot from a copy of the inventory's slot, with only its icon showing.</summary>
    /// <param name="prefab">The inventory's slot.</param>
    /// <param name="place">The inventory slot whose place it takes.</param>
    /// <param name="index">The skin's index in <see cref="m_Options"/>.</param>
    /// <param name="isWorn">Whether the weapon wears the skin.</param>
    [HideFromIl2Cpp]
    private Slot BuildSlot(GameObject prefab, Transform place, int index, bool isWorn)
    {
        var copy = CopyInactive(prefab, m_Grid!.transform, out var item);
        copy.name = $"Skin{index}";
        copy.transform.localPosition = place.localPosition;
        copy.transform.localRotation = place.localRotation;
        copy.transform.localScale = place.localScale;

        Slot slot = new(item != null ? item.m_SelectedSprite : null);
        if (item != null)
        {
            if (item.m_GearSprite != null)
                item.m_GearSprite.mainTexture = IconFor(m_Options[index]);

            // Everything but the icon is about an item's condition, weight and so on.
            SetActive(item.m_EquippedSprite, isWorn);
            SetActive(item.m_RuinedSprite, false);
            SetActive(item.m_LiquidLabel, false);
            SetActive(item.m_StackLabel, false);
            SetActive(item.m_ConditionLabel, false);
            SetActive(item.m_WeightLabel, false);
            SetActive(item.m_NameLabel, false);
            SetActive(item.m_ConditionSprite, false);
            SetActive(item.m_FavoritedSprite, false);
            SetActive(item.m_UnitLabel, false);
            SetActive(item.m_UnitSprite, false);
            SetActive(item.m_NotificationFlag, false);
            SetActive(item.m_HoverEffect, false);
            SetActive(item.m_SelectedSprite, false);

            var hoverEffect = item.m_HoverEffect;
            var clickTarget = item.m_Button != null ? item.m_Button.gameObject : copy;
            var listener = UIEventListener.Get(clickTarget);
            listener.onClick = new Action<GameObject>(_ =>
            {
                if (index != m_SelectedIndex)
                    GameAudioManager.PlayGUIButtonClick();
                Select(index);
            });
            listener.onDoubleClick = new Action<GameObject>(_ =>
            {
                Select(index);
                Save();
            });
            listener.onHover = new Action<GameObject, bool>((_, isOver) => SetActive(hoverEffect, isOver));

            // The slot's own script would look for an item to show, so only its widgets are kept.
            DestroyImmediate(item);
        }

        copy.SetActive(true);
        return slot;
    }

    /// <summary>A skin's icon, or the weapon's own for <see langword="null"/>.</summary>
    /// <param name="skin">The skin.</param>
    [HideFromIl2Cpp]
    private Texture? IconFor(SkinDefinition? skin) =>
        skin == null ? WeaponSkins.GetDefaultIcon(m_GearItem!) : WeaponSkins.GetIcon(skin);

    /// <summary>Hides part of the inventory while the panel is open, remembering whether it showed.</summary>
    /// <param name="gameObject">The part, or <see langword="null"/> to do nothing.</param>
    private void Hide(GameObject? gameObject)
    {
        if (gameObject == null)
            return;

        m_Hidden.Add((gameObject, gameObject.activeSelf));
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Copies one of the page's buttons with a new label and click, without what made the original do its job.
    /// </summary>
    /// <param name="template">The button to copy.</param>
    /// <param name="name">The copy's name.</param>
    /// <param name="text">Its label.</param>
    /// <param name="onClick">What it does when clicked.</param>
    [HideFromIl2Cpp]
    private static GameObject CopyButton(GameObject template, string name, string text, Action onClick)
    {
        var copy = CopyInactive(template, template.transform.parent, out _);
        copy.name = name;

        // The label's text would otherwise come from its translation, or from the button that spawned it.
        foreach (var spawner in copy.GetComponentsInChildren<GenericButtonMouseSpawner>(true))
            DestroyImmediate(spawner);

        foreach (var localize in copy.GetComponentsInChildren<UILocalize>(true))
            DestroyImmediate(localize);

        var label = copy.GetComponentInChildren<UILabel>(true);
        if (label != null)
            label.text = text;

        foreach (var button in copy.GetComponentsInChildren<UIButton>(true))
            button.onClick.Clear();

        var colliders = copy.GetComponentsInChildren<Collider>(true);
        if (colliders.Length == 0)
        {
            NGUITools.AddWidgetCollider(copy);
            colliders = copy.GetComponentsInChildren<Collider>(true);
        }

        foreach (var collider in colliders)
            UIEventListener.Get(collider.gameObject).onClick = new Action<GameObject>(_ => onClick());

        return copy;
    }

    /// <summary>
    /// Copies an object inactive, so none of its scripts start, with any <see cref="InventoryGridItem"/> on it
    /// returned to read before it's removed.
    /// </summary>
    /// <param name="template">The object to copy.</param>
    /// <param name="parent">Where the copy goes.</param>
    /// <param name="item">The copy's <see cref="InventoryGridItem"/>, if it has one.</param>
    [HideFromIl2Cpp]
    private static GameObject CopyInactive(GameObject template, Transform parent, out InventoryGridItem? item)
    {
        var holder = new GameObject("AdaptiveArsenal_Holder");
        holder.SetActive(false);
        holder.transform.SetParent(parent, false);

        var copy = Instantiate(template, holder.transform);
        item = copy.GetComponent<InventoryGridItem>();

        // The slot's button would call the removed script.
        if (item != null && item.m_Button != null)
            item.m_Button.onClick.Clear();

        copy.SetActive(false);
        copy.transform.SetParent(parent, false);
        Destroy(holder);
        return copy;
    }

    private static void SetActive(Component? component, bool active)
    {
        if (component != null)
            component.gameObject.SetActive(active);
    }

    private static void SetActive(GameObject? gameObject, bool active)
    {
        if (gameObject != null)
            gameObject.SetActive(active);
    }

    /// <summary>A skin's slot in the grid.</summary>
    /// <param name="Selected">The highlight shown when it's selected.</param>
    private sealed record Slot(UISprite? Selected);

    /// <summary>Where one of the page's buttons goes, and how big, with three buttons in the row and with four.</summary>
    private sealed class ButtonLayout
    {
        private readonly GameObject m_Button;
        private readonly Transform m_Page;
        private readonly Vector3 m_Position;
        private readonly Vector3 m_Scale;
        private readonly float m_FourScale;
        private readonly float m_FourCentreX;

        /// <summary>Remembers where a button is now, which is where it goes with three to the row.</summary>
        /// <param name="button">The button.</param>
        /// <param name="page">The page, whose space <paramref name="fourCentreX"/> is in.</param>
        /// <param name="fourScale">How much smaller it is with four to the row.</param>
        /// <param name="fourCentreX">Where its middle goes with four to the row.</param>
        internal ButtonLayout(GameObject button, Transform page, float fourScale, float fourCentreX)
        {
            m_Button = button;
            m_Page = page;
            m_Position = button.transform.localPosition;
            m_Scale = button.transform.localScale;
            m_FourScale = fourScale;
            m_FourCentreX = fourCentreX;
        }

        /// <summary>Lays the button out for three or four to the row.</summary>
        /// <param name="four">Whether there are four.</param>
        internal void Apply(bool four)
        {
            if (m_Button == null)
                return;

            var transform = m_Button.transform;
            transform.localPosition = m_Position;
            transform.localScale = four ? m_Scale * m_FourScale : m_Scale;
            if (!four)
                return;

            // Scaling moves the middle unless the button's origin is in it, so it's measured again.
            var bounds = NGUIMath.CalculateRelativeWidgetBounds(m_Page, transform, true, false);
            transform.position += m_Page.TransformVector(new Vector3(m_FourCentreX - bounds.center.x, 0f, 0f));
        }
    }
}
