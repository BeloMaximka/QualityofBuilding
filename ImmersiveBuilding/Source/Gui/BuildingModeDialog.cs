using ImmersiveBuilding.Source.CollectibleBehaviors.BuildingModes;
using ImmersiveBuilding.Source.Common;
using ImmersiveBuilding.Source.Extensions.Inventory;
using ImmersiveBuilding.Source.Network;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace ImmersiveBuilding.Source.Gui;

// We don't need more than one building dialog
public static class BuildingModeDialogSingleton
{
    private static BuildingModeDialog? dialog;

    public static bool TryOpen(ICoreClientAPI capi, ItemStack heldItem, List<SkillItem> buildingOptions)
    {
        if (dialog is null)
        {
            dialog = new(capi, heldItem, buildingOptions);
            return dialog.TryOpen();
        }

        dialog.BuildingOptions = buildingOptions;
        dialog.HeldItem = heldItem;
        dialog.ComposeDialog();
        return dialog.TryOpen();
    }

    public static bool IsOpened()
    {
        if (dialog is null)
        {
            return false;
        }
        return dialog.IsOpened();
    }

    public static bool TryClose()
    {
        if (dialog is null || !dialog.IsOpened())
        {
            return true;
        }
        return dialog.TryClose();
    }

    public static void FreeRam()
    {
        dialog = null;
    }
}

public class BuildingModeDialog : GuiDialog
{
    private int prevSlotOver = -1;

    private readonly IClientNetworkChannel? buildingModeChannel;

    public ItemStack HeldItem { get; set; }

    public List<SkillItem> BuildingOptions { get; set; }

    public const string ToggleCombinationCode = "buildingmodedialog";

    public override string ToggleKeyCombinationCode => ToggleCombinationCode;

    public BuildingModeDialog(ICoreClientAPI capi, ItemStack heldItem, List<SkillItem> buildingOptions)
        : base(capi)
    {
        HeldItem = heldItem;
        BuildingOptions = buildingOptions;

        buildingModeChannel = capi.Network.GetChannel(SharedConstants.BuildingModeNetworkChannel);
        ComposeDialog();
    }

    public void ComposeDialog()
    {
        int cnt = Math.Max(1, BuildingOptions.Count);

        int cols = Math.Min(cnt, 8);

        int rows = (int)Math.Ceiling(cnt / (float)cols);

        double size = GuiElementPassiveItemSlot.unscaledSlotSize + GuiElementItemSlotGridBase.unscaledSlotPadding;
        double innerWidth = Math.Max(400, cols * size);
        ElementBounds skillGridBounds = ElementBounds.Fixed(0, 30, innerWidth, rows * size);

        ElementBounds nameBounds = ElementBounds.Fixed(0, rows * size + 50, innerWidth, 55);
        ElementBounds descBounds = nameBounds.BelowCopy();

        ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
        bgBounds.BothSizing = ElementSizing.FitToChildren;

        SingleComposer = capi
            .Gui.CreateCompo("buildingModeSelect", ElementStdBounds.AutosizedMainDialog)
            .AddShadedDialogBG(bgBounds, true)
            .BeginChildElements(bgBounds)
            .AddSkillItemGrid(BuildingOptions, cols, rows, OnSlotClick, skillGridBounds, "skillitemgrid")
            .AddDynamicText("", CairoFont.WhiteSmallishText(), nameBounds, "name")
            .AddDynamicText("", CairoFont.WhiteDetailText(), descBounds, "ingredient")
            .EndChildElements()
            .Compose();

        SingleComposer.GetSkillItemGrid("skillitemgrid").OnSlotOver = OnSlotOver;
    }

    private void OnSlotOver(int slotIndex)
    {
        if (slotIndex >= BuildingOptions.Count)
        {
            return;
        }

        if (slotIndex != prevSlotOver)
        {
            prevSlotOver = slotIndex;
            SingleComposer.GetDynamicText("name").SetNewText(BuildingOptions[slotIndex].Name);
            if (BuildingOptions[slotIndex].Data is BuildingModeContext context)
            {
                SingleComposer.GetDynamicText("ingredient").SetNewText(context.Ingredients.GetMaterialsString());
            }
        }
    }

    private void OnSlotClick(int slotIndex)
    {
        buildingModeChannel?.SendPacket(new SetBuildingModeMessage() { Mode = slotIndex });
        HeldItem.Attributes.SetInt(SharedConstants.BuildingModeAttributeName, slotIndex);

        TryClose();
    }
}
