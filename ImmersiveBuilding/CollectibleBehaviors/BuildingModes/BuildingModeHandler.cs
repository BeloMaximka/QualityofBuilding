using ImmersiveBuilding.Extensions;
using ImmersiveBuilding.Recipes;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;

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

        block.DoPlaceBlock(api.World, byPlayer, newBlockSelection, slot.Itemstack);
        api.World.PlaySoundAt(block.Sounds.Place, blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, byPlayer);
    }
}
