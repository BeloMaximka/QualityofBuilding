using ImmersiveBuilding.Source.CollectibleBehaviors.ShovelModes;
using ImmersiveBuilding.Source.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace ImmersiveBuilding.Source.BlockBehaviors;

internal class StonePathStairsBehavior(Block block) : BlockBehavior(block)
{
    public override WorldInteraction[] GetPlacedBlockInteractionHelp(
        IWorldAccessor world,
        BlockSelection selection,
        IPlayer forPlayer,
        ref EnumHandling handling
    )
    {
        if (forPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack?.HasMode(ShovelBehavior.StonePathToolModeCode) == true)
        {
            return
            [
                .. base.GetPlacedBlockInteractionHelp(world, selection, forPlayer, ref handling),
                new()
                {
                    Itemstacks = [forPlayer.InventoryManager.ActiveHotbarSlot.Itemstack],
                    ActionLangCode = "interactionhelp-build-slab",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCode = "shift",
                },
            ];
        }
        return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer, ref handling);
    }
}
