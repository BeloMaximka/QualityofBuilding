using QualityOfBuilding.Source.Utils.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace QualityOfBuilding.Source.CollectibleBehaviors.ShovelModes;

public enum ShovelPathReplacementMode
{
    None,
    SoilWthBlock,
    SoilSlab,
    BlockWithStairs,
    StairWithSlab,
}

public class ShovelPathModeHandler : ModeHandlerBase
{
    private readonly int[] replacableSlabIds;
    private readonly int[] replacableBlockIds;
    private readonly NsweBlockIds stonePathStairIds;
    private readonly ItemIngredient[][] recipes;
    private readonly Block path;
    private readonly Block pathSlab;

    private readonly BlockSelection tmpSel = new(new(Dimensions.NormalWorld), BlockFacing.NORTH, null);

    public ShovelPathModeHandler(ICoreAPI api, Block path, Block pathSlab, Block[] pathStairs)
    {
        string[] replacableBlocks = ["soil-*", "forestfloor-*", "sand-*", "gravel-*"];
        List<int> blockIdsFound = [];
        foreach (string replaceableBlock in replacableBlocks)
        {
            foreach (Block searchBlock in api.World.SearchBlocks(new AssetLocation(replaceableBlock)))
            {
                blockIdsFound.Add(searchBlock.BlockId);
            }
        }
        replacableBlockIds = [.. blockIdsFound];

        string[] replacableSlabs = ["terrainslabs:soil-*", "terrainslabs:forestfloor-*", "terrainslabs:sand-*", "terrainslabs:gravel-*"];
        List<int> slabIdsFound = [];

        foreach (string replaceableSlab in replacableSlabs)
        {
            foreach (Block slab in api.World.SearchBlocks(new AssetLocation(replaceableSlab)))
            {
                slabIdsFound.Add(slab.BlockId);
            }
        }
        replacableSlabIds = [.. slabIdsFound];

        this.path = path;
        this.pathSlab = pathSlab;
        stonePathStairIds = new(pathStairs[0].Id, pathStairs[1].Id, pathStairs[2].Id, pathStairs[3].Id);

        recipes =
        [
            [
                new()
                {
                    Type = EnumItemClass.Block,
                    Code = path.Code,
                    Quantity = 1,
                },
            ],
            [
                new()
                {
                    Type = EnumItemClass.Item,
                    Code = "stone-*",
                    Quantity = GetStoneCount(api),
                    TranslatedName = Lang.Get("any-stone"),
                },
            ],
        ];
    }

    public override bool HandleStep(
        float secondsUsed,
        ItemSlot slot,
        EntityAgent byEntity,
        BlockSelection blockSel,
        EntitySelection entitySel
    )
    {
        if (byEntity is not EntityPlayer { Player: IPlayer byPlayer } || byPlayer.CurrentBlockSelection.Block is null)
        {
            return false;
        }

        tmpSel.Position.Set(blockSel.Position);
        tmpSel.Face = blockSel.Face;
        tmpSel.Block = byPlayer.CurrentBlockSelection.Block;
        AdjustPosition(tmpSel, byEntity.World.BlockAccessor);

        ShovelPathReplacementMode mode = GetReplacementMode(tmpSel.Block.Id, byEntity.Controls);
        if (
            mode == ShovelPathReplacementMode.None
            || !slot.Itemstack.Item.MiningSpeed.TryGetValue(tmpSel.Block.BlockMaterial, out float miningSpeed)
        )
        {
            return false;
        }

        byEntity.World.BlockAccessor.DamageBlock(tmpSel.Position, blockSel.Face, miningSpeed * 1f / tmpSel.Block.Resistance / 30);
        return miningSpeed * secondsUsed < tmpSel.Block.Resistance;
    }

