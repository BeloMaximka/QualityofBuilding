using System.IO;
using Vintagestory.API.Common;

namespace ImmersiveBuilding.Recipes;

public class SkillModeRecipeOutput : IByteSerializable
{
    public EnumItemClass Type { get; set; }

    public AssetLocation Code { get; set; } = string.Empty;

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
