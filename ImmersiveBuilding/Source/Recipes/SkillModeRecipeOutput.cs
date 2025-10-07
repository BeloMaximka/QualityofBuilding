using ImmersiveBuilding.Source.Extensions;
using Newtonsoft.Json.Linq;
using System.IO;
using Vintagestory.API.Common;

namespace ImmersiveBuilding.Source.Recipes;

public class SkillModeRecipeOutput : IByteSerializable
{
    public EnumItemClass Type { get; set; }

    public required AssetLocation Code { get; set; }

    public JToken? Attributes { get; set; }

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
