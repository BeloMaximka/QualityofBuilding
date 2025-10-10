using System.Collections.Generic;
using System.Linq;

namespace ImmersiveBuilding.Source.Utils.Inventory;

public static class ItemIngredientExtensions
{
    public static string GetMaterialsString(this IEnumerable<ItemIngredient> ingredients) =>
        string.Join(
            ", ",
            ingredients.Select(ingredient =>
                $"{ingredient.Quantity}x {ingredient.TranslatedName ?? ingredient.ResolvedItemStack?.GetName().ToLower() ?? ingredient.Code.ToString()}"
            )
        );
}
