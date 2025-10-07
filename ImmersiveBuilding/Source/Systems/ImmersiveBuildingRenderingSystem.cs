using ImmersiveBuilding.Source.CollectibleBehaviors.BuildingModes;
using ImmersiveBuilding.Source.Common;
using ImmersiveBuilding.Source.Gui;
using ImmersiveBuilding.Source.Render;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace ImmersiveBuilding.Source.Systems;

public class ImmersiveBuildingRenderingSystem : ModSystem
{
    private ICoreClientAPI capi = null!;

    public static SkillModeBuildingHud SkillModeHud { get; private set; } = null!;

    public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Client;

    public override void StartClientSide(ICoreClientAPI api)
    {
        capi = api;
        SkillModeHud = new SkillModeBuildingHud(api);

        api.Input.RegisterHotKey(
            BuildingModeDialog.ToggleCombinationCode,
            "Opens the building dialog",
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

                HandleSkillModeHudForActiveSlot(player);
            };
        };

        api.Event.BeforeActiveSlotChanged += (args) =>
        {
            BuildingModeDialogSingleton.TryClose();
            return EnumHandling.PassThrough;
        };

        // Update hud for new selected slot
        api.Event.AfterActiveSlotChanged += (args) =>
        {
            HandleSkillModeHudForActiveSlot(capi.World.Player);
        };
    }

    public override void Dispose()
    {
        BuildingModeDialogSingleton.FreeRam();
        SkillModeHud = null!;
    }

    private bool ToggleBuildingModeDialog(KeyCombination combination)
    {
        ItemSlot? slot = capi.World.Player.InventoryManager.ActiveHotbarSlot;
        BuildingItemBehavior? behavior = GetBuildingItemBehavior(slot);
        if (behavior is null)
        {
            return false;
        }

        behavior.ToggleDialog(slot);
        return true;
    }

    private static void HandleSkillModeHudForActiveSlot(IClientPlayer player)
    {
        ItemSlot? slot = player.InventoryManager.ActiveHotbarSlot;
        BuildingItemBehavior? behavior = GetBuildingItemBehavior(slot);
        if (behavior is null)
        {
            SkillModeHud.TryClose();
            BuildingModeDialogSingleton.TryClose();
            return;
        }

        if (behavior?.GetSelectedBuildingOutput(slot) is ItemStack item)
        {
            if (slot.Itemstack.Attributes.GetInt(SharedConstants.BuildingModeAttributeName) == 0)
            {
                SkillModeHud.TryClose();
                return;
            }

            SkillModeHud.Item = item;
            if (!SkillModeHud.IsOpened())
            {
                SkillModeHud.TryOpen();
            }
            return;
        }

        SkillModeHud.TryClose();
    }

    private static BuildingItemBehavior? GetBuildingItemBehavior(ItemSlot? slot) =>
        slot?.Itemstack is ItemStack activeItem ? activeItem.Collectible.GetBehavior<BuildingItemBehavior>() : null;
}
