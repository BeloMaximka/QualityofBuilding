using ImmersiveBuilding.Source.Extensions.Inventory;
using System.Collections.Generic;
using Vintagestory.API.Common;

namespace ImmersiveBuilding.Source.CollectibleBehaviors.BuildingModes;

public class BuildingModeContext
{
    public required ItemStack Output { get; set; }

    public IEnumerable<ItemIngredient> Ingredients { get; set; } = [];
}
