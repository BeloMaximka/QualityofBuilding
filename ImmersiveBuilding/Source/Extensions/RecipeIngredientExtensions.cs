using ImmersiveBuilding.Source.Extensions.Inventory;
using ImmersiveBuilding.Source.Recipes;
using System.Linq;
using Vintagestory.API.Common;

namespace ImmersiveBuilding.Source.Extensions;

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
