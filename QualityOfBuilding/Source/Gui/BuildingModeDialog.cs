using Cairo;
using QualityOfBuilding.Source.CollectibleBehaviors.BuildingModes;
using QualityOfBuilding.Source.Network;
using QualityOfBuilding.Source.Utils;
using QualityOfBuilding.Source.Utils.Inventory;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace QualityOfBuilding.Source.Gui;

// We don't need more than one building dialog
public static class BuildingModeDialogSingleton
{
    private static BuildingModeDialog? dialog;

    public static bool TryOpen(ICoreClientAPI capi, ItemStack heldItem, List<BuildingMode> buildingOptions)
    {
        if (dialog is null)
        {
            dialog = new(capi, heldItem, buildingOptions);
            return dialog.TryOpen();
        }

        dialog.BuildingOptions = buildingOptions;
        dialog.HeldItem = heldItem;
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
        dialog?.Dispose();
        dialog = null;
    }
}

// TODO: Optimize
public class BuildingModeDialog : GuiDialog
{
    private int prevSelectedMode;
    private int selectedMode;

    private readonly float radiusFactor = 0.66f;
    private readonly int gap = 8;
    private readonly float maxItemSize;
    private readonly IClientNetworkChannel? buildingModeChannel;

    private LoadedTexture wheelTexture;
    private readonly GearRingElement gearRing;
    private readonly ImageSurface surface;
    private readonly Context context;

    public ItemStack HeldItem { get; set; }
    public IReadOnlyList<BuildingMode> BuildingOptions { get; set; }

    public const string ToggleCombinationCode = "buildingmodedialog";
    public override string ToggleKeyCombinationCode => ToggleCombinationCode;

    public BuildingModeDialog(ICoreClientAPI capi, ItemStack heldItem, List<BuildingMode> buildingOptions)
        : base(capi)
    {
        maxItemSize = (float)GuiElementPassiveItemSlot.unscaledSlotSize + (float)GuiElementItemSlotGridBase.unscaledSlotPadding;

        HeldItem = heldItem;
        BuildingOptions = buildingOptions;

        buildingModeChannel = capi.Network.GetChannel(SetBuildingModePacket.Channel);

        wheelTexture = new(capi);

        //TODO: fix surface size when resizing image
        // TODO: crop textures
        surface = new(Format.Argb32, capi.Render.FrameWidth, capi.Render.FrameHeight);
        context = new(surface);

        float maxHalf = Math.Min(capi.Render.FrameHeight, capi.Render.FrameWidth) * 0.5f;
        gearRing = new(capi, buildingOptions.Count, maxHalf * radiusFactor + gap * 2);
        ComposeDialog();
    }

    public override void OnGuiClosed()
    {
        if (selectedMode == prevSelectedMode)
        {
            return;
        }

        string toolModeCode = BuildingOptions[selectedMode].Code;
        buildingModeChannel?.SendPacket(new SetBuildingModePacket() { ToolModeCode = toolModeCode });
        HeldItem.SetBuildingMode(toolModeCode);
    }

    public override void OnGuiOpened()
    {
        selectedMode = HeldItem.GetBuildingMode(BuildingOptions);
        prevSelectedMode = selectedMode;

        gearRing.SetOptionsCount(BuildingOptions.Count);
    }

    public override void OnMouseMove(MouseEvent args)
    {
        base.OnMouseMove(args);

        if (BuildingOptions.Count == 0)
        {
            return;
        }

        // mouse vector from center
        double dx = args.X - capi.Render.FrameWidth * 0.5f;
        double dy = args.Y - capi.Render.FrameHeight * 0.5f;

        // skip if mouse near center
        if (Math.Sqrt(dx * dx + dy * dy) < 32)
        {
            return;
        }

        double startAngle = -Math.PI / 2.0;
        double angleStep = 2.0 * Math.PI / BuildingOptions.Count;
        startAngle -= angleStep / 2.0;
        // convert mouseAngle (atan2 -PI..PI) to a positive offset relative to startAngle in range [0, 2PI)
        double rel = Math.Atan2(dy, dx) - startAngle;
        // normalize to [0, 2PI)
        rel = rel % (2.0 * Math.PI);
        if (rel < 0)
            rel += 2.0 * Math.PI;

        OnSlotOver((int)(rel / angleStep));
    }

    public override void OnMouseDown(MouseEvent args)
    {
        args.Handled = true;
        TryClose();
    }

    public override void OnMouseWheel(MouseWheelEventArgs args)
    {
        int newIndex = selectedMode + args.delta;
        if (newIndex < 0)
        {
            newIndex += BuildingOptions.Count;
        }
        else if (newIndex >= BuildingOptions.Count)
        {
            newIndex -= BuildingOptions.Count;
        }
        OnSlotOver(newIndex);
        args.SetHandled();
    }

