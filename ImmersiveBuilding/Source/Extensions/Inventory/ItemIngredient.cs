using Vintagestory.API.Common;

namespace ImmersiveBuilding.Source.Extensions.Inventory;

public class ItemIngredient()
{
    public required EnumItemClass Type { get; set; }

    public required AssetLocation Code { get; set; }

    public required int Quantity { get; set; }

    public ItemStack? ResolvedItemStack { get; set; }

    public ItemIngredient Clone()
    {
        return new()
        {
            Type = Type,
            Code = Code,
            Quantity = Quantity,
            ResolvedItemStack = ResolvedItemStack,
        };
    }

    public void Resolve(IWorldAccessor accessor)
    {
        CollectibleObject? resolvedCollectible = Type == EnumItemClass.Block ? accessor.GetBlock(Code) : accessor.GetItem(Code);

        if (resolvedCollectible is null)
        {
            accessor.Logger.Warning(
                "Unable to resolve recipe ingredient by code {0}, {1} not found.",
                Code.ToString(),
                Type.ToString().ToLowerInvariant()
            );
            return;
        }

        ResolvedItemStack = new(resolvedCollectible, Quantity);
    }
}
