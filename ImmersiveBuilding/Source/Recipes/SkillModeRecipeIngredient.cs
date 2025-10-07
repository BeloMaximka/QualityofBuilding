using System.IO;
using Vintagestory.API.Common;

namespace ImmersiveBuilding.Source.Recipes;

public class SkillModeRecipeIngredient : IByteSerializable
{
    public EnumItemClass Type { get; set; }

    public required AssetLocation Code { get; set; }

    public int Quantity { get; set; } = 1;

    public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
    {
        Type = (EnumItemClass)reader.ReadInt32();
        Code = new(reader.ReadString());
        Quantity = reader.ReadInt32();
    }

    public void ToBytes(BinaryWriter writer)
    {
        writer.Write((int)Type);
        writer.Write(Code.ToString());
        writer.Write(Quantity);
    }
}
