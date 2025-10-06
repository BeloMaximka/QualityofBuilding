using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace ImmersiveBuilding.Source.Render;

public class SkillModeBuildingHud : HudElement
{
    private readonly DummyInventory dummyInventory;

    public ItemStack Item
    {
        get => dummyInventory.Slots[0].Itemstack;
        set { dummyInventory.Slots[0].Itemstack = value; }
    }

    public SkillModeBuildingHud(ICoreClientAPI capi)
        : base(capi)
    {
        dummyInventory = new(capi);

        ElementBounds slotBounds = ElementStdBounds.Slot();
        ElementBounds dialogBounds = ElementStdBounds
            .AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterTop)
            .WithFixedOffset(220, GuiStyle.DialogToScreenPadding);
        SingleComposer = capi
            .Gui.CreateCompo("ImmersiveBuildingSkillModeTooltip", dialogBounds)
            .AddGrayBG(slotBounds)
            .AddPassiveItemSlot(slotBounds, dummyInventory, dummyInventory[0], false)
            .Compose(false);
    }
}