    public override void OnRenderGUI(float deltaTime)
    {
        if (BuildingOptions.Count == 0)
        {
            return;
        }

        float centerX = capi.Render.FrameWidth * 0.5f;
        float centerY = capi.Render.FrameHeight * 0.5f;

        float maxHalf = Math.Min(capi.Render.FrameHeight, capi.Render.FrameWidth) * 0.5f;
        float radius = maxHalf * radiusFactor;

        double startAngle = -Math.PI / 2.0;
        double angleOffset = startAngle;
        double angleStep = 2.0 * Math.PI / BuildingOptions.Count;
        startAngle -= angleStep / 2;

        float size = Math.Min(maxItemSize, radius * (float)angleStep / 3);

        double outerRadius = radius;
        double innerRadius = outerRadius - maxItemSize * 2.5;

        context.Antialias = Antialias.Subpixel;
        context.LineWidth = 1;
        context.Clear();
        context.PushGroup();

        double[] color = GuiStyle.DialogLightBgColor;

        context.SetSourceRGBA(color[0], color[1], color[2], 1);
        context.SetSourceRGBA(color[0], color[1], color[2], 1);
        context.Arc(centerX, centerY, radius + gap, 0, Math.PI * 2);
        context.Fill();

        double halfGapSize = gap / 2.0;
        double angleGapOuter = halfGapSize / outerRadius;
        double angleGapInner = halfGapSize / innerRadius;

        for (int index = 0; index < BuildingOptions.Count; ++index)
        {
            double segmentRagius = (outerRadius - innerRadius) / 2;
            float itemCenterX = centerX + (float)(Math.Cos(angleOffset + index * angleStep) * (radius - segmentRagius));
            float itemCenterY = centerY + (float)(Math.Sin(angleOffset + index * angleStep) * (radius - segmentRagius));

            capi.Render.RenderItemstackToGui(
                BuildingOptions[index].RenderSlot,
                itemCenterX,
                itemCenterY,
                100,
                size,
                ColorUtil.WhiteArgb,
                true,
                showStackSize: false
            );

            double rawA0 = startAngle + index * angleStep;
            double rawA1 = rawA0 + angleStep;

            double outerStart = rawA0 + angleGapOuter;
            double outerEnd = rawA1 - angleGapOuter;

            double innerStart = rawA0 + angleGapInner;
            double innerEnd = rawA1 - angleGapInner;

            context.MoveTo(centerX + Math.Cos(outerStart) * outerRadius, centerY + Math.Sin(outerStart) * outerRadius);

            context.Arc(centerX, centerY, outerRadius, outerStart, outerEnd);

            context.LineTo(centerX + Math.Cos(innerEnd) * innerRadius, centerY + Math.Sin(innerEnd) * innerRadius);

            context.ArcNegative(centerX, centerY, innerRadius, innerEnd, innerStart);

            context.ClosePath();

            bool hovered = (index == selectedMode);
            color = hovered ? GuiStyle.DialogHighlightColor : [0, 0, 0, 1];
            context.SetSourceRGBA(color[0], color[1], color[2], 1);
            context.FillPreserve();
            color = GuiStyle.DialogLightBgColor;
            context.SetSourceRGBA(color[0], color[1], color[2], 1);

            // stroke outline
            context.Stroke();
        }

        // central circle
        context.SetSourceRGBA(0, 0, 0, 1);
        context.Arc(centerX, centerY, innerRadius - gap, 0, Math.PI * 2);
        context.Fill();

        color = GuiStyle.DialogLightBgColor;
        context.SetSourceRGBA(color[0], color[1], color[2], 1);
        context.Arc(centerX, centerY, innerRadius - gap * 2, 0, Math.PI * 2);
        context.Fill();

        context.PopGroupToSource();
        context.PaintWithAlpha(0.75);
        surface.Flush();
        capi.Gui.LoadOrUpdateCairoTexture(surface, false, ref wheelTexture);
        capi.Render.Render2DLoadedTexture(wheelTexture, 0, 0);

        gearRing.OnRender(deltaTime);
        base.OnRenderGUI(deltaTime);
    }

    public override void Dispose()
    {
        base.Dispose();
        GC.SuppressFinalize(this);
        wheelTexture.Dispose();
        context.Dispose();
        surface.Dispose();
    }

    private void ComposeDialog()
    {
        double innerWidth = 400;

        ElementBounds nameBounds = ElementBounds.Fixed(0, 0, innerWidth, 55);
        ElementBounds descBounds = nameBounds.BelowCopy().WithFixedSize(innerWidth, 150);
        ElementBounds bgBounds = ElementBounds.Fill.WithSizing(ElementSizing.FitToChildren);

        CairoFont nameFont = new()
        {
            Color = GuiStyle.DialogDefaultTextColor,
            Fontname = GuiStyle.StandardFontName,
            UnscaledFontsize = GuiStyle.SmallishFontSize,
            Orientation = EnumTextOrientation.Center,
        };

        CairoFont descFont = new()
        {
            Color = GuiStyle.DialogDefaultTextColor,
            Fontname = GuiStyle.StandardFontName,
            UnscaledFontsize = GuiStyle.DetailFontSize,
            Orientation = EnumTextOrientation.Center,
        };

        SingleComposer = capi
            .Gui.CreateCompo("buildingModeSelect", ElementStdBounds.AutosizedMainDialog)
            .BeginChildElements(bgBounds)
            .AddDynamicText("", nameFont, nameBounds, "name")
            .AddDynamicText("", descFont, descBounds, "ingredient")
            .EndChildElements()
            .Compose();

        gearRing.Compose();
    }

    private void OnSlotOver(int slotIndex)
    {
        if (slotIndex >= BuildingOptions.Count)
        {
            return;
        }

        if (slotIndex != selectedMode)
        {
            selectedMode = slotIndex;
            SingleComposer.GetDynamicText("name").SetNewText(BuildingOptions[slotIndex].Name);
            SingleComposer.GetDynamicText("ingredient").SetNewText(BuildingOptions[slotIndex].Ingredients.GetMaterialsString("\n"));
            gearRing.SetSelectedOption(slotIndex);
        }
    }
}
