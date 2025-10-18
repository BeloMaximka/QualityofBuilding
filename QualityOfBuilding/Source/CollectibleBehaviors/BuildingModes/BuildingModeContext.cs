using QualityOfBuilding.Source.Utils.Inventory;
using System.Collections.Generic;
using Vintagestory.API.Common;

namespace QualityOfBuilding.Source.CollectibleBehaviors.BuildingModes;

public class BuildingModeContext
{
    public ItemStack? Output { get; set; }

    public required IModeHandler Handler { get; set; }

    public IReadOnlyCollection<ItemIngredient> Ingredients { get; set; } = [];
}
