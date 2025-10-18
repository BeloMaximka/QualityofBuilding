using System.Collections.Generic;
using System.Linq;

namespace QualityOfBuilding.Source.Utils.Inventory;

public static class ItemIngredientExtensions
{
    public static string GetMaterialsString(this IEnumerable<ItemIngredient> ingredients) =>
        string.Join(
            ", ",
            ingredients.Select(ingredient => $"{ingredient.Quantity}x {ingredient.TranslatedName ?? ingredient.Code.ToString()}")
        );
}
