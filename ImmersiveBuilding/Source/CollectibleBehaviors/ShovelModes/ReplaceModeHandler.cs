using System;
using System.Linq;
using Vintagestory.API.Common;

namespace ImmersiveBuilding.Source.CollectibleBehaviors.ShovelModes;

public class ReplaceModeHandler(int[] replacableBlockIds, int outputBlockId) : IModeHandler
{
    public void HandleStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
    {
        IPlayer? byPlayer = (byEntity as EntityPlayer)?.Player;
        if (byPlayer == null)
        {
            return;
        }

        Block block = byEntity.World.BlockAccessor.GetBlock(blockSel.Position);
        if (block.Id != outputBlockId && replacableBlockIds.Contains(block.Id))
        {
            byEntity.World.PlaySoundAt(block.Sounds.Break, blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, byPlayer);
            byEntity.World.BlockAccessor.BreakBlock(blockSel.Position, byPlayer, dropQuantityMultiplier: 0);
            byEntity.World.BlockAccessor.SetBlock(outputBlockId, blockSel.Position);

            if (byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)
            {
                slot.Itemstack.Item?.DamageItem(byEntity.World, byEntity, slot);
            }
        }
    }
}
