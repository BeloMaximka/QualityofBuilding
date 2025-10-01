using ImmersiveBuilding.Source.Extensions;
using System.IO;
using Vintagestory.API.Common;

namespace ImmersiveBuilding.Source.Recipes;

public class SkillModeBuildingRecipe : IByteSerializable
{
    public SkillModeRecipeTool Tool { get; set; } = null!;

    public SkillModeRecipeIngredient[] Ingredients { get; set; } = null!;

    public SkillModeRecipeOutput Output { get; set; } = null!;

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
