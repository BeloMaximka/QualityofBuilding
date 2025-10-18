using HarmonyLib;
using QualityOfBuilding.Source.CollectibleBehaviors.BuildingModes;
using QualityOfBuilding.Source.Utils;
using QualityOfBuilding.Source.Utils.Inventory;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace QualityOfBuilding.Source.HarmonyPatches;

[HarmonyPatch]
public static class ItemChiselPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ItemChisel), nameof(ItemChisel.SetToolMode))]
    public static bool HandleItemWithBuildingMode(ICoreAPI ___api, IPlayer byPlayer, BlockSelection blockSel)
    {
        ItemSlot mouseslot = byPlayer.InventoryManager.MouseItemSlot;
        if (
            mouseslot.Itemstack is null
            || mouseslot.Itemstack.Collectible.GetBehavior<BuildingItemBehavior>() is not BuildingItemBehavior buildingBehavior
             || buildingBehavior.ToolModes[mouseslot.Itemstack.GetBuildingMode(buildingBehavior.ToolModes)].Data is not BuildingModeContext context
            || context.Output is null
            || blockSel is null
        )
        {
            return true;
        }

        if (
            ___api.World.BlockAccessor.GetBlockEntity(blockSel.Position) is not BlockEntityChisel be
            || !ItemChisel.IsValidChiselingMaterial(___api, blockSel.Position, context.Output.Block, byPlayer)
        )
        {
            return false;
        }

        if (byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)
        {
            be.AddMaterial(context.Output.Block, out bool isFull);
            if (!isFull && !byPlayer.TryTakeItems(context.Ingredients))
            {
                be.RemoveMaterial(context.Output.Block);
            }
        }
        else
        {
            be.AddMaterial(context.Output.Block, out _, false);
        }

        be.MarkDirty();
        ___api.Event.PushEvent("keepopentoolmodedlg");

        return false;
    }
}
