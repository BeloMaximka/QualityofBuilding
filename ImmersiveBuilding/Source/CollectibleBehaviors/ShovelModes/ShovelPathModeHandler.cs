using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace ImmersiveBuilding.Source.CollectibleBehaviors.ShovelModes;

public class ShovelPathModeHandler : IModeHandler
{
    private static readonly string[] replaceablePathBlockPatterns = ["soil-*", "forestfloor-*", "sand-*", "gravel-*"];
    private readonly int[] replaceablePathBlockIds;
    private readonly Block? stonePath;
    private readonly int stonePathSlabId;
    private readonly int[] stonePathStairIds;

    public ShovelPathModeHandler(ICoreAPI api)
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

        const string stonePathCode = "stonepath-free";
        stonePath = api.World.GetBlock(new AssetLocation(stonePathCode));
        stonePathSlabId = api.World.GetBlock(new AssetLocation("game:stonepathslab-free"))?.BlockId ?? -1;
        stonePathStairIds =
        [
            api.World.GetBlock(new AssetLocation("stonepathstairs-up-south-free"))?.BlockId ?? -1,
            api.World.GetBlock(new AssetLocation("stonepathstairs-up-east-free"))?.BlockId ?? -1,
            api.World.GetBlock(new AssetLocation("stonepathstairs-up-north-free"))?.BlockId ?? -1,
            api.World.GetBlock(new AssetLocation("stonepathstairs-up-west-free"))?.BlockId ?? -1,
        ];
    }

    public void HandleStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
    {
        IPlayer? byPlayer = (byEntity as EntityPlayer)?.Player;
        if (byPlayer == null)
        {
            return;
        }
        if (stonePath is null)
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
        byEntity.Api.Logger.Debug("[ImmersiveBuilding] Shovel interacted with {0}", block.Code);
        if (replaceablePathBlockIds.Contains(block.Id))
        {
            (bool isSuccessful, bool shouldDrop) = TryTakeMaterialsForPath(byPlayer);
            if (isSuccessful)
            {
                ReplaceSoilWithPath(blockSel, byEntity, slot, byPlayer, shouldDrop, stonePath.Id);
            }
            else if (byEntity is EntityPlayer { Player: IServerPlayer player })
            {
                player.SendIngameError("nomatsforpath", "You don't have enough materials");
            }
        }
        else if (byEntity.Controls.ShiftKey && block.Id == stonePath.Id)
        {
            ReplacePathWithStairs(byEntity.Api, blockSel, byPlayer);
        }
        else if (byEntity.Controls.ShiftKey && stonePathStairIds.Contains(block.Id))
        {
            ReplaceStairsWithSlab(byEntity.Api, blockSel, byPlayer);
        }
    }

    private static void ReplaceSoilWithPath(
        BlockSelection blockSel,
        EntityAgent byEntity,
        ItemSlot slot,
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

        slot.Itemstack.Item?.DamageItem(byEntity.World, byEntity, slot);
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

        float yaw = byPlayer.Entity.Pos.Yaw * GameMath.RAD2DEG;
        int compasDirection = GameMath.Mod((int)Math.Round(yaw / 90f), 4); // S E N W
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

        // Workaround because I couldn't remove drop from stairs
        api.World.BlockAccessor.SetBlock(stonePathSlabId, blockSel.Position);
        api.World.BlockAccessor.BreakBlock(blockSel.Position, byPlayer, dropQuantityMultiplier: 1);
        api.World.BlockAccessor.SetBlock(stonePathSlabId, blockSel.Position);
    }

    private (bool isSuccessful, bool shouldDrop) TryTakeMaterialsForPath(IPlayer player)
    {
        if (player.WorldData.CurrentGameMode == EnumGameMode.Creative)
        {
            return (true, false);
        }

        return TryTakeMaterialsFromInventories(
            [player.InventoryManager.GetHotbarInventory(), player.InventoryManager.GetOwnInventory("backpack")]
        );
    }

    // Priority:
    // 1. Path blocks
    // 2. Path stairs
    // 3. Path slabs
    // 4. Stones
    private (bool isSuccessful, bool shouldDrop) TryTakeMaterialsFromInventories(IInventory?[] inventories)
    {
        ItemSlot? bestSlot = null;
        int bestPriority = int.MaxValue;
        int takeOutSize = 0;
        bool shouldDrop = false;
        foreach (var inventory in inventories)
        {
            if (inventory is null)
            {
                continue;
            }

            foreach (ItemSlot slot in inventory)
            {
                if (!slot.CanTake())
                {
                    continue;
                }

                // Stone path block (highest priority)
                if (stonePath is not null && slot.Itemstack.Block?.Id == stonePath.Id)
                {
                    slot.TakeOut(1);
                    return (true, true);
                }

                // Stone path stair (priority 2)
                if (bestPriority > 2 && slot.Itemstack.Block?.Id is not null && stonePathStairIds.Contains(slot.Itemstack.Block.Id))
                {
                    bestSlot = slot;
                    bestPriority = 2;
                    takeOutSize = 1;
                    shouldDrop = true;
                    continue;
                }

                // Stone path slab (priority 3)
                if (bestPriority > 3 && slot.Itemstack.Block?.Id == stonePathSlabId && slot.Itemstack.StackSize >= 2)
                {
                    bestSlot = slot;
                    bestPriority = 3;
                    takeOutSize = 2;
                    shouldDrop = true;
                    continue;
                }

                // Any suitable stone (least priority)
                if (
                    bestPriority > 4
                    && slot.Itemstack.Item?.Code is not null
                    && Regex.IsMatch(slot.Itemstack.Item?.Code, "\\w+:stone-")
                    && slot.Itemstack.StackSize >= 4
                )
                {
                    bestSlot = slot;
                    bestPriority = 4;
                    takeOutSize = 4;
                    continue;
                }
            }
        }

        if (bestSlot is not null)
        {
            bestSlot.TakeOut(takeOutSize);
            return (true, shouldDrop);
        }
        return (false, false);
    }
}
