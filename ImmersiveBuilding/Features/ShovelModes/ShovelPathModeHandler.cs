using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace ImmersiveBuilding.Features.ShovelModes;

internal class ShovelPathModeHandler : IModeHandler
{
    private static readonly string[] replaceablePathBlockPatterns = new string[]
    {
        "soil-*",
        "forestfloor-*",
        "sand-*",
        "gravel-*",
    };
    private readonly int[] replaceablePathBlockIds;
    private readonly int stonePathId;
    private readonly int stonePathSlabId;
    private readonly int[] stonePathStairIds;

    public ShovelPathModeHandler(ICoreAPI api)
    {
        List<int> blockIdsFound = new();
        foreach (string replaceableBlock in replaceablePathBlockPatterns)
        {
            foreach (Block searchBlock in api.World.SearchBlocks(new AssetLocation(replaceableBlock)))
            {
                blockIdsFound.Add(searchBlock.BlockId);
            }
        }
        replaceablePathBlockIds = blockIdsFound.ToArray();

        stonePathId = api.World.GetBlock(new AssetLocation("stonepath-free"))?.BlockId ?? -1;
        stonePathSlabId = api.World.GetBlock(new AssetLocation("game:stonepathslab-free"))?.BlockId ?? -1;
        stonePathStairIds = new int[]
        {
            api.World.GetBlock(new AssetLocation("stonepathstairs-up-south-free"))?.BlockId ?? -1,
            api.World.GetBlock(new AssetLocation("stonepathstairs-up-east-free"))?.BlockId ?? -1,
            api.World.GetBlock(new AssetLocation("stonepathstairs-up-north-free"))?.BlockId ?? -1,
            api.World.GetBlock(new AssetLocation("stonepathstairs-up-west-free"))?.BlockId ?? -1,
        };
    }

    public void HandleStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
    {
        IPlayer? byPlayer = (byEntity as EntityPlayer)?.Player;
        if (byPlayer == null)
        {
            return;
        }

        // Maybe add additional check or whitelist
        // Select block underneath if looking at grass and other plants
        if (
            byEntity.World.BlockAccessor.IsValidPos(blockSel.Position.DownCopy())
            && byEntity.World.BlockAccessor.GetBlock(blockSel.Position).BlockMaterial == EnumBlockMaterial.Plant
        )
        {
            blockSel.Position.Down();
        }

        Block block = byEntity.World.BlockAccessor.GetBlock(blockSel.Position);
        byEntity.Api.Logger.Debug("Shovel interacted with {0}", block.Code);
        if (replaceablePathBlockIds.Contains(block.Id))
        {
            ReplaceSoilWithPath(blockSel, byEntity, slot, byPlayer);
        }
        else if (byEntity.Controls.ShiftKey && block.Id == stonePathId)
        {
            ReplacePathWithStairs(byEntity.Api, blockSel, byPlayer);
        }
        else if (byEntity.Controls.ShiftKey && stonePathStairIds.Contains(block.Id))
        {
            ReplaceStairsWithSlab(byEntity.Api, blockSel, byPlayer);
        }
    }

    private void ReplaceSoilWithPath(BlockSelection blockSel, EntityAgent byEntity, ItemSlot slot, IPlayer byPlayer)
    {
        byEntity.World.PlaySoundAt(
            AssetLocation.Create("sounds/block/dirt"),
            blockSel.Position.X,
            blockSel.Position.Y,
            blockSel.Position.Z,
            byPlayer
        );

        slot.Itemstack.Item?.DamageItem(byEntity.World, byEntity, slot);
        byEntity.World.BlockAccessor.BreakBlock(blockSel.Position, byPlayer, dropQuantityMultiplier: 0);
        byEntity.World.BlockAccessor.SetBlock(stonePathId, blockSel.Position);
    }

    private void ReplacePathWithStairs(ICoreAPI api, BlockSelection blockSel, IPlayer byPlayer)
    {
        api.World.PlaySoundAt(
           AssetLocation.Create("sounds/block/gravel"),
           blockSel.Position.X,
           blockSel.Position.Y,
           blockSel.Position.Z,
           byPlayer
       );

        float yaw = byPlayer.Entity.Pos.Yaw * GameMath.RAD2DEG;
        int compasDirection = GameMath.Mod((int)Math.Round(yaw / 90f), 4);
        api.World.BlockAccessor.BreakBlock(blockSel.Position, byPlayer, dropQuantityMultiplier: 0);
        api.World.BlockAccessor.SetBlock(stonePathStairIds[compasDirection], blockSel.Position);
    }

    private void ReplaceStairsWithSlab(ICoreAPI api, BlockSelection blockSel, IPlayer byPlayer)
    {
        api.World.PlaySoundAt(
           AssetLocation.Create("sounds/block/gravel"),
           blockSel.Position.X,
           blockSel.Position.Y,
           blockSel.Position.Z,
           byPlayer
       );

        api.World.BlockAccessor.BreakBlock(blockSel.Position, byPlayer, dropQuantityMultiplier: 0);
        api.World.BlockAccessor.SetBlock(stonePathSlabId, blockSel.Position);
    }
}
