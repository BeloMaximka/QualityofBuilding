using ImmersiveBuilding.Source.Utils;
using System.IO;
using Vintagestory.API.Common;

namespace ImmersiveBuilding.Source.Recipes;

public class SkillModeBuildingRecipe : IByteSerializable
{
    public string CodeSuffix { get; set; } = string.Empty;

    public SkillModeRecipeTool Tool { get; set; } = null!;

    public bool ReplaceDrops { get; set; } = true;

    public SkillModeRecipeIngredient[] Ingredients { get; set; } = null!;

    public SkillModeRecipeOutput Output { get; set; } = null!;

    public AssetLocation ResolveSubstitute(AssetLocation code, string substitute)
    {
        return string.IsNullOrEmpty(Tool.Name) ? code : new AssetLocation(code.ToString().Replace($"{{{Tool.Name}}}", substitute));
    }

    public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
    {
        CodeSuffix = reader.ReadString();
        Tool = reader.Read<SkillModeRecipeTool>(resolver);
        ReplaceDrops = reader.ReadBoolean();
        Ingredients = reader.ReadArray<SkillModeRecipeIngredient>(resolver);
        Output = reader.Read<SkillModeRecipeOutput>(resolver);
    }

    public void ToBytes(BinaryWriter writer)
    {
        writer.Write(CodeSuffix);
        writer.Write(Tool);
        writer.Write(ReplaceDrops);
        writer.WriteArray(Ingredients);
        writer.Write(Output);
    }
}
