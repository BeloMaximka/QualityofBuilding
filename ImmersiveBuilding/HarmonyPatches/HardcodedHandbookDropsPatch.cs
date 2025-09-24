using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace ImmersiveBuilding.HarmonyPatches;

[HarmonyPatch]
public static class HardcodedHandbookDropsPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(BlockStairs), nameof(BlockStairs.GetDropsForHandbook))]
    public static void FixBlockStairs(ref BlockDropItemStack[] __result, ItemStack handbookStack) =>
        FixHandbookDropsFor<BlockStairs>(handbookStack, ref __result);

    [HarmonyPostfix]
    [HarmonyPatch(typeof(BlockSlab), nameof(BlockSlab.GetDropsForHandbook))]
    public static void FixBlockSlab(ref BlockDropItemStack[] __result, ItemStack handbookStack) =>
        FixHandbookDropsFor<BlockSlab>(handbookStack, ref __result);

    [HarmonyPostfix]
    [HarmonyPatch(typeof(BlockFence), nameof(BlockFence.GetDropsForHandbook))]
    public static void FixBlockFence(ref BlockDropItemStack[] __result, ItemStack handbookStack) =>
        FixHandbookDropsFor<BlockFence>(handbookStack, ref __result);

    [HarmonyPostfix]
    [HarmonyPatch(typeof(BlockFenceGate), nameof(BlockFenceGate.GetDropsForHandbook))]
    public static void FixBlockFenceGate(ref BlockDropItemStack[] __result, ItemStack handbookStack) =>
        FixHandbookDropsFor<BlockFenceGate>(handbookStack, ref __result);

    private static void FixHandbookDropsFor<TBlock>(ItemStack itemStack, ref BlockDropItemStack[] drops)
        where TBlock : Block
    {
        if (drops is null || itemStack is null || itemStack.Block is not TBlock) // check for correct code instead?
        {
            return;
        }

        drops = itemStack.Block.Drops;
    }
}
