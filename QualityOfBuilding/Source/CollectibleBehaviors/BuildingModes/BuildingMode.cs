using QualityOfBuilding.Source.Utils.Inventory;
using System.Collections.Generic;
using Vintagestory.API.Common;

namespace QualityOfBuilding.Source.CollectibleBehaviors.BuildingModes;

public class BuildingMode
{
    public required AssetLocation Code { get; set; }
    public required string Name { get; set; }
    public required ModeHandlerBase Handler { get; set; }
    public ItemStack? Output { get; set; }
    public ItemSlot? RenderSlot { get; set; }
    public IReadOnlyCollection<ItemIngredient> Ingredients { get; set; } = [];
}
