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
using Vintagestory.Client.NoObf;

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

public struct SegmentInfo
{
    public double X { get; set; }
    public double Y { get; set; }
    public float Rotation { get; set; }
}

public class BuildingModeDialog : GuiDialog
{
    private int prevOptionsCount;
    private int prevSelectedMode;
    private int selectedMode;
    float itemSize;

    private readonly List<SegmentInfo> segmentCoords;
    private readonly float maxItemSize;
    private readonly IClientNetworkChannel? buildingModeChannel;

    private readonly SurfacePattern backgroundPattern;
    private LoadedTexture wheelTexture;
    private LoadedTexture centerCircleBgTexture;
    private LoadedTexture segmentBgTexture;
    private LoadedTexture selectedSegmentOverlayTexture;
    private readonly GearRingElement gearRing;

    public ItemStack HeldItem { get; set; }
    public IReadOnlyList<BuildingMode> BuildingOptions { get; set; }

    public const string ToggleCombinationCode = "buildingmodedialog";
    public override string ToggleKeyCombinationCode => ToggleCombinationCode;

    public BuildingModeDialog(ICoreClientAPI capi, ItemStack heldItem, List<BuildingMode> buildingOptions)
        : base(capi)
    {
        segmentCoords = [];

        maxItemSize = (float)GuiElementPassiveItemSlot.unscaledSlotSize + (float)GuiElementItemSlotGridBase.unscaledSlotPadding;

        HeldItem = heldItem;
        BuildingOptions = buildingOptions;

        buildingModeChannel = capi.Network.GetChannel(SetBuildingModePacket.Channel);

        AssetLocation texturePath = new("qualityofbuilding", "gui/backgrounds/metal.png");
        backgroundPattern = GuiElement.getPattern(capi, texturePath, doCache: false, mulAlpha: 255, scale: 0.125f);
        wheelTexture = new(capi);
        centerCircleBgTexture = new(capi);
        segmentBgTexture = new(capi);
        selectedSegmentOverlayTexture = new(capi);

        float maxHalf = Math.Min(capi.Render.FrameHeight, capi.Render.FrameWidth) * 0.5f;
        gearRing = new(capi, backgroundPattern, buildingOptions.Count, maxHalf * RadialMenuStyle.RadiusFactor);
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
        gearRing.SetOptionsCount(BuildingOptions.Count);
        if (prevOptionsCount != BuildingOptions.Count)
        {
            prevOptionsCount = BuildingOptions.Count;
            RebuildTextures();
        }

        OnSlotOver(HeldItem.GetBuildingMode(BuildingOptions));
        prevSelectedMode = selectedMode;
    }

