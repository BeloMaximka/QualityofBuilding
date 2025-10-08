using ImmersiveBuilding.Source.Utils.Inventory;
using System.Collections.Generic;
using Vintagestory.API.Common;

namespace ImmersiveBuilding.Source.CollectibleBehaviors.BuildingModes;

public class BuildingModeContext
{
    public ItemStack? Output { get; set; }

    public IEnumerable<ItemIngredient> Ingredients { get; set; } = [];
}
