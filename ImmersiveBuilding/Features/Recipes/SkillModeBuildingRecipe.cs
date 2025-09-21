using System.IO;
using Vintagestory.API.Common;

namespace ImmersiveBuilding.Features.Recipes;

public class SkillModeBuildingRecipe : IByteSerializable
{
    public CraftingRecipeIngredient Tool { get; set; } = new();

    public CraftingRecipeIngredient[] Ingredients { get; set; } = [];

    public CraftingRecipeIngredient Output { get; set; } = new();

    public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
    {
        Tool = new() { Code = new(reader.ReadString()) };
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
        writer.Write(Ingredients.Length);
        foreach (var ingredient in Ingredients)
        {
            writer.Write(ingredient.Code.ToString());
            writer.Write(ingredient.Quantity);
        }
        writer.Write(Output.Code.ToString());
    }
}
