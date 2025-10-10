using ImmersiveBuilding.Source.Utils.Inventory;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace ImmersiveBuilding.Source.CollectibleBehaviors.BuildingModes;

public class BuildingModeHandler(ICoreAPI api) : IModeHandler
{
    public required IReadOnlyCollection<ItemIngredient> Ingredients { get; init; }

    public required ItemStack? Output { get; init; }

    public void HandleStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
    {
        IPlayer? byPlayer = (byEntity as EntityPlayer)?.Player;
        if (byPlayer == null || Output is null)
        {
            return;
        }

        BlockSelection newBlockSelection = blockSel;
        if (!api.World.BlockAccessor.GetBlock(blockSel.Position).IsReplacableBy(Output.Block))
        {
            newBlockSelection = blockSel.AddPosCopy(blockSel.Face.Normali);
        }

        string resultCode = "success";
        Output.Block.CanPlaceBlock(api.World, byPlayer, newBlockSelection, ref resultCode);
        if (resultCode != "success")
        {
            if (byPlayer is IServerPlayer serverPlayer)
            {
                serverPlayer.SendIngameError(resultCode, Lang.Get($"placefailure-{resultCode}"));
            }
            return;
        }

        if (!byPlayer.TryTakeItems(Ingredients))
        {
            return;
        }

        // TryPlaceBlock instead of DoPlaceBlock because some blocks like BlockFence don't have DoPlaceBlock override
        Output.Block.TryPlaceBlock(api.World, byPlayer, Output, newBlockSelection, ref resultCode);
        UpdateNeighbours(newBlockSelection);
        api.World.PlaySoundAt(Output.Block.Sounds.Place, blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, byPlayer);
    }

    private void UpdateNeighbours(BlockSelection blockSelection) // Some manual stuff to make client update instant
    {
        BlockPos tempPos = blockSelection.Position.NorthCopy();
        api.World.BlockAccessor.GetBlock(tempPos).OnNeighbourBlockChange(api.World, tempPos, blockSelection.Position);
        tempPos.Set(blockSelection.Position).South();
        api.World.BlockAccessor.GetBlock(tempPos).OnNeighbourBlockChange(api.World, tempPos, blockSelection.Position);
        tempPos.Set(blockSelection.Position).West();
        api.World.BlockAccessor.GetBlock(tempPos).OnNeighbourBlockChange(api.World, tempPos, blockSelection.Position);
        tempPos.Set(blockSelection.Position).East();
        api.World.BlockAccessor.GetBlock(tempPos).OnNeighbourBlockChange(api.World, tempPos, blockSelection.Position);
    }
}
