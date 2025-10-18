using HarmonyLib;
using QualityOfBuilding.Source.CollectibleBehaviors.BuildingModes;
using QualityOfBuilding.Source.Utils;
using QualityOfBuilding.Source.Utils.Inventory;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace QualityOfBuilding.Source.HarmonyPatches;

[HarmonyPatch]
public static class BlockBloomeryPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(BlockBloomery), nameof(BlockBloomery.OnBlockInteractStart))]
    public static bool HandleItemWithBuildingMode(
        BlockBloomery __instance,
        ref bool __result,
        IWorldAccessor world,
        IPlayer byPlayer,
        BlockSelection blockSel
    )
    {
        ItemStack? hotbarstack = byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack;
        if (
            blockSel is null
            || hotbarstack?.Collectible?.GetBehavior<BuildingItemBehavior>() is not BuildingItemBehavior buildingBehavior
            || buildingBehavior.ToolModes[hotbarstack.GetBuildingMode(buildingBehavior.ToolModes)].Data is not BuildingModeContext context
            || context.Output?.Collectible?.Code?.PathStartsWith("bloomerychimney") == false
        )
        {
            return true;
        }
        __result = true;


        Block aboveBlock = world.BlockAccessor.GetBlock(blockSel.Position.UpCopy());
        if (aboveBlock.IsReplacableBy(context.Output!.Block))
        {
            if (byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative && !byPlayer.TryTakeItems(context.Ingredients))
            {
                return false;
            }

            context.Output.Block.DoPlaceBlock(
                world,
                byPlayer,
                new BlockSelection() { Position = blockSel.Position.UpCopy(), Face = BlockFacing.UP },
                context.Output!
            );
            world.PlaySoundAt(__instance.Sounds?.Place, blockSel.Position, 0.5, byPlayer, true, 16, 1);
        }

        return false;
    }
}
