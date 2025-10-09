using HarmonyLib;
using ImmersiveBuilding.Source.CollectibleBehaviors.BuildingModes;
using ImmersiveBuilding.Source.Utils.Inventory;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace ImmersiveBuilding.Source.HarmonyPatches;

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
            || buildingBehavior.GetSelectedModeHandler(mouseslot.Itemstack) is not BuildingModeHandler handler
            || handler.Output is null
            || blockSel is null
        )
        {
            return true;
        }

        if (
            ___api.World.BlockAccessor.GetBlockEntity(blockSel.Position) is not BlockEntityChisel be
            || !ItemChisel.IsValidChiselingMaterial(___api, blockSel.Position, handler.Output.Block, byPlayer)
        )
        {
            return false;
        }

        if (byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)
        {
            be.AddMaterial(handler.Output.Block, out bool isFull);
            if (!isFull && !byPlayer.TryTakeItems(handler.Ingredients))
            {
                be.RemoveMaterial(handler.Output.Block);
            }
        }
        else
        {
            be.AddMaterial(handler.Output.Block, out _, false);
        }

        be.MarkDirty();
        ___api.Event.PushEvent("keepopentoolmodedlg");

        return false;
    }
}
