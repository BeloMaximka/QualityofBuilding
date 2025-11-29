using QualityOfBuilding.Source.Utils.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
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

public class ShovelPathModeHandler : ShovelModeHandlerBase
{
    private readonly int[] replacableSlabIds;
    private readonly int[] replacableBlockIds;
    private readonly NsweBlockIds stonePathStairIds;
    private readonly ItemIngredient[][] recipes;
    private readonly Block path;
    private readonly Block pathSlab;

    public ShovelPathModeHandler(ICoreAPI api, Block path, Block pathSlab, Block[] pathStairs)
        : base(path)
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
        UpdateSelection(byPlayer, blockSel, tmpSel);

        ShovelPathReplacementMode mode = GetReplacementMode(tmpSel.Block.Id, byEntity.Controls);
        if (mode == ShovelPathReplacementMode.None || HasNotMinedEnough(tmpSel.Block, secondsUsed, slot))
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
                        DamageShovel(byPlayer, slot);
                    }
                }
                break;

            case ShovelPathReplacementMode.SoilSlab:
                {
                    int recipeIndex = -1;
                    if (byPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative || byPlayer.TryTakeItems(recipes, out recipeIndex))
                    {
                        ReplaceBlock(tmpSel.Position, byPlayer, pathSlab, recipeIndex == 0);
                        DamageShovel(byPlayer, slot);
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

    internal override bool CanHandle(Block block, EntityAgent entity)
    {
        return GetReplacementMode(block.Id, entity.Controls) != ShovelPathReplacementMode.None;
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
}
