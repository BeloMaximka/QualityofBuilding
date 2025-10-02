using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace ImmersiveBuilding.Source.Extensions;

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

public static class InventoryExtensions
{
    private sealed record ItemSlotToTakeFrom(ItemSlot Slot, int Quantity);

    public static bool TryTakeItems(this IPlayer player, IReadOnlyCollection<ItemIngredient> ingredients)
    {
        List<ItemIngredient> missingMaterials = [.. ingredients.Select(ingredient => ingredient.Clone())];
        if (player.WorldData.CurrentGameMode == EnumGameMode.Creative)
        {
            return true;
        }

        IEnumerable<IInventory> inventories = new IInventory[]
        {
            player.InventoryManager.GetOwnInventory("backpack"),
            player.InventoryManager.GetHotbarInventory(),
        }.Where(inventory => inventory is not null);

        if (!inventories.TryTakeItems(missingMaterials))
        {
            string translatedMissingMaterials = string.Join(
                ", ",
                missingMaterials.Select(ingredient =>
                    $"{ingredient.Quantity} {ingredient.ResolvedItemStack?.GetName() ?? ingredient.Code.ToString()}"
                )
            );
            if (player is IServerPlayer serverPlayer)
            {
                serverPlayer.SendIngameError("nomatsforbuilding", null, translatedMissingMaterials);
            }
            return false;
        }

        return true;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Minor Code Smell",
        "S3267:Loops should be simplified with \"LINQ\" expressions",
        Justification = "The foreach is not a pure loop"
    )]
    private static bool TryTakeItems(this IEnumerable<IInventory> inventories, List<ItemIngredient> ingredients)
    {
        List<ItemSlotToTakeFrom> slotsToTakeFrom = [];
        foreach (IInventory inventory in inventories)
        {
            if (inventory.FillSlotsToTakeFrom(ingredients, slotsToTakeFrom))
            {
                break;
            }
        }

        if (ingredients.Count > 0)
        {
            return false;
        }

        foreach (ItemSlotToTakeFrom slotToTakeFrom in slotsToTakeFrom)
        {
            slotToTakeFrom.Slot.TakeOut(slotToTakeFrom.Quantity);
            slotToTakeFrom.Slot.MarkDirty();
        }

        return true;
    }

    private static bool FillSlotsToTakeFrom(
        this IInventory inventory,
        List<ItemIngredient> ingredients,
        List<ItemSlotToTakeFrom> slotsToTakeFrom
    )
    {
        foreach (ItemSlot slot in inventory)
        {
            if (!slot.CanTake())
            {
                continue;
            }

            foreach (var ingredient in ingredients)
            {
                if (slot.Itemstack.StackSize > 0 && WildcardUtil.Match(ingredient.Code, slot.Itemstack.Collectible.Code))
                {
                    int quantityToTake = Math.Min(slot.Itemstack.StackSize, ingredient.Quantity);
                    slotsToTakeFrom.Add(new(slot, quantityToTake));
                    ingredient.Quantity -= quantityToTake;
                    if (ingredient.Quantity <= 0)
                    {
                        ingredients.Remove(ingredient);
                        break;
                    }
                }
            }
        }

        return ingredients.Count == 0;
    }
}
