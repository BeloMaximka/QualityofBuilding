using HarmonyLib;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace ImmersiveBuilding.Source.HarmonyPatches;

[HarmonyPatch]
public static class HardcodedDropsPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(BlockStairs), nameof(BlockStairs.GetDrops))]
    public static void FixBlockStairs(BlockStairs __instance, ref ItemStack[] __result) => FixDropsFor(__instance, ref __result);

    [HarmonyPostfix]
    [HarmonyPatch(typeof(BlockSlab), nameof(BlockSlab.GetDrops))]
    public static void FixBlockSlab(BlockSlab __instance, ref ItemStack[] __result) => FixDropsFor(__instance, ref __result);

    [HarmonyPostfix]
    [HarmonyPatch(typeof(BlockFence), nameof(BlockFence.GetDrops))]
    public static void FixBlockFence(BlockFence __instance, ref ItemStack[] __result) => FixDropsFor(__instance, ref __result);

    [HarmonyPostfix]
    [HarmonyPatch(typeof(BlockFenceGate), nameof(BlockFenceGate.GetDrops))]
    public static void FixBlockFenceGate(BlockFenceGate __instance, ref ItemStack[] __result) => FixDropsFor(__instance, ref __result);

    // This one can be removed if we utilize the `drop` property from BlockBehaviorHorizontalOrientable. Maybe.
    [HarmonyPostfix]
    [HarmonyPatch(typeof(BlockBehaviorHorizontalOrientable), nameof(BlockBehaviorHorizontalOrientable.GetDrops))]
    public static void FixBlockBehaviorHorizontalOrientable(BlockBehaviorHorizontalOrientable __instance, ref ItemStack[] __result)
    {
        if (__result == null || __result.Length != 1 || __result[0]?.Block?.FirstCodePart() != __instance.block.FirstCodePart())
        {
            return;
        }

        __result = [.. __result[0].Block.Drops.Select(blockDrop => blockDrop.ResolvedItemstack)];
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(BlockBehaviorNWOrientable), nameof(BlockBehaviorNWOrientable.GetDrops))]
    public static void FixBlockBehaviorNWOrientable(BlockBehaviorNWOrientable __instance, ref ItemStack[] __result)
    {
        if (__result == null || __result.Length != 1 || __result[0]?.Block?.FirstCodePart() != __instance.block.FirstCodePart())
        {
            return;
        }

        __result = [.. __result[0].Block.Drops.Select(blockDrop => blockDrop.ResolvedItemstack)];
    }

    private static void FixDropsFor<TBlock>(TBlock block, ref ItemStack[] drops)
        where TBlock : Block
    {
        if (drops == null || drops.Length != 1 || drops[0]?.Block?.FirstCodePart() != block.FirstCodePart())
        {
            return;
        }

        drops = [.. drops[0].Block.Drops.Select(blockDrop => blockDrop.ResolvedItemstack)];
    }
}
