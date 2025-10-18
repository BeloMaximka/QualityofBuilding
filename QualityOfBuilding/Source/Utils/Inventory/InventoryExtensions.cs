using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace QualityOfBuilding.Source.Utils.Inventory;

public static class InventoryExtensions
{
    private sealed record ItemSlotToTakeFrom(ItemSlot Slot, int Quantity);

    public static bool TryTakeItems(this IPlayer player, IReadOnlyList<IReadOnlyCollection<ItemIngredient>> recipes, out int recipeIndex)
    {
        recipeIndex = -1;
        for (int i = 0; i < recipes.Count; i++)
        {
            bool isSuccessful = player.TryTakeItems(recipes[i], showErrorMessage: i + 1 == recipes.Count);
            if (isSuccessful)
            {
                recipeIndex = i;
                return true;
            }
        }

        return false;
    }

    public static bool TryTakeItems(this IPlayer player, IReadOnlyCollection<ItemIngredient> ingredients, bool showErrorMessage = true)
    {
        List<ItemIngredient> missingMaterials = [.. ingredients.Select(ingredient => ingredient.Clone())];
        if (player.WorldData.CurrentGameMode == EnumGameMode.Creative)
        {
            return true;
        }

        DummyInventory mouseSlotInventory = new(player.Entity.Api);
        mouseSlotInventory[0] = player.InventoryManager.MouseItemSlot;
        IEnumerable<IInventory> inventories = new IInventory[]
        {
            player.InventoryManager.GetOwnInventory("backpack"),
            player.InventoryManager.GetHotbarInventory(),
            mouseSlotInventory,
        }.Where(inventory => inventory is not null);

        if (!inventories.TryTakeItems(missingMaterials))
        {
            if (showErrorMessage && player is IServerPlayer serverPlayer)
            {
                serverPlayer.SendIngameError("nomatsforbuilding", null, missingMaterials.GetMaterialsString());
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