    public override void HandleStop(
        float secondsUsed,
        ItemSlot slot,
        EntityAgent byEntity,
        BlockSelection blockSel,
        EntitySelection entitySel
    )
    {
        if (
            byEntity.Api.Side == EnumAppSide.Client
            || byEntity is not EntityPlayer { Player: IPlayer byPlayer }
            || byPlayer.CurrentBlockSelection.Block is null
        )
        {
            return;
        }

        tmpSel.Position.Set(blockSel.Position);
        tmpSel.Face = blockSel.Face;
        tmpSel.Block = byPlayer.CurrentBlockSelection.Block;
        AdjustPosition(tmpSel, byEntity.World.BlockAccessor);

        float latencyBuffer = 0.2f;
        ShovelPathReplacementMode mode = GetReplacementMode(tmpSel.Block.Id, byEntity.Controls);
        if (
            mode == ShovelPathReplacementMode.None
            || !slot.Itemstack.Item.MiningSpeed.TryGetValue(tmpSel.Block.BlockMaterial, out float miningSpeed)
            || miningSpeed * secondsUsed < tmpSel.Block.Resistance - latencyBuffer
        )
        {
            return;
        }

        switch (mode)
        {
            case ShovelPathReplacementMode.SoilWthBlock:
                {
                    int recipeIndex = -1;
                    if (byPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative || byPlayer.TryTakeItems(recipes, out recipeIndex))
                    {
                        ReplaceBlock(tmpSel.Position, byPlayer, path, recipeIndex == 0);
                        DamageShovel(byPlayer, byEntity, slot);
                    }
                }
                break;

            case ShovelPathReplacementMode.SoilSlab:
                {
                    int recipeIndex = -1;
                    if (byPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative || byPlayer.TryTakeItems(recipes, out recipeIndex))
                    {
                        ReplaceBlock(tmpSel.Position, byPlayer, pathSlab, recipeIndex == 0);
                        DamageShovel(byPlayer, byEntity, slot);
                    }
                }
                break;

            case ShovelPathReplacementMode.BlockWithStairs:
                ReplaceBlock(
                    tmpSel.Position,
                    byPlayer,
                    byEntity.World.Blocks[stonePathStairIds.GetCorrectBlockOrientationVariant(byPlayer, blockSel)]
                );
                break;

            case ShovelPathReplacementMode.StairWithSlab:
                ReplaceBlock(tmpSel.Position, byPlayer, pathSlab, false, true);
                break;
        }
    }

    private void AdjustPosition(BlockSelection blockSel, IBlockAccessor accessor)
    {
        // Select block underneath if looking at grass, plants and other small blocks
        if (blockSel.Block.IsReplacableBy(path) || blockSel.Block.BlockMaterial == EnumBlockMaterial.Plant)
        {
            blockSel.Position.Down();
            blockSel.Block = accessor.GetBlock(blockSel.Position);
        }
    }

    private ShovelPathReplacementMode GetReplacementMode(int blockId, EntityControls controls)
    {
        if (replacableBlockIds.Contains(blockId))
        {
            return ShovelPathReplacementMode.SoilWthBlock;
        }
        else if (replacableSlabIds.Contains(blockId))
        {
            return ShovelPathReplacementMode.SoilSlab;
        }
        else if (controls.ShiftKey && blockId == path.Id)
        {
            return ShovelPathReplacementMode.BlockWithStairs;
        }
        else if (controls.ShiftKey && stonePathStairIds.Contains(blockId))
        {
            return ShovelPathReplacementMode.StairWithSlab;
        }

        return ShovelPathReplacementMode.None;
    }

    private static void ReplaceBlock(BlockPos pos, IPlayer byPlayer, Block output, bool shouldDrop = false, bool dropFix = false)
    {
        byPlayer.Entity.World.PlaySoundAt(output.Sounds.Place, pos.X, pos.Y, pos.Z);

        if (dropFix)
        {
            // Workaround because I couldn't remove drop from stairs
            byPlayer.Entity.World.BlockAccessor.SetBlock(output.Id, pos);
        }
        byPlayer.Entity.World.BlockAccessor.BreakBlock(pos, byPlayer, dropQuantityMultiplier: shouldDrop ? 1 : 0);
        byPlayer.Entity.World.BlockAccessor.SetBlock(output.Id, pos);
    }

    private int GetStoneCount(ICoreAPI api)
    {
        GridRecipe? recipe = api.World.GridRecipes.FirstOrDefault(recipe => recipe.Output.Code == path.Code);
        if (recipe is null)
        {
            return 4;
        }

        return recipe
            .resolvedIngredients.Where(ingredient => ingredient.Code.Path.StartsWith("stone"))
            .Sum(ingredient => ingredient.Quantity);
    }

    private static void DamageShovel(IPlayer player, EntityAgent byEntity, ItemSlot slot)
    {
        if (player.WorldData.CurrentGameMode != EnumGameMode.Creative)
        {
            slot.Itemstack.Item?.DamageItem(byEntity.World, byEntity, slot);
        }
    }
}
