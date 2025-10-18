using QualityOfBuilding.Source.Utils;
using Newtonsoft.Json.Linq;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace QualityOfBuilding.Source.Recipes;

public class SkillModeRecipeOutput : IByteSerializable
{
    public EnumItemClass Type { get; set; }

    public required AssetLocation Code { get; set; }

    public JToken? Attributes { get; set; }

    public ItemStack? ResolvedItemStack { get; set; }

    public void ResolveItemStack(IWorldAccessor resolver)
    {
        if (ResolvedItemStack is not null)
        {
            return;
        }

        CollectibleObject? collectible = resolver.GetItem(Code);
        collectible ??= resolver.GetBlock(Code);

        if (collectible is null)
        {
            resolver.Logger.Warning("Unable to resolve building recipe output for {0}, no blocks or items found!", Code);
            return;
        }

        ResolvedItemStack = new ItemStack(collectible);

        if (Attributes is not null && new JsonObject(Attributes).ToAttribute() is ITreeAttribute treeAttribute)
        {
            ResolvedItemStack.Attributes.MergeTree(treeAttribute.ConvertLongsToInts());
        }
    }

    public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
    {
        Type = (EnumItemClass)reader.ReadInt32();
        Code = new(reader.ReadString());
        Attributes = reader.ReadNullableJToken();
    }

    public void ToBytes(BinaryWriter writer)
    {
        writer.Write((int)Type);
        writer.Write(Code.ToString());
        writer.WriteNullable(Attributes);
    }
}
