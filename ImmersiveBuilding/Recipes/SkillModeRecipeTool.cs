using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace ImmersiveBuilding.Recipes;

public class SkillModeRecipeTool : IByteSerializable
{
    public EnumItemClass Type { get; set; }

    public AssetLocation Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string[] AllowVariants { get; set; } = [];

    public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
    {
        Type = (EnumItemClass)reader.ReadInt32();
        Code = new(reader.ReadString());
        Name = reader.ReadString();
        AllowVariants = reader.ReadStringArray();
    }

    public void ToBytes(BinaryWriter writer)
    {
        writer.Write((int)Type);
        writer.Write(Code.ToString());
        writer.Write(Name);
        writer.WriteArray(AllowVariants);
    }
}
