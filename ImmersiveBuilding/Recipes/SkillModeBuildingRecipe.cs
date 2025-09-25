using ImmersiveBuilding.Extensions;
using System.IO;
using Vintagestory.API.Common;

namespace ImmersiveBuilding.Recipes;

public class SkillModeBuildingRecipe : IByteSerializable
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.

    public SkillModeRecipeTool Tool { get; set; }

    public SkillModeRecipeIngredient[] Ingredients { get; set; }

    public SkillModeRecipeOutput Output { get; set; }

#pragma warning restore CS8618 // This way of (de)serializing sucks

    public AssetLocation ResolveSubstitute(AssetLocation code, string substitute)
    {
        return string.IsNullOrEmpty(Tool.Name) ? code : new AssetLocation(code.ToString().Replace($"{{{Tool.Name}}}", substitute));
    }

    public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
    {
        Tool = reader.Read<SkillModeRecipeTool>(resolver);
        Ingredients = reader.ReadArray<SkillModeRecipeIngredient>(resolver);
        Output = reader.Read<SkillModeRecipeOutput>(resolver);
    }

    public void ToBytes(BinaryWriter writer)
    {
        writer.Write(Tool);
        writer.WriteArray(Ingredients);
        writer.Write(Output);
    }
}
