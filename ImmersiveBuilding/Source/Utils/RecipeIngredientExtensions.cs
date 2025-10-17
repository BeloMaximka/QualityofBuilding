using ImmersiveBuilding.Source.Recipes;
using ImmersiveBuilding.Source.Utils.Inventory;
using System.Linq;
using Vintagestory.API.Config;

namespace ImmersiveBuilding.Source.Utils;

public static class RecipeIngredientExtensions
{
    public static ItemIngredient[] GetItemIngredients(this SkillModeBuildingRecipe recipe) =>
        [
            .. recipe.Ingredients.Select(ingredient =>
            {
                ItemIngredient itemIngredient = new()
                {
                    Type = ingredient.Type,
                    Code = ingredient.Code,
                    Quantity = ingredient.Quantity,
                };

                if (ingredient.TranslationCode is not null)
                {
                    itemIngredient.TranslatedName = Lang.Get(ingredient.TranslationCode);
                }
                else if (ingredient.ResolvedItemStack is not null)
                {
                    itemIngredient.TranslatedName = ingredient.ResolvedItemStack.GetName();
                }

                return itemIngredient;
            }),
        ];
}
