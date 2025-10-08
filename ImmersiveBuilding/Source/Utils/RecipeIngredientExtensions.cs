using ImmersiveBuilding.Source.Recipes;
using ImmersiveBuilding.Source.Utils.Inventory;
using System.Linq;
using Vintagestory.API.Common;

namespace ImmersiveBuilding.Source.Utils;

public static class RecipeIngredientExtensions
{
    public static ItemIngredient[] GetItemIngredients(this SkillModeBuildingRecipe recipe, IWorldAccessor accessor, string wildcardValue) =>
        [
            .. recipe.Ingredients.Select(ingredient =>
            {
                ItemIngredient itemIngredient = new()
                {
                    Type = ingredient.Type,
                    Code = recipe.ResolveSubstitute(ingredient.Code, wildcardValue),
                    Quantity = ingredient.Quantity,
                };
                itemIngredient.Resolve(accessor);
                return itemIngredient;
            }),
        ];
}
