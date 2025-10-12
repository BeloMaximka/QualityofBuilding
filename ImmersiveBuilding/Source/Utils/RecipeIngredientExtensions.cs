using ImmersiveBuilding.Source.Recipes;
using ImmersiveBuilding.Source.Utils.Inventory;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

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

                if (ingredient.TranslationCode is not null)
                {
                    itemIngredient.TranslatedName = Lang.Get(ingredient.TranslationCode);
                }
                else
                {
                    itemIngredient.Resolve(accessor);
                }

                return itemIngredient;
            }),
        ];
}
