using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace ImmersiveBuilding.Extensions;

public class ItemIngredient()
{
    public required AssetLocation Code { get; set; }
    public required int Quantity { get; set; }
}

public static class InventoryExtensions
{
    private sealed record ItemSlotToTakeFrom(ItemSlot Slot, int Quantity);

    public static bool TryTakeItems(this IPlayer player, ICollection<ItemIngredient> ingredients)
    {
        if (player.WorldData.CurrentGameMode == EnumGameMode.Creative)
        {
            return true;
        }

        IEnumerable<IInventory> inventories = new IInventory[]
        {
            player.InventoryManager.GetOwnInventory("backpack"),
            player.InventoryManager.GetHotbarInventory(),
        }.Where(inventory => inventory is not null);
        if (!inventories.TryTakeItems(ingredients))
        {
            if (player is IServerPlayer serverPlayer)
            {
                serverPlayer.SendIngameError("nomatsforpath", "You don't have enough materials"); // TODO: Add info about missing materials
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
    public static bool TryTakeItems(this IEnumerable<IInventory> inventories, ICollection<ItemIngredient> ingredients)
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
        ICollection<ItemIngredient> ingredients,
        ICollection<ItemSlotToTakeFrom> slotsToTakeFrom
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
