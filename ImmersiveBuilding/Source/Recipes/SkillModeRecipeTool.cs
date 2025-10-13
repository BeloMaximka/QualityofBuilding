using ImmersiveBuilding.Source.Utils;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace ImmersiveBuilding.Source.Recipes;

public class SkillModeRecipeTool : IByteSerializable
{
    public EnumItemClass Type { get; set; }

    public required AssetLocation Code { get; set; }

    public string? Name { get; set; }

    public string[] AllowVariants { get; set; } = [];

    public string[] SkipVariants { get; set; } = [];

    public bool IsValidVariant(string variant)
    {
        if (AllowVariants.Length != 0)
        {
            return AllowVariants.Contains(variant);
        }

        if (SkipVariants.Length != 0)
        {
            return !SkipVariants.Contains(variant);
        }

        return true;
    }

    public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
    {
        Type = (EnumItemClass)reader.ReadInt32();
        Code = new(reader.ReadString());
        Name = reader.ReadNullableString();
        AllowVariants = reader.ReadStringArray();
        SkipVariants = reader.ReadStringArray();
    }

    public void ToBytes(BinaryWriter writer)
    {
        writer.Write((int)Type);
        writer.Write(Code.ToString());
        writer.WriteNullable(Name);
        writer.WriteArray(AllowVariants);
        writer.WriteArray(SkipVariants);
    }
}
