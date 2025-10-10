using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace ImmersiveBuilding.Source.Utils;
public static class RenderSkillItemDelegateUtils
{
    public static RenderSkillItemDelegate GetRenderDelegate(this ItemStack itemStack, ICoreClientAPI capi)
    {
        return new DummySlot(itemStack).GetRenderDelegate(capi);
    }

    public static RenderSkillItemDelegate GetRenderDelegate(this ItemSlot slot, ICoreClientAPI capi)
    {
        return (code, dt, posX, posY) =>
        {
            double size = GuiElementPassiveItemSlot.unscaledSlotSize + GuiElementItemSlotGridBase.unscaledSlotPadding;
            double scsize = GuiElement.scaled(size - 5);

            capi.Render.RenderItemstackToGui(
                slot,
                posX + scsize / 2,
                posY + scsize / 2,
                100,
                (float)GuiElement.scaled(GuiElementPassiveItemSlot.unscaledItemSize),
                ColorUtil.WhiteArgb
            );
        };
    }
}
