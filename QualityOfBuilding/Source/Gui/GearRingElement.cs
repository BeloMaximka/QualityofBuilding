using Cairo;
using QualityOfBuilding.Source.Utils;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.Client.NoObf;

namespace QualityOfBuilding.Source.Gui;

public class GearRingElement : IDisposable
{
    private bool disposed;
    private LoadedTexture gearRingTexture;
    private float gearAngleCurrent = 0;
    private float gearAngleTarget = 0;
    private int optionsCount;
    private readonly ICoreClientAPI capi;
    private readonly Pattern background;
    private readonly Pattern cursor;
    private readonly float radius;

    public GearRingElement(ICoreClientAPI capi, Pattern background, int optionsCount, float radius)
    {
        this.capi = capi;
        this.background = background;
        this.optionsCount = optionsCount;
        this.radius = radius;
        gearRingTexture = new(capi);

        AssetLocation texturePath = new("qualityofbuilding", "gui/backgrounds/temporal.png");
        cursor = GuiElement.getPattern(capi, texturePath, doCache: false, mulAlpha: 255, scale: 0.125f);
    }

    public void SetOptionsCount(int value)
    {
        if (optionsCount != value)
        {
            gearAngleCurrent = 0;
            gearAngleTarget = 0;
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

        // calculate the shortest distance
        float step = (gearAngleTarget - gearAngleCurrent) % 360;

        // normalize to range [-180, 180]
        if (step < -180)
        {
            step += 360;
        }
        if (step > 180)
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
        double maxItemSize = GuiElementPassiveItemSlot.unscaledSlotSize + GuiElementItemSlotGridBase.unscaledSlotPadding;
        double outerRadius = radius + maxItemSize / 2.0;

        int count = Math.Max(8, optionsCount);
        double angleStep = 2.0 * Math.PI / count;

        double toothH = Math.Min(outerRadius * angleStep / 3.0, maxItemSize);
        double halfBase = outerRadius * angleStep / 3.0;
        double halfTip = halfBase * 0.8;
        double yTip = outerRadius + toothH;

        int texSize = (int)Math.Ceiling(yTip * 2) + 32;
        using ImageSurface surface = new(Format.Argb32, texSize, texSize);
        using Context ctx = new(surface);

        double center = texSize / 2.0;
        ctx.Translate(center, center);
        ctx.Rotate(-Math.PI); // start at top

        // teeth
        for (int i = 0; i < count; i++)
        {
            ctx.LineTo(halfBase, outerRadius); // bottom right

            if (i == 0)
            {
                ctx.LineTo(0, yTip); // triangle tip for the first tooth
            }
            else
            {
                ctx.LineTo(halfTip, yTip); // top right
                ctx.LineTo(-halfTip, yTip); // top left
            }

            ctx.LineTo(-halfBase, outerRadius); // bottom left

            // rotate context for the next tooth
            ctx.Rotate(angleStep);
        }

        ctx.ClosePath();

        // paint
        ctx.SetSource(background);
        ctx.FillPreserve();

        ctx.LineWidth = RadialMenuStyle.Gap / 2.0;
        ctx.SetSourceRGBA(RadialMenuStyle.BorderColor);
        ctx.Stroke();

        // selection arrow
        ctx.MoveTo(halfBase - RadialMenuStyle.Gap * 1.5, outerRadius);
        ctx.LineTo(0, yTip - RadialMenuStyle.Gap * 1.5);
        ctx.LineTo(-halfBase + RadialMenuStyle.Gap * 1.5, outerRadius);
        ctx.ClosePath();
        ctx.SetSource(cursor);
        ctx.FillPreserve();
        ctx.SetSourceRGBA(RadialMenuStyle.CursorBorderColor);
        ctx.LineWidth = RadialMenuStyle.Gap / 4.0;
        ctx.LineJoin = LineJoin.Bevel;
        ctx.Stroke();

        surface.Flush();
        capi.Gui.LoadOrUpdateCairoTexture(surface, false, ref gearRingTexture);
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
            cursor.Dispose();
            gearRingTexture.Dispose();
        }

        disposed = true;
    }
}
