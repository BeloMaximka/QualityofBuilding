using QualityOfBuilding.Source.Utils.Inventory;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace QualityOfBuilding.Source.CollectibleBehaviors.ShovelModes;

public class ShovelPathModeHandler : IModeHandler
{
    private static readonly string[] replaceablePathBlockPatterns = ["soil-*", "forestfloor-*", "sand-*", "gravel-*"];
    private readonly int[] replaceablePathBlockIds;
    private readonly int stonePathSlabId;
    private readonly NsweBlockIds stonePathStairIds;
    private readonly ItemIngredient[][] recipes;
    private readonly Block stonePath;

    public ShovelPathModeHandler(ICoreAPI api, Block stonePath)
    {
        List<int> blockIdsFound = [];
        foreach (string replaceableBlock in replaceablePathBlockPatterns)
        {
            foreach (Block searchBlock in api.World.SearchBlocks(new AssetLocation(replaceableBlock)))
            {
                blockIdsFound.Add(searchBlock.BlockId);
            }
        }
        replaceablePathBlockIds = [.. blockIdsFound];

        this.stonePath = stonePath;
        stonePathSlabId = api.World.GetBlock(new AssetLocation("game:stonepathslab-free"))?.BlockId ?? -1;
        stonePathStairIds = new(
            api.World.GetBlock(new AssetLocation("stonepathstairs-up-north-free"))?.BlockId ?? -1,
            api.World.GetBlock(new AssetLocation("stonepathstairs-up-south-free"))?.BlockId ?? -1,
            api.World.GetBlock(new AssetLocation("stonepathstairs-up-west-free"))?.BlockId ?? -1,
            api.World.GetBlock(new AssetLocation("stonepathstairs-up-east-free"))?.BlockId ?? -1
        );

        recipes =
        [
            [
                new()
                {
                    Type = EnumItemClass.Block,
                    Code = stonePath.Code,
                    Quantity = 1,
                },
            ],
            [
                new()
                {
                    Type = EnumItemClass.Item,
                    Code = "stone-*",
                    Quantity = 4,
                    TranslatedName = Lang.Get("any-stone"),
                },
            ],
        ];
    }

    public void HandleStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
    {
        IPlayer? byPlayer = (byEntity as EntityPlayer)?.Player;
        if (byPlayer == null)
        {
            return;
        }

        // Select block underneath if looking at grass, plants and other small blocks
        Block selectedBlock = byEntity.World.BlockAccessor.GetBlock(blockSel.Position);
        if (
            byEntity.World.BlockAccessor.IsValidPos(blockSel.Position.DownCopy())
            && (selectedBlock.IsReplacableBy(stonePath) || selectedBlock.BlockMaterial == EnumBlockMaterial.Plant)
        )
        {
            blockSel.Position.Down();
        }

        Block block = byEntity.World.BlockAccessor.GetBlock(blockSel.Position);
        if (replaceablePathBlockIds.Contains(block.Id))
        {
            int recipeIndex = -1;
            if (byPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative || byPlayer.TryTakeItems(recipes, out recipeIndex))
            {
                ReplaceSoilWithPath(blockSel, byEntity, byPlayer, recipeIndex == 0, stonePath.Id);
                DamageShovel(byPlayer, byEntity, slot);
            }
        }
        else if (byEntity.Controls.ShiftKey && block.Id == stonePath.Id)
        {
            ReplacePathWithStairs(byEntity.Api, blockSel, byPlayer);
            DamageShovel(byPlayer, byEntity, slot);
        }
        else if (byEntity.Controls.ShiftKey && stonePathStairIds.Contains(block.Id))
        {
            ReplaceStairsWithSlab(byEntity.Api, blockSel, byPlayer);
            DamageShovel(byPlayer, byEntity, slot);
        }
    }

    private static void ReplaceSoilWithPath(
        BlockSelection blockSel,
        EntityAgent byEntity,
        IPlayer byPlayer,
        bool shouldDrop,
        int stonePathId
    )
    {
        byEntity.World.PlaySoundAt(
            AssetLocation.Create("sounds/block/dirt"),
            blockSel.Position.X,
            blockSel.Position.Y,
            blockSel.Position.Z,
            byPlayer
        );

        byEntity.World.BlockAccessor.BreakBlock(blockSel.Position, byPlayer, dropQuantityMultiplier: shouldDrop ? 1 : 0);
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

        api.World.BlockAccessor.BreakBlock(blockSel.Position, byPlayer, dropQuantityMultiplier: 0);
        api.World.BlockAccessor.SetBlock(stonePathStairIds.GetCorrectBlockOrientationVariant(byPlayer, blockSel), blockSel.Position);
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

        // Workaround because I couldn't remove drop from stairs
        api.World.BlockAccessor.SetBlock(stonePathSlabId, blockSel.Position);
        api.World.BlockAccessor.BreakBlock(blockSel.Position, byPlayer, dropQuantityMultiplier: 0);
        api.World.BlockAccessor.SetBlock(stonePathSlabId, blockSel.Position);
    }

    private static void DamageShovel(IPlayer player, EntityAgent byEntity, ItemSlot slot)
    {
        if (player.WorldData.CurrentGameMode != EnumGameMode.Creative)
        {
            slot.Itemstack.Item?.DamageItem(byEntity.World, byEntity, slot);
        }
    }
}
