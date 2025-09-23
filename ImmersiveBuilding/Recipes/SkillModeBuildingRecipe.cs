using System.IO;
using Vintagestory.API.Common;

namespace ImmersiveBuilding.Recipes;

public class SkillModeBuildingRecipe : IByteSerializable
{
    public CraftingRecipeIngredient Tool { get; set; } = new();

    public CraftingRecipeIngredient[] Ingredients { get; set; } = [];

    public CraftingRecipeIngredient Output { get; set; } = new();

    public AssetLocation ResolveSubstitute(AssetLocation code, string substitute)
    {
        return string.IsNullOrEmpty(Tool.Name) ? code : new AssetLocation(code.ToString().Replace($"{{{Tool.Name}}}", substitute));
    }

    public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
    {
        Tool = new() { Code = new(reader.ReadString()), Name = reader.ReadString() };
        int ingredientCount = reader.ReadInt32();
        Ingredients = new CraftingRecipeIngredient[ingredientCount];
        for (int i = 0; i < ingredientCount; i++)
        {
            Ingredients[i] = new CraftingRecipeIngredient() { Code = new(reader.ReadString()), Quantity = reader.ReadInt32() };
        }
        Output = new() { Code = new(reader.ReadString()) };
    }

    public void ToBytes(BinaryWriter writer)
    {
        writer.Write(Tool.Code.ToString());
        writer.Write(Tool.Name ?? string.Empty);
        writer.Write(Ingredients.Length);
        foreach (var ingredient in Ingredients)
        {
            writer.Write(ingredient.Code.ToString());
            writer.Write(ingredient.Quantity);
        }
        writer.Write(Output.Code.ToString());
    }
}
