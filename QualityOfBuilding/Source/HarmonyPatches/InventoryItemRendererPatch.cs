using HarmonyLib;
using QualityOfBuilding.Source.CollectibleBehaviors;
using QualityOfBuilding.Source.CollectibleBehaviors.BuildingModes;
using QualityOfBuilding.Source.Utils;
using Vintagestory.API.Common;
using Vintagestory.Client.NoObf;

namespace QualityOfBuilding.Source.HarmonyPatches;

[HarmonyPatch]
public static class InventoryItemRendererPatch
{
    private static readonly DummySlot outputSlot = new();

    // Maybe it's worth to try a different approach with RegisterItemstackRenderer
    [HarmonyPrefix]
    [HarmonyPatch(
        typeof(InventoryItemRenderer),
        nameof(InventoryItemRenderer.RenderItemstackToGui),
        typeof(ItemSlot),
        typeof(double),
        typeof(double),
        typeof(double),
        typeof(float),
        typeof(int),
        typeof(float),
        typeof(bool),
        typeof(bool),
        typeof(bool)
    )]
    public static bool RenderBuildingOutputInSlot(
        InventoryItemRenderer __instance,
        ref ItemSlot inSlot,
        ref double posX,
        ref double posY,
        ref double posZ,
        ref float size,
        ref int color,
        ref bool shading,
        ref bool origRotate,
        ref bool showStackSize
    )
    {
        if (
            inSlot.Itemstack is null
            || inSlot.Itemstack.Collectible.GetCollectibleBehavior<CustomToolModeBehavior>(true) is not CustomToolModeBehavior behavior
            || behavior.ToolModes.Count == 0
            || behavior.ToolModes[inSlot.Itemstack.GetBuildingMode(behavior.ToolModes)].Data is not BuildingModeContext context
            || context.Output is null
        )
        {
            return true;
        }

        int originalQuantity = context.Output.StackSize;
        context.Output.StackSize = inSlot.StackSize;
        outputSlot.Itemstack = context.Output;
        __instance.RenderItemstackToGui(
            outputSlot,
            posX - size * 0.3,
            posY - size * 0.3,
            posZ,
            size * 0.7f,
            color,
            shading,
            origRotate: false,
            showStackSize: false
        );

        // Workaround to properly render quantity
        __instance.RenderItemstackToGui(outputSlot, posX, posY, posZ, size, 0, shading, origRotate: false, showStackSize);
        outputSlot.Itemstack = null;
        context.Output.StackSize = originalQuantity;

        posX += size * 0.2;
        posY += size * 0.2;
        posZ += size;
        size *= 0.8f;
        origRotate = false;
        showStackSize = false;

        return true;
    }
}
