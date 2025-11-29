using Cairo;
using QualityOfBuilding.Source.Utils;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.Client.NoObf;

namespace QualityOfBuilding.Source.Gui;

public class GearRingElement(ICoreClientAPI capi, int optionsCount, float radius) : IDisposable
{
    private bool disposed;
    private LoadedTexture gearRingTexture = new(capi);
    private float gearAngleCurrent = 0;
    private float gearAngleTarget = 0;

    public void SetOptionsCount(int value)
    {
        if (optionsCount != value)
        {
            optionsCount = value;
            Compose();
        }
    }

    public void SetSelectedOption(int value)
    {
        gearAngleTarget = 360f / optionsCount * value;
    }

    public void OnRender(float deltaTime)
    {
        if (capi.World is not ClientMain client)
        {
            return;
        }

        float step = gearAngleTarget - gearAngleCurrent;
        // calculate a proper shorter path
        // so if current angle is 10 and target is 350 it doesnt do a full spin
        if (step < -180)
        {
            step += 360;
        }
        if (step >= 180)
        {
            step -= 360;
        }

        gearAngleCurrent += step * deltaTime * 10;

        float x = (capi.Render.FrameWidth - gearRingTexture.Width) * 0.5f;
        float y = (capi.Render.FrameHeight - gearRingTexture.Height) * 0.5f;
        client.Render2DTextureRotated(gearRingTexture, x, y, 10, gearAngleCurrent);
    }

    public void Compose()
    {
        // calculate geometry
        double maxItemSize = GuiElementPassiveItemSlot.unscaledSlotSize + GuiElementItemSlotGridBase.unscaledSlotPadding;
        double outerRadius = radius + maxItemSize / 2;

        int count = Math.Max(8, optionsCount);
        double angleStep = 2.0 * Math.PI / count;

        double toothHeight = Math.Min(outerRadius * angleStep / 3, maxItemSize);
        double halfBase = outerRadius * angleStep / 3;
        double halfTip = halfBase * 0.8;
        double yTip = outerRadius + toothHeight;

        // setup surface
        int texSize = (int)Math.Ceiling((outerRadius + toothHeight) * 2) + 32;
        double center = texSize * 0.5f;

        using ImageSurface surface = new(Format.Argb32, texSize, texSize);
        using Context ctx = new(surface);

        // helper: rotates a point (x, y) by (cos, sin) and draws the line
        void LineToRot(double x, double y, double cos, double sin) => ctx.LineTo(center + x * cos - y * sin, center + x * sin + y * cos);

        // draw Teeth
        for (int i = 0; i < count; i++)
        {
            double angle = -Math.PI + i * angleStep;
            double cos = Math.Cos(angle);
            double sin = Math.Sin(angle);

            LineToRot(halfBase, outerRadius, cos, sin); // bottom right

            if (i == 0)
            {
                // draw a triangular tooh for the first one
                ctx.LineTo(center + yTip * sin, center + sin + yTip * cos); // top center
            }
            else
            {
                LineToRot(halfTip, yTip, cos, sin); // top right
                LineToRot(-halfTip, yTip, cos, sin); // top left
            }

            LineToRot(-halfBase, outerRadius, cos, sin); // bottom left
        }
        ctx.ClosePath();

        // draw a hole in the center
        ctx.NewSubPath();
        ctx.FillRule = FillRule.EvenOdd;
        ctx.Arc(center, center, radius, 0, 2 * Math.PI);

        // fill with texture
        AssetLocation texturePath = new("qualityofbuilding", "gui/backgrounds/metal.png");
        using SurfacePattern pattern = GuiElement.getPattern(capi, texturePath, doCache: false, mulAlpha: 255, scale: 0.125f);
        ctx.SetSource(pattern);
        ctx.FillPreserve();

        FillShade(ctx, surface, 4);

        surface.Flush();
        capi.Gui.LoadOrUpdateCairoTexture(surface, false, ref gearRingTexture);
    }

    // TODO: move to the shader
    public static void FillShade(Context ctx, ImageSurface surface, int radius)
    {
        ctx.Save();
        ctx.LineWidth = radius;
        ctx.SetSourceRGBA(0.12, 0.1, 0.08, 1);
        ctx.StrokePreserve();
        ctx.Restore();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed)
            return;

        if (disposing)
        {
            gearRingTexture.Dispose();
        }

        disposed = true;
    }
}
