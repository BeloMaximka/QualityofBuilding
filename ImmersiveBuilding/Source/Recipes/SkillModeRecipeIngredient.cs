using ImmersiveBuilding.Source.Utils;
using System.IO;
using Vintagestory.API.Common;

namespace ImmersiveBuilding.Source.Recipes;

public class SkillModeRecipeIngredient : IByteSerializable
{
    public EnumItemClass Type { get; set; }

    public required AssetLocation Code { get; set; }

    public int Quantity { get; set; } = 1;

    public string? TranslationCode { get; set; }

    public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
    {
        Type = (EnumItemClass)reader.ReadInt32();
        Code = new(reader.ReadString());
        Quantity = reader.ReadInt32();
        TranslationCode = reader.ReadNullableString();
    }

    public void ToBytes(BinaryWriter writer)
    {
        writer.Write((int)Type);
        writer.Write(Code.ToString());
        writer.Write(Quantity);
        writer.WriteNullable(TranslationCode);
    }
}
