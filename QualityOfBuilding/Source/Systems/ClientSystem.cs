using QualityOfBuilding.Source.BlockBehaviors;
using QualityOfBuilding.Source.CollectibleBehaviors;
using QualityOfBuilding.Source.CollectibleBehaviors.BuildingModes.Handbook;
using QualityOfBuilding.Source.Commands;
using QualityOfBuilding.Source.Gui;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace QualityOfBuilding.Source.Systems;

public class ClientSystem : ModSystem
{
    private ICoreClientAPI capi = null!;

    public override double ExecuteOrder() => 1.2;

    public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Client;

    public override void StartClientSide(ICoreClientAPI api)
    {
        capi = api;

        CheckInvalidRecipeProportionsCommand.Register(capi);

        api.Input.RegisterHotKey(
            BuildingModeDialog.ToggleCombinationCode,
            Lang.Get("heldhelp-building-menu"),
            GlKeys.B,
            HotkeyType.GUIOrOtherControls
        );

        api.Input.SetHotKeyHandler(BuildingModeDialog.ToggleCombinationCode, ToggleBuildingModeDialog);

        // Maybe there's a better event for handling active slot updates
        api.Event.PlayerEntitySpawn += (player) =>
        {
            if (capi.World.Player != player)
            {
                return;
            }

            player.InventoryManager.GetHotbarInventory().SlotModified += (slotId) =>
            {
                if (player.InventoryManager.ActiveHotbarSlotNumber != slotId)
                {
                    return;
                }

                if (GetBuildingItemBehavior(player.InventoryManager.ActiveHotbarSlot) is null)
                {
                    BuildingModeDialogSingleton.TryClose();
                }
            };
        };

        api.Event.BeforeActiveSlotChanged += (args) =>
        {
            BuildingModeDialogSingleton.TryClose();
            return EnumHandling.PassThrough;
        };

        foreach (Block stonePathBlock in api.World.SearchBlocks("stonepath-*"))
        {
            stonePathBlock.BlockBehaviors = stonePathBlock.BlockBehaviors.Append(new StonePathBehavior(stonePathBlock));
        }
        foreach (Block pathStairsStairsBlock in api.World.SearchBlocks("stonepathstairs-*"))
        {
            pathStairsStairsBlock.BlockBehaviors = pathStairsStairsBlock.BlockBehaviors.Append(
                new StonePathBehavior(pathStairsStairsBlock)
            );
        }
    }

    public override void AssetsFinalize(ICoreAPI api)
    {
        capi.InitBuildingRecipesForHandbook();
    }

    public override void Dispose()
    {
        BuildingModeDialogSingleton.FreeRam();
    }

    private bool ToggleBuildingModeDialog(KeyCombination combination)
    {
        ItemSlot? slot = capi.World.Player.InventoryManager.ActiveHotbarSlot;
        CustomToolModeBehavior? behavior = GetBuildingItemBehavior(slot);
        if (behavior is null)
        {
            return false;
        }

        behavior.ToggleDialog(slot);
        return true;
    }

    private static CustomToolModeBehavior? GetBuildingItemBehavior(ItemSlot? slot) =>
        slot?.Itemstack is ItemStack activeItem ? activeItem.Collectible.GetCollectibleBehavior<CustomToolModeBehavior>(true) : null;
}
