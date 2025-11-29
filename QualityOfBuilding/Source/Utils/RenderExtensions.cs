using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace QualityOfBuilding.Source.Utils;

public static class RenderExtensions
{
    public static void Render2DLoadedTextureCentered(
        this IRenderAPI render,
        LoadedTexture textTexture,
        float posX,
        float posY,
        float z = 50f
    )
    {
        posX -= textTexture.Width * 0.5f;
        posY -= textTexture.Height * 0.5f;
        render.Render2DLoadedTexture(textTexture, posX, posY, z);
    }

    public static void Render2DTextureCenteredAndRotated(
        this ClientMain clientMain,
        LoadedTexture texture,
        double x,
        double y,
        double z,
        float angle,
        Vec4f? color = null
    )
    {
        x -= texture.Width * 0.5;
        y -= texture.Height * 0.5;
        clientMain.Render2DTextureRotated(texture, x, y, z, angle, color);
    }

    public static void Render2DTextureRotated(
        this ClientMain clientMain,
        LoadedTexture texture,
        double x,
        double y,
        double z,
        float angle,
        Vec4f? color = null
    )
    {
        // TODO: Fix pixel edge issues on rotation
        clientMain.guiShaderProg.RgbaIn = color ?? ColorUtil.WhiteArgbVec;
        clientMain.guiShaderProg.ExtraGlow = 0;
        clientMain.guiShaderProg.ApplyColor = 0;
        clientMain.guiShaderProg.NoTexture = 0.0f;
        clientMain.guiShaderProg.Tex2d2D = texture.TextureId;
        clientMain.guiShaderProg.OverlayOpacity = 0.0f;
        clientMain.guiShaderProg.NormalShaded = 0;
        clientMain.GlPushMatrix();
        clientMain.GlTranslate(x, y, z);
        double cx = texture.Width / 2f;
        double cy = texture.Height / 2f;
        clientMain.GlTranslate(cx, cy, 0.0);
        clientMain.GlRotate(angle, 0.0, 0.0, 1.0);
        clientMain.GlTranslate(-cx, -cy, 0.0);
        clientMain.GlScale(texture.Width, texture.Height, 0.0);
        clientMain.GlScale(0.5, 0.5, 0.0);
        clientMain.GlTranslate(1.0, 1.0, 0.0);
        clientMain.guiShaderProg.ProjectionMatrix = clientMain.CurrentProjectionMatrix;
        clientMain.guiShaderProg.ModelViewMatrix = clientMain.CurrentModelViewMatrix;
        clientMain.Platform.RenderMesh(clientMain.quadModel);

        clientMain.GlPopMatrix();
    }
}
