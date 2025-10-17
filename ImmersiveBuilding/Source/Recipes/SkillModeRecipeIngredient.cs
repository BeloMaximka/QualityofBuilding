using ImmersiveBuilding.Source.Utils;
using System.IO;
using System.Linq;
using Vintagestory.API.Common;

namespace ImmersiveBuilding.Source.Recipes;

public class SkillModeRecipeIngredient : IByteSerializable
{
    public EnumItemClass Type { get; set; }

    public required AssetLocation Code { get; set; }

    public int Quantity { get; set; } = 1;

    public string? TranslationCode { get; set; }

    public ItemStack[] ResolvedItemStacks { get; set; } = [];

    // Maybe we should get rid of ResolvedItemStack in the future to avoid potentials bugs
    public ItemStack? ResolvedItemStack => ResolvedItemStacks.Length > 0 ? ResolvedItemStacks[0] : null;

    public void ResolveItemStack(IWorldAccessor resolver)
    {
        if (ResolvedItemStack is not null)
        {
            return;
        }

        CollectibleObject[] collectibles = resolver.SearchItems(Code);
        if (collectibles.Length == 0)
        {
            collectibles = resolver.SearchBlocks(Code);
        }

        if (collectibles.Length == 0)
        {
            resolver.Logger.Warning("Unable to resolve building recipe ingredient for {0}, no blocks or items found!", Code);
            return;
        }
        ResolvedItemStacks = [.. collectibles.Select(collectible => new ItemStack(collectible, Quantity))];
    }

    public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
    {
        Type = (EnumItemClass)reader.ReadInt32();
        Code = new(reader.ReadString());
        Quantity = reader.ReadInt32();
        TranslationCode = reader.ReadNullableString();
        ResolvedItemStacks = [];
    }

    public void ToBytes(BinaryWriter writer)
    {
        writer.Write((int)Type);
        writer.Write(Code.ToString());
        writer.Write(Quantity);
        writer.WriteNullable(TranslationCode);
    }
}
