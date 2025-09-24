using HarmonyLib;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace ImmersiveBuilding.HarmonyPatches;

[HarmonyPatch]
public static class HardcodedDropsPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(BlockStairs), nameof(BlockStairs.GetDrops))]
    public static void FixBlockStairs(ref ItemStack[] __result) => FixDropsFor<BlockStairs>(ref __result);

    [HarmonyPostfix]
    [HarmonyPatch(typeof(BlockSlab), nameof(BlockSlab.GetDrops))]
    public static void FixBlockSlab(ref ItemStack[] __result) => FixDropsFor<BlockSlab>(ref __result);

    [HarmonyPostfix]
    [HarmonyPatch(typeof(BlockFence), nameof(BlockFence.GetDrops))]
    public static void FixBlockFence(ref ItemStack[] __result) => FixDropsFor<BlockFence>(ref __result);

    [HarmonyPostfix]
    [HarmonyPatch(typeof(BlockFenceGate), nameof(BlockFenceGate.GetDrops))]
    public static void FixBlockFenceGate(ref ItemStack[] __result) => FixDropsFor<BlockFenceGate>(ref __result);

    private static void FixDropsFor<TBlock>(ref ItemStack[] drops)
        where TBlock : Block
    {
        if (drops == null || drops.Length != 1 || drops[0].Block is not TBlock) // check for correct code instead?
        {
            return;
        }

        drops = [.. drops[0].Block.Drops.Select(blockDrop => blockDrop.ResolvedItemstack)];
    }
}
