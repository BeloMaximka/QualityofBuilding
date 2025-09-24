using ImmersiveBuilding.Extensions;
using ImmersiveBuilding.Recipes;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace ImmersiveBuilding.CollectibleBehaviors.BuildingModes;

public class BuildingModeHandler(ICoreAPI api, SkillModeBuildingRecipe recipe, string wildcardValue) : IModeHandler
{
    private readonly Block? block = api.World.GetBlock(recipe.ResolveSubstitute(recipe.Output.Code, wildcardValue));

    public void HandleStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
    {
        IPlayer? byPlayer = (byEntity as EntityPlayer)?.Player;
        if (byPlayer == null || block is null)
        {
            return;
        }

        BlockSelection newBlockSelection = blockSel;
        if (!api.World.BlockAccessor.GetBlock(blockSel.Position).IsReplacableBy(block))
        {
            newBlockSelection = blockSel.AddPosCopy(blockSel.Face.Normali);
        }

        string resultCode = "success";
        block.CanPlaceBlock(api.World, byPlayer, newBlockSelection, ref resultCode);
        if (resultCode != "success")
        {
            return;
        }

        List<ItemIngredient> ingredients =
        [
            .. recipe.Ingredients.Select(ingredient => new ItemIngredient()
            {
                Code = recipe.ResolveSubstitute(ingredient.Code, wildcardValue),
                Quantity = ingredient.Quantity,
            }),
        ];
        if (!byPlayer.TryTakeItems(ingredients))
        {
            return;
        }

        // TryPlaceBlock instead of DoPlaceBlock because some blocks like BlockFence don't have DoPlaceBlock override
        block.TryPlaceBlock(api.World, byPlayer, slot.Itemstack, newBlockSelection, ref resultCode);
        UpdateNeighbours(newBlockSelection);
        api.World.PlaySoundAt(block.Sounds.Place, blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, byPlayer);
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
