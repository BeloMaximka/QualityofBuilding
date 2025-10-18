using Newtonsoft.Json;
using System.IO;
using Vintagestory.API.Common;

namespace QualityOfBuilding.Source.Recipes;

public class SkillModeRecipeTool : IByteSerializable
{
    public EnumItemClass Type { get; set; }

    public required AssetLocation Code { get; set; }

    [JsonIgnore]
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
            resolver.Logger.Warning("Unable to resolve building recipe tool for {0}, no blocks or items found!", Code);
            return;
        }
        ResolvedItemStack = new ItemStack(collectible);
    }

    public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
    {
        Type = (EnumItemClass)reader.ReadInt32();
        Code = new(reader.ReadString());
    }

    public void ToBytes(BinaryWriter writer)
    {
        writer.Write((int)Type);
        writer.Write(Code.ToString());
    }
}
