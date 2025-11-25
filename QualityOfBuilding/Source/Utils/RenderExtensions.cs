using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace QualityOfBuilding.Source.Utils;

public static class RenderExtensions
{
    public static void Render2DTextureRotated(
        this ClientMain clientMain,
        LoadedTexture texture,
        float x1,
        float y1,
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
        clientMain.GlTranslate((double)x1, (double)y1, 10.0);
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
