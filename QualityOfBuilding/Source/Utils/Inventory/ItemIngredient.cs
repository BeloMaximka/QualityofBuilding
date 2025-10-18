using Vintagestory.API.Common;

namespace QualityOfBuilding.Source.Utils.Inventory;

public class ItemIngredient()
{
    public required EnumItemClass Type { get; set; }

    public required AssetLocation Code { get; set; }

    public required int Quantity { get; set; }

    public string? TranslatedName { get; set; }

    public ItemIngredient Clone()
    {
        return new()
        {
            Type = Type,
            Code = Code,
            Quantity = Quantity,
            TranslatedName = TranslatedName,
        };
    }
}