    public override void OnMouseMove(MouseEvent args)
    {
        base.OnMouseMove(args);

        int count = BuildingOptions.Count;
        if (count == 0)
        {
            return;
        }

        double dx = args.X - capi.Render.FrameWidth * 0.5;
        double dy = args.Y - capi.Render.FrameHeight * 0.5;

        // 32px dead zone near center
        // (32 * 32 = 1024) to avoid Math.Sqrt()
        if (dx * dx + dy * dy < 1024)
        {
            return;
        }

        // angle-to-index math
        // (angle * count / 2PI) shifts the range from [-PI, PI] to [-count/2, count/2]
        // adding (count * 0.25 + 0.5) rotates the starting point so Index 0 is at -PI/2 (top).
        double angle = Math.Atan2(dy, dx);
        double rawIndex = (angle * count / (2.0 * Math.PI)) + (count * 0.25) + 0.5;

        // handle wrapping (e.g. index -1 becomes count-1)
        int index = ((int)Math.Floor(rawIndex) % count + count) % count;

        OnSlotOver(index);
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

    public override void Dispose()
    {
        base.Dispose();
        GC.SuppressFinalize(this);

        backgroundPattern.Dispose();
        wheelTexture.Dispose();
        centerCircleBgTexture.Dispose();
        segmentBgTexture.Dispose();
        selectedSegmentOverlayTexture.Dispose();
    }

    #region Drawing
    public override void OnRenderGUI(float deltaTime)
    {
        if (BuildingOptions.Count == 0 || capi.World is not ClientMain client)
        {
            return;
        }

        float centerX = capi.Render.FrameWidth * 0.5f;
        float centerY = capi.Render.FrameHeight * 0.5f;
        int z = 11;

        // render backgrounds
        gearRing.OnRender(deltaTime);
        capi.Render.Render2DLoadedTextureCentered(wheelTexture, centerX, centerY, z++);
        capi.Render.Render2DLoadedTextureCentered(centerCircleBgTexture, centerX, centerY, z++);

        // render segments and items
        for (int i = 0; i < BuildingOptions.Count; i++)
        {
            client.Render2DTextureCenteredAndRotated(
                segmentBgTexture,
                segmentCoords[i].X,
                segmentCoords[i].Y,
                z,
                segmentCoords[i].Rotation
            );
            capi.Render.RenderItemstackToGui(
                BuildingOptions[i].RenderSlot,
                segmentCoords[i].X,
                segmentCoords[i].Y,
                z + 3,
                itemSize,
                ColorUtil.WhiteArgb,
                true,
                false
            );
        }

        // draw selection overlay
        client.Render2DTextureCenteredAndRotated(
            selectedSegmentOverlayTexture,
            segmentCoords[selectedMode].X,
            segmentCoords[selectedMode].Y,
            z + 2,
            segmentCoords[selectedMode].Rotation
        );

        base.OnRenderGUI(deltaTime);
    }

    private void RebuildTextures()
    {
        double maxHalf = Math.Min(capi.Render.FrameHeight, capi.Render.FrameWidth) * 0.5f;
        double radius = maxHalf * RadialMenuStyle.RadiusFactor;
        BuildBigCircle(radius);

        double outerRadius = radius;
        double innerRadius = outerRadius - maxItemSize * 2.5;
        BuildSmallCircle(innerRadius);
        BuildSegments(innerRadius, outerRadius);
        CalculateSizesAndPositions();
    }

    private void BuildBigCircle(double radius)
    {
        radius += RadialMenuStyle.Gap * 2;
        using ImageSurface surface = new(Format.Argb32, (int)radius * 2, (int)radius * 2);
        using Context context = new(surface);

        context.Arc(radius, radius, radius - RadialMenuStyle.Gap, 0, Math.PI * 2);
        context.LineWidth = RadialMenuStyle.Gap * 1.5;
        context.SetSourceRGBA(RadialMenuStyle.BorderColor);
        context.StrokePreserve();
        context.SetSource(backgroundPattern);
        context.FillPreserve();
        surface.Flush();

        capi.Gui.LoadOrUpdateCairoTexture(surface, false, ref wheelTexture);
    }

    private void BuildSmallCircle(double radius)
    {
        using ImageSurface surface = new(Format.Argb32, (int)radius * 2, (int)radius * 2);
        using Context context = new(surface);

        context.Arc(radius, radius, radius - RadialMenuStyle.Gap, 0, Math.PI * 2);
        double[] color = RadialMenuStyle.OverlayColor;
        context.SetSourceRGBA(color[0], color[1], color[2], color[3]);
        context.Fill();

        surface.Flush();
        capi.Gui.LoadOrUpdateCairoTexture(surface, false, ref centerCircleBgTexture);
    }

    private void BuildSegments(double innerRadius, double outerRadius)
    {
        int count = BuildingOptions.Count;
        double halfGap = RadialMenuStyle.Gap / 2.0;

        // dimensions
        double step = 2.0 * Math.PI / count;
        double halfStep = step / 2.0;
        double chordWidth = 2.0 * outerRadius * Math.Sin(halfStep);
        double thickness = outerRadius - innerRadius;
        int texSize = (int)Math.Ceiling(Math.Max(chordWidth, thickness)) + 4;
        double center = texSize / 2.0;

        // angles
        double baseStart = -Math.PI / 2.0 - halfStep;
        double baseEnd = baseStart + step;
        double gapOut = halfGap / outerRadius;
        double gapIn = halfGap / innerRadius;

        using ImageSurface surface = new(Format.Argb32, texSize, texSize);
        using Context ctx = new(surface);

        // translate to the center
        ctx.Translate(center, center);
        double midRadius = (innerRadius + outerRadius) / 2.0;
        ctx.Translate(0, midRadius);

        // path
        ctx.MoveTo(Math.Cos(baseStart + gapOut) * outerRadius, Math.Sin(baseStart + gapOut) * outerRadius);
        ctx.Arc(0, 0, outerRadius, baseStart + gapOut, baseEnd - gapOut);
        ctx.ArcNegative(0, 0, innerRadius, baseEnd - gapIn, baseStart + gapIn);
        ctx.ClosePath();

        // draw selected overlay
        ctx.SetSourceRGBA(1, 1, 1, 0.5);
        ctx.FillPreserve();
        surface.Flush();
        capi.Gui.LoadOrUpdateCairoTexture(surface, false, ref selectedSegmentOverlayTexture);

        // draw background
        ctx.Clear();
        ctx.SetSourceRGBA(RadialMenuStyle.OverlayColor);
        ctx.Fill();

        surface.Flush();
        capi.Gui.LoadOrUpdateCairoTexture(surface, false, ref segmentBgTexture);
    }

    private void CalculateSizesAndPositions()
    {
        int count = BuildingOptions.Count;
        float centerX = capi.Render.FrameWidth * 0.5f;
        float centerY = capi.Render.FrameHeight * 0.5f;

        float stepDeg = 360f / count;
        double stepRad = 2.0 * Math.PI / count;

        float minDim = Math.Min(capi.Render.FrameWidth, capi.Render.FrameHeight);
        float outerRad = minDim * 0.5f * RadialMenuStyle.RadiusFactor;
        float itemDist = outerRad - (maxItemSize * 1.25f);

        double startAngle = -Math.PI / 2.0;
        segmentCoords.Clear();
        for (int i = 0; i < count; i++)
        {
            double angle = startAngle + (i * stepRad);
            segmentCoords.Add(
                new()
                {
                    X = centerX + (float)(Math.Cos(angle) * itemDist),
                    Y = centerY + (float)(Math.Sin(angle) * itemDist),
                    Rotation = i * stepDeg,
                }
            );
        }

        itemSize = Math.Min(maxItemSize, outerRad * (float)stepRad / 3f);
    }
    #endregion

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
        RebuildTextures();
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
