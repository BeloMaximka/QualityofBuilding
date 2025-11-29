using System;
using System.Linq;
using Vintagestory.API.Common;

namespace QualityOfBuilding.Source.CollectibleBehaviors.ShovelModes;

public class ReplaceModeHandler(int[] replacableBlockIds, int outputBlockId) : ModeHandlerBase
{
    public override bool HandleStep(
        float secondsUsed,
        ItemSlot slot,
        EntityAgent byEntity,
        BlockSelection blockSel,
        EntitySelection entitySel
    )
    {
        if (secondsUsed < 1f)
        {
            return true;
        }

        IPlayer? byPlayer = (byEntity as EntityPlayer)?.Player;
        if (byPlayer == null)
        {
            return false;
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

        return false;
    }
}
