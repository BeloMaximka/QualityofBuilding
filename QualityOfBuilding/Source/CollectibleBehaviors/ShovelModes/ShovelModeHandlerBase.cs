using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace QualityOfBuilding.Source.CollectibleBehaviors.ShovelModes;

public class ShovelModeHandlerBase(Block output) : ModeHandlerBase
{
    internal readonly BlockSelection tmpSel = new(new(Dimensions.NormalWorld), BlockFacing.NORTH, null);

    public override bool HandleStep(
        float secondsUsed,
        ItemSlot slot,
        EntityAgent byEntity,
        BlockSelection blockSel,
        EntitySelection entitySel
    )
    {
        if (blockSel is null || byEntity is not EntityPlayer { Player: IPlayer byPlayer })
        {
            return false;
        }
        UpdateSelection(byPlayer, blockSel, tmpSel);

        if (
            !CanHandle(tmpSel.Block, byEntity)
            || !slot.Itemstack.Item.MiningSpeed.TryGetValue(tmpSel.Block.BlockMaterial, out float miningSpeed)
        )
        {
            return false;
        }

        byEntity.World.BlockAccessor.DamageBlock(tmpSel.Position, blockSel.Face, miningSpeed / (tmpSel.Block.Resistance * 30f));
        return miningSpeed * secondsUsed < tmpSel.Block.Resistance;
    }

    internal bool HasNotMinedEnough(Block block, float secondsUsed, ItemSlot slot)
    {
        float latencyBuffer = 0.2f;
        return slot.Itemstack.Item.MiningSpeed.TryGetValue(tmpSel.Block.BlockMaterial, out float miningSpeed)
            && miningSpeed * secondsUsed < block.Resistance - latencyBuffer;
    }

    internal virtual bool CanHandle(Block block, EntityAgent entity)
    {
        return true;
    }

    internal void ReplaceBlock(BlockPos pos, IPlayer byPlayer, Block output, bool shouldDrop = false, bool dropFix = false)
    {
        if (dropFix)
        {
            // Workaround because I couldn't remove drop from stairs
            byPlayer.Entity.World.BlockAccessor.SetBlock(output.Id, pos);
        }
        byPlayer.Entity.World.BlockAccessor.BreakBlock(pos, null, dropQuantityMultiplier: shouldDrop ? 1 : 0);
        byPlayer.Entity.World.BlockAccessor.SetBlock(output.Id, pos);
        ReplaceBlockAboveIfReplacable(byPlayer, pos);
    }

    internal void ReplaceBlockAboveIfReplacable(IPlayer player, BlockPos pos)
    {
        pos.Up();
        Block block = player.Entity.World.BlockAccessor.GetBlock(pos);
        if (block.Id != 0 && block.IsReplacableBy(output) || block.BlockMaterial == EnumBlockMaterial.Plant)
        {
            player.Entity.World.BlockAccessor.BreakBlock(pos, null);
        }
        pos.Down();
    }

    internal static void DamageShovel(IPlayer player, ItemSlot slot)
    {
        if (player.WorldData.CurrentGameMode != EnumGameMode.Creative)
        {
            slot.Itemstack.Item?.DamageItem(player.Entity.World, player.Entity, slot);
        }
    }

    internal void UpdateSelection(IPlayer player, BlockSelection src, BlockSelection dst)
    {
        dst.Position.Set(src.Position);
        dst.Face = src.Face;
        dst.Block = player.Entity.World.BlockAccessor.GetBlock(dst.Position);

        // Select block underneath if looking at grass, plants and other small blocks
        if (dst.Block.IsReplacableBy(output) || dst.Block.BlockMaterial == EnumBlockMaterial.Plant)
        {
            dst.Position.Down();
            dst.Block = player.Entity.World.BlockAccessor.GetBlock(dst.Position);
        }
    }
}
