using QualityOfBuilding.Source.Utils.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace QualityOfBuilding.Source.CollectibleBehaviors.ShovelModes;

public class ShovelPathModeHandler : IModeHandler
{
    private readonly int[] replacableSlabIds;
    private readonly int[] replacableBlockIds;
    private readonly NsweBlockIds stonePathStairIds;
    private readonly ItemIngredient[][] recipes;
    private readonly Block path;
    private readonly Block pathSlab;

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
            && (selectedBlock.IsReplacableBy(path) || selectedBlock.BlockMaterial == EnumBlockMaterial.Plant)
        )
        {
            blockSel.Position.Down();
        }

        Block block = byEntity.World.BlockAccessor.GetBlock(blockSel.Position);
        if (replacableBlockIds.Contains(block.Id))
        {
            int recipeIndex = -1;
            if (byPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative || byPlayer.TryTakeItems(recipes, out recipeIndex))
            {
                ReplaceBlock(blockSel, byPlayer, path, recipeIndex == 0);
                DamageShovel(byPlayer, byEntity, slot);
            }
        }
        else if (replacableSlabIds.Contains(block.Id))
        {
            int recipeIndex = -1;
            if (byPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative || byPlayer.TryTakeItems(recipes, out recipeIndex))
            {
                ReplaceBlock(blockSel, byPlayer, pathSlab, recipeIndex == 0);
                DamageShovel(byPlayer, byEntity, slot);
            }
        }
        else if (byEntity.Controls.ShiftKey && block.Id == path.Id)
        {
            ReplaceBlock(
                blockSel,
                byPlayer,
                byEntity.World.Blocks[stonePathStairIds.GetCorrectBlockOrientationVariant(byPlayer, blockSel)]
            );
            DamageShovel(byPlayer, byEntity, slot);
        }
        else if (byEntity.Controls.ShiftKey && stonePathStairIds.Contains(block.Id))
        {
            ReplaceStairsWithSlab(blockSel, byPlayer);
            DamageShovel(byPlayer, byEntity, slot);
        }
    }

    private static void ReplaceBlock(BlockSelection blockSel, IPlayer byPlayer, Block output, bool shouldDrop = false)
    {
        byPlayer.Entity.World.PlaySoundAt(output.Sounds.Place, blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, byPlayer);

        byPlayer.Entity.World.BlockAccessor.BreakBlock(blockSel.Position, byPlayer, dropQuantityMultiplier: shouldDrop ? 1 : 0);
        byPlayer.Entity.World.BlockAccessor.SetBlock(output.Id, blockSel.Position);
    }

    private void ReplaceStairsWithSlab(BlockSelection blockSel, IPlayer byPlayer)
    {
        byPlayer.Entity.World.PlaySoundAt(pathSlab.Sounds.Place, blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, byPlayer);

        // Workaround because I couldn't remove drop from stairs
        byPlayer.Entity.World.BlockAccessor.SetBlock(pathSlab.Id, blockSel.Position);
        byPlayer.Entity.World.BlockAccessor.BreakBlock(blockSel.Position, byPlayer, dropQuantityMultiplier: 0);
        byPlayer.Entity.World.BlockAccessor.SetBlock(pathSlab.Id, blockSel.Position);
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
