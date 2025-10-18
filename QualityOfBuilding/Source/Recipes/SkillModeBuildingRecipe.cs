using QualityOfBuilding.Source.Utils;
using System.IO;
using Vintagestory.API.Common;

namespace QualityOfBuilding.Source.Recipes;

public class SkillModeBuildingRecipe : IByteSerializable
{
    /// <summary>
    /// Used to group recipes created by the same file
    /// </summary>
    public int GroupId { get; set; } = -1;

    public AssetLocation Code { get; set; } = string.Empty;

    public SkillModeRecipeTool Tool { get; set; } = null!;

    public bool ReplaceDrops { get; set; } = true;

    public SkillModeRecipeIngredient[] Ingredients { get; set; } = null!;

    public SkillModeRecipeOutput Output { get; set; } = null!;

    public void ResolveItemStacks(IWorldAccessor resolver)
    {
        Tool.ResolveItemStack(resolver);
        Output.ResolveItemStack(resolver);
        foreach (var ingredient in Ingredients)
        {
            ingredient.ResolveItemStack(resolver);
        }

    }
    public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
    {
        GroupId = reader.ReadInt32();
        Code = reader.ReadString();
        Tool = reader.Read<SkillModeRecipeTool>(resolver);
        ReplaceDrops = reader.ReadBoolean();
        Ingredients = reader.ReadArray<SkillModeRecipeIngredient>(resolver);
        Output = reader.Read<SkillModeRecipeOutput>(resolver);
    }

    public void ToBytes(BinaryWriter writer)
    {
        writer.Write(GroupId);
        writer.Write(Code);
        writer.Write(Tool);
        writer.Write(ReplaceDrops);
        writer.WriteArray(Ingredients);
        writer.Write(Output);
    }
}
