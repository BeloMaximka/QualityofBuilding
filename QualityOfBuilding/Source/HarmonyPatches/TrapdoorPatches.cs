using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace QualityOfBuilding.Source.HarmonyPatches;

[HarmonyPatch]
public static class TrapdoorPatches
{
    /// <summary>
    /// For some reason <see cref="BlockBeeHiveKilnDoor" /> has a check for air block in 
    /// <see cref="BlockBeeHiveKilnDoor.TryPlaceBlock" /> 
    /// <br/>that is different from <see cref="Block.CanPlaceBlock" /> which it uses
    /// <br/><a href="https://github.com/anegostudios/vssurvivalmod/blob/90b87707038ed3803e7b76170e9f75d64196ea83/Block/BlockBeeHiveKilnDoor.cs#L17">Source</a>
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(BlockBeeHiveKilnDoor), nameof(BlockBeeHiveKilnDoor.TryPlaceBlock))]
    public static bool FixDiscepancyWithCanPlaceBlock(
        BlockBeeHiveKilnDoor __instance,
        ref bool __result,
        IWorldAccessor world,
        IPlayer byPlayer,
        ItemStack itemstack,
        BlockSelection blockSel,
        ref string failureCode
    )
    {
        BlockPos pos = blockSel.Position;
        IBlockAccessor ba = world.BlockAccessor;

        __result = __instance.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode);
        if (__result)
        {
            __instance.placeDoor(world, byPlayer, itemstack, blockSel, pos, ba);
        }

        return false;
    }

    /// <summary>
    /// For some reason <see cref="BlockBehaviorTrapDoor" /> has a check for air block in 
    /// <br/><see cref="BlockBehaviorTrapDoor.TryPlaceBlock" /> 
    /// <br/>that is different from <see cref="Block.CanPlaceBlock" /> which it uses
    /// <br/><a href="https://github.com/anegostudios/vssurvivalmod/blob/90b87707038ed3803e7b76170e9f75d64196ea83/BlockBehavior/BlockBehaviorTrapDoor.cs#L117">Source</a>
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(BlockBehaviorTrapDoor), nameof(BlockBehaviorTrapDoor.TryPlaceBlock))]
    public static bool FixDiscepancyWithCanPlaceBlock(
        BlockBehaviorTrapDoor __instance,
        ref bool __result,
        IWorldAccessor world,
        IPlayer byPlayer,
        ItemStack itemstack,
        BlockSelection blockSel,
        ref EnumHandling handling,
        ref string failureCode
    )
    {
        handling = EnumHandling.PreventDefault;
        BlockPos pos = blockSel.Position;
        IBlockAccessor ba = world.BlockAccessor;

        __result = __instance.block.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode);
        if (__result)
        {
            __instance.placeDoor(world, byPlayer, itemstack, blockSel, pos, ba);
        }

        return false;
    }
}
