using ImmersiveBuilding.Source.Extensions;
using ImmersiveBuilding.Source.Extensions.Inventory;
using ImmersiveBuilding.Source.Recipes;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace ImmersiveBuilding.Source.CollectibleBehaviors.BuildingModes;

public class BuildingModeHandler(ICoreAPI api, SkillModeBuildingRecipe recipe, string wildcardValue) : IModeHandler
{
    public readonly IReadOnlyCollection<ItemIngredient> Ingredients = recipe.GetItemIngredients(api.World, wildcardValue);

    public Block? Block { get; private set; } = api.World.GetBlock(recipe.ResolveSubstitute(recipe.Output.Code, wildcardValue));

    public void HandleStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
    {
        IPlayer? byPlayer = (byEntity as EntityPlayer)?.Player;
        if (byPlayer == null || Block is null)
        {
            return;
        }

        BlockSelection newBlockSelection = blockSel;
        if (!api.World.BlockAccessor.GetBlock(blockSel.Position).IsReplacableBy(Block))
        {
            newBlockSelection = blockSel.AddPosCopy(blockSel.Face.Normali);
        }

        string resultCode = "success";
        Block.CanPlaceBlock(api.World, byPlayer, newBlockSelection, ref resultCode);
        if (resultCode != "success")
        {
            return;
        }

        if (!byPlayer.TryTakeItems(Ingredients))
        {
            return;
        }

        // TryPlaceBlock instead of DoPlaceBlock because some blocks like BlockFence don't have DoPlaceBlock override
        Block.TryPlaceBlock(api.World, byPlayer, slot.Itemstack, newBlockSelection, ref resultCode);
        UpdateNeighbours(newBlockSelection);
        api.World.PlaySoundAt(Block.Sounds.Place, blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, byPlayer);
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
