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
             || buildingBehavior.BuildingModes[mouseslot.Itemstack.GetBuildingMode(buildingBehavior.BuildingModes)] is not BuildingMode mode
            || mode.Output is null
            || blockSel is null
        )
        {
            return true;
        }

        if (
            ___api.World.BlockAccessor.GetBlockEntity(blockSel.Position) is not BlockEntityChisel be
            || !ItemChisel.IsValidChiselingMaterial(___api, blockSel.Position, mode.Output.Block, byPlayer)
        )
        {
            return false;
        }

        if (byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)
        {
            be.AddMaterial(mode.Output.Block, out bool isFull);
            if (!isFull && !byPlayer.TryTakeItems(mode.Ingredients))
            {
                be.RemoveMaterial(mode.Output.Block);
            }
        }
        else
        {
            be.AddMaterial(mode.Output.Block, out _, false);
        }

        be.MarkDirty();
        ___api.Event.PushEvent("keepopentoolmodedlg");

        return false;
    }
}
