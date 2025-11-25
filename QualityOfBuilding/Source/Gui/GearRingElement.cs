using Cairo;
using QualityOfBuilding.Source.Utils;
using System;
using Vintagestory.API.Client;
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
        if (capi.World is ClientMain client)
        {
            float gearAngleStep = gearAngleTarget - gearAngleCurrent;
            if (gearAngleStep < -180)
            {
                gearAngleStep += 360;
            }
            else if (gearAngleStep >= 180)
            {
                gearAngleStep -= 360;
            }
            gearAngleCurrent += gearAngleStep * deltaTime * 10;

            float xPos = (capi.Render.FrameWidth / 2.0f) - (gearRingTexture.Width / 2.0f);
            float yPos = (capi.Render.FrameHeight / 2.0f) - (gearRingTexture.Height / 2.0f);
            client.Render2DTextureRotated(gearRingTexture, xPos, yPos, gearAngleCurrent);
        }
    }

    public void Compose()
    {
        float maxItemSize = (float)GuiElementPassiveItemSlot.unscaledSlotSize + (float)GuiElementItemSlotGridBase.unscaledSlotPadding;

        double outerRadius = radius + maxItemSize / 2;
        int toothCount = Math.Max(8, optionsCount);
        double angleStep = 2.0 * Math.PI / toothCount;
        double toothWidth = outerRadius * (float)angleStep / 1.5;
        double toothHeight = Math.Min(toothWidth / 2, maxItemSize);

        double maxRadius = outerRadius + toothHeight;
        int texSize = (int)Math.Ceiling(maxRadius * 2) + 2;

        using ImageSurface surface = new(Format.Argb32, texSize, texSize);
        using Context context = new(surface);
        context.Clear();
        context.NewPath();

        float centerX = texSize * 0.5f;
        float centerY = texSize * 0.5f;

        double toothStartAngle = -Math.PI;

        for (int i = 0; i < toothCount; i++)
        {
            double angle = toothStartAngle + i * angleStep;

            double halfBaseWidth = toothWidth / 2;

            // make the tip narrower
            // TODO: calculate tip width so it perfectly matches the tooth gaps
            double halfTipWidth = halfBaseWidth * 0.8;

            double x0 = -halfBaseWidth;
            double y0 = outerRadius;
            double x1 = halfBaseWidth;
            double y1 = outerRadius;

            double x2 = halfTipWidth;
            double y2 = outerRadius + toothHeight;
            double x3 = -halfTipWidth;
            double y3 = outerRadius + toothHeight;

            double cos = Math.Cos(angle);
            double sin = Math.Sin(angle);

            if (i == 0)
            {
                context.LineTo(centerX + x1 * cos - y1 * sin, centerY + x1 * sin + y1 * cos);
                context.LineTo(centerX + y2 * sin, centerY + sin + y2 * cos);
                context.LineTo(centerX + x0 * cos - y0 * sin, centerY + x0 * sin + y0 * cos);
                continue;
            }

            context.LineTo(centerX + x1 * cos - y1 * sin, centerY + x1 * sin + y1 * cos);
            context.LineTo(centerX + x2 * cos - y2 * sin, centerY + x2 * sin + y2 * cos);
            context.LineTo(centerX + x3 * cos - y3 * sin, centerY + x3 * sin + y3 * cos);
            context.LineTo(centerX + x0 * cos - y0 * sin, centerY + x0 * sin + y0 * cos);
        }
        context.MoveTo(centerX, centerY);
        context.FillRule = FillRule.EvenOdd;
        context.Arc(centerX, centerY, radius, 0, 2 * Math.PI);
        double[] color = GuiStyle.DialogLightBgColor;
        context.SetSourceRGBA(color[0], color[1], color[2], color[3]);
        context.Fill();
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
            gearRingTexture?.Dispose();
        }

        disposed = true;
    }
}
