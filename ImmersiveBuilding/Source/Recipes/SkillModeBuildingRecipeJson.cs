using ImmersiveBuilding.Source.Common;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace ImmersiveBuilding.Source.Recipes;

public class SkillModeBuildingRecipeJson
{
    public string CodeSuffix { get; set; } = string.Empty;

    public required SkillModeRecipeToolJson Tool { get; set; }

    public bool ReplaceDrops { get; set; } = true;

    public required SkillModeRecipeIngredient[] Ingredients { get; set; }

    public required SkillModeRecipeOutput Output { get; set; }

    public SkillModeBuildingRecipe[] Unpack(IWorldAccessor resolver)
    {
        List<SkillModeBuildingRecipe> recipes = [];
        CollectibleObject[] tools = resolver.SearchItems(Tool.Code);

        foreach (string toolCode in tools.Select(tool => tool.Code))
        {
            string variant = WildcardUtil.GetWildcardValue(Tool.Code, toolCode);
            if (!Tool.IsValidVariant(variant))
            {
                continue;
            }

            string outputCode = ResolveSubstitute(Output.Code, variant);
            recipes.Add(
                new()
                {
                    Code = new AssetLocation($"{SharedConstants.ModName}:{outputCode}{CodeSuffix}"),
                    Tool = new SkillModeRecipeTool { Type = Tool.Type, Code = new AssetLocation(toolCode) },
                    ReplaceDrops = ReplaceDrops,
                    Ingredients =
                    [
                        .. Ingredients.Select(ingredient => new SkillModeRecipeIngredient()
                        {
                            Type = ingredient.Type,
                            Code = new AssetLocation(ResolveSubstitute(ingredient.Code, variant)),
                            Quantity = ingredient.Quantity,
                            TranslationCode = ingredient.TranslationCode,
                        }),
                    ],
                    Output = new SkillModeRecipeOutput
                    {
                        Type = Output.Type,
                        Code = new AssetLocation(outputCode),
                        Attributes = Output.Attributes,
                    },
                }
            );
        }

        return [.. recipes];
    }

    private AssetLocation ResolveSubstitute(AssetLocation code, string substitute)
    {
        return string.IsNullOrEmpty(Tool.Name) ? code : new AssetLocation(code.ToString().Replace($"{{{Tool.Name}}}", substitute));
    }
}
