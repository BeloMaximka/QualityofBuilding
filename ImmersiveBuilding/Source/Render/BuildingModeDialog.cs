using ImmersiveBuilding.Source.CollectibleBehaviors.BuildingModes;
using ImmersiveBuilding.Source.Common;
using ImmersiveBuilding.Source.Network;
using ImmersiveBuilding.Source.Systems;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace ImmersiveBuilding.Source.Render;

public class BuildingModeDialog : GuiDialog
{
    private int prevSlotOver = -1;
    private readonly IClientNetworkChannel? buildingModeChannel;

    private readonly List<SkillItem> skillItems;
    private readonly Action<int> onSelectedRecipe;

    public const string ToggleCombinationCode = "buildingmodedialog";
    public override string ToggleKeyCombinationCode => ToggleCombinationCode;

    public BuildingModeDialog(List<SkillItem> skillItems, Action<int> onSelectedRecipe, ICoreClientAPI capi)
        : base(capi)
    {
        this.skillItems = skillItems;
        this.onSelectedRecipe = onSelectedRecipe;
        buildingModeChannel = capi.Network.GetChannel(SharedConstants.BuildingModeNetworkChannel);
        SetupDialog();
    }

    void SetupDialog()
    {
        int cnt = Math.Max(1, skillItems.Count);

        int cols = Math.Min(cnt, 8);

        int rows = (int)Math.Ceiling(cnt / (float)cols);

        double size = GuiElementPassiveItemSlot.unscaledSlotSize + GuiElementItemSlotGrid.unscaledSlotPadding;
        double innerWidth = Math.Max(300, cols * size);
        ElementBounds skillGridBounds = ElementBounds.Fixed(0, 30, innerWidth, rows * size);

        ElementBounds nameBounds = ElementBounds.Fixed(0, rows * size + 50, innerWidth, 33);
        ElementBounds descBounds = nameBounds.BelowCopy(0, 10, 0, 0);

        ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
        bgBounds.BothSizing = ElementSizing.FitToChildren;

        SingleComposer = capi
            .Gui.CreateCompo("buildingModeSelect", ElementStdBounds.AutosizedMainDialog)
            .AddShadedDialogBG(bgBounds, true)
            .AddDialogTitleBar(Lang.Get("Select Recipe"), OnTitleBarClose)
            .BeginChildElements(bgBounds)
            .AddSkillItemGrid(skillItems, cols, rows, OnSlotClick, skillGridBounds, "skillitemgrid")
            .AddDynamicText("", CairoFont.WhiteSmallishText(), nameBounds, "name")
            .AddDynamicText("", CairoFont.WhiteDetailText(), descBounds, "desc")
            .AddDynamicText("", CairoFont.WhiteDetailText(), descBounds.BelowCopy(0, 20, 0, 0), "ingredient")
            .EndChildElements()
            .Compose();

        SingleComposer.GetSkillItemGrid("skillitemgrid").OnSlotOver = OnSlotOver;
    }

    private void OnSlotOver(int num)
    {
        if (num >= skillItems.Count)
            return;

        if (num != prevSlotOver)
        {
            prevSlotOver = num;
            SingleComposer.GetDynamicText("name").SetNewText(skillItems[num].Name);
            SingleComposer.GetDynamicText("desc").SetNewText(skillItems[num].Description);
            string requires = "";
            if (skillItems[num].Data is ItemStack[] ingredients)
                requires = Lang.Get("recipeselector-requiredcount", ingredients[0].StackSize, ingredients[0].GetName().ToLower());
            SingleComposer.GetDynamicText("ingredient").SetNewText(requires);
        }
    }

    private void OnSlotClick(int num)
    {
        BuildingModeContext? context = skillItems[num].Data as BuildingModeContext;
        if (context is not null)
        {
            ImmersiveBuildingRenderingSystem.SkillModeHud.Item = context.Output;
        }
        buildingModeChannel?.SendPacket(new SetBuildingModeMessage() { Mode = num });

        onSelectedRecipe(num);
        TryClose();
    }

    private void OnTitleBarClose()
    {
        TryClose();
    }
}
