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
    private readonly float gap = 8f;
    private readonly float maxItemSize;
    private readonly IClientNetworkChannel? buildingModeChannel;

    private LoadedTexture wheelTexture;
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
        surface = new(Format.Argb32, capi.Render.FrameWidth, capi.Render.FrameHeight);
        context = new(surface);

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

        GenerateGearTexture();

        for (int index = 0; index < BuildingOptions.Count; ++index)
        {
            double a0 = startAngle + index * angleStep;
            double a1 = a0 + angleStep;

            // create path for ring segment:
            // 1) move to outer arc start
            double x0 = centerX + Math.Cos(a0) * outerRadius;
            double y0 = centerY + Math.Sin(a0) * outerRadius;
            context.MoveTo(x0, y0);

            // 2) outer arc forward from a0 to a1
            context.Arc(centerX, centerY, outerRadius, a0, a1);

            // 3) line to inner arc at a1
            double x1In = centerX + Math.Cos(a1) * innerRadius;
            double y1In = centerY + Math.Sin(a1) * innerRadius;
            context.LineTo(x1In, y1In);

            // 4) inner arc backwards from a1 to a0
            context.ArcNegative(centerX, centerY, innerRadius, a1, a0);

            context.ClosePath();
            bool hovered = (index == selectedMode);
            double[] color = hovered ? GuiStyle.DialogHighlightColor : GuiStyle.DialogLightBgColor;
            context.SetSourceRGBA(color[0], color[1], color[2], color[3]);
            context.FillPreserve();

            // stroke outline
            context.SetSourceRGBA(0, 0, 0, 0.45);
            context.Stroke();

            // separator line
            context.Save();
            context.LineTo(centerX + Math.Cos(a0) * outerRadius, centerY + Math.Sin(a0) * outerRadius);
            context.Stroke();
            context.Restore();

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
        }

        // central circle
        context.Save();
        context.Arc(centerX, centerY, innerRadius - gap, 0, Math.PI * 2);
        context.Fill();
        context.Restore();

        surface.Flush();
        capi.Gui.LoadOrUpdateCairoTexture(surface, false, ref wheelTexture);
        capi.Render.Render2DLoadedTexture(wheelTexture, 0, 0);

        base.OnRenderGUI(deltaTime);
    }

    public override void Dispose()
    {
        base.Dispose();
        GC.SuppressFinalize(this);
        wheelTexture?.Dispose();
        context.Dispose();
        surface.Dispose();
    }

    private void GenerateGearTexture()
    {
        context.Clear();
        context.NewPath();

        int toothCount = Math.Max(8, BuildingOptions.Count);

        float centerX = capi.Render.FrameWidth * 0.5f;
        float centerY = capi.Render.FrameHeight * 0.5f;

        float maxHalf = Math.Min(capi.Render.FrameHeight, capi.Render.FrameWidth) * 0.5f;
        float radius = maxHalf * radiusFactor;

        double angleStep = 2.0 * Math.PI / toothCount;

        double innerRadius = radius + maxItemSize / 2;

        double toothWidth = innerRadius * (float)angleStep / 1.5;
        double toothHeight = Math.Min(toothWidth / 2, maxItemSize);
        double toothStartAngle = -Math.PI;

        for (int i = 0; i < toothCount; i++)
        {
            double angle = toothStartAngle + i * 2 * Math.PI / toothCount;

            double halfBaseWidth = toothWidth / 2;

            // make the tip narrower
            // TODO: calculate tip width so it perfectly matches the tooth gaps
            double halfTipWidth = halfBaseWidth * 0.8;

            double x0 = -halfBaseWidth;
            double y0 = innerRadius;
            double x1 = halfBaseWidth;
            double y1 = innerRadius;

            double x2 = halfTipWidth;
            double y2 = innerRadius + toothHeight;
            double x3 = -halfTipWidth;
            double y3 = innerRadius + toothHeight;

            double cos = Math.Cos(angle);
            double sin = Math.Sin(angle);

            context.LineTo(centerX + x1 * cos - y1 * sin, centerY + x1 * sin + y1 * cos);
            context.LineTo(centerX + x2 * cos - y2 * sin, centerY + x2 * sin + y2 * cos);
            context.LineTo(centerX + x3 * cos - y3 * sin, centerY + x3 * sin + y3 * cos);
            context.LineTo(centerX + x0 * cos - y0 * sin, centerY + x0 * sin + y0 * cos);
        }
        context.MoveTo(centerX, centerY);
        context.FillRule = FillRule.EvenOdd;
        context.Arc(centerX, centerY, innerRadius - maxItemSize / 2 + gap, 0, 2 * Math.PI);
        double[] color = GuiStyle.DialogLightBgColor;
        context.SetSourceRGBA(color[0], color[1], color[2], color[3]);
        context.Fill();
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
        }
    }
}
