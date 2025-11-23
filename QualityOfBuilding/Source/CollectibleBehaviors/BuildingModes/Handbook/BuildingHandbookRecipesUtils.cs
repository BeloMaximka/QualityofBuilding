using QualityOfBuilding.Source.Recipes;
using QualityOfBuilding.Source.Systems;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace QualityOfBuilding.Source.CollectibleBehaviors.BuildingModes.Handbook;

public static class BuildingHandbookRecipesUtils
{
    public static void InitBuildingRecipesForHandbook(this ICoreClientAPI api)
    {
        List<SkillModeBuildingRecipe> recipes = api.ModLoader.GetModSystem<MainSystem>().BuildingRecipes;
        InitBuildByRecipesInfo(recipes);
        InitIngredientForInfo(recipes);
    }

    private static void InitBuildByRecipesInfo(IEnumerable<SkillModeBuildingRecipe> recipes)
    {
        // I hope it is not very complicated
        // Group by output so we can add one behavior to them
        foreach (var recipesGroupedByOutput in recipes.GroupBy(recipe => recipe.Output.Code))
        {
            CollectibleObject? outputCollectible = recipesGroupedByOutput.First().Output.ResolvedItemStack?.Collectible;
            if (outputCollectible is null)
            {
                continue;
            }

            List<HandbookBuildingRecipe> recipesToBuild = [];
            // Group by groupId so we know how many recipes we have to build the block
            foreach (var anotherGroupByRecipeFile in recipesGroupedByOutput.GroupBy(recipyGroup => recipyGroup.GroupId))
            {
                // Group tools in case we have more than one tool to build the block
                ItemStack[] tools =
                [
                    .. anotherGroupByRecipeFile
                        .Where(recipe => recipe.Tool.ResolvedItemStack is not null)
                        .Select(recipe => recipe.Tool.ResolvedItemStack)!,
                ];

                // Merge ingredients withing the same recipe group (eg when we can use different soils to build a stone path block)
                ItemStack[][] ingredientsGroups = Enumerable
                    .Range(0, anotherGroupByRecipeFile.First().Ingredients.Length)
                    .Select(i => anotherGroupByRecipeFile.SelectMany(recipe => recipe.Ingredients[i].ResolvedItemStacks).ToArray())
                    .Where(ingredients => ingredients.Length > 0)
                    .ToArray()!;

                recipesToBuild.Add(new() { Tools = tools, IngredientGroups = [.. ingredientsGroups.Select(group => group.ToArray())] });
            }
            outputCollectible.CollectibleBehaviors = outputCollectible.CollectibleBehaviors.Append(
                new BuildingItemHandbookInfoBehavior(outputCollectible) { RecipesToBuild = recipesToBuild }
            );
        }
    }

    private static void InitIngredientForInfo(IEnumerable<SkillModeBuildingRecipe> recipes)
    {
        // We want to avoid duplicated items and group outputs from the same recipe by groupId
        Dictionary<CollectibleObject, HashSet<RecipeOutputHashValue>> collectibles = [];
        foreach (var recipe in recipes)
        {
            if (recipe.Output.ResolvedItemStack is null)
            {
                continue;
            }

            foreach (
                CollectibleObject ingredientCollectible in recipe
                    .Ingredients.Where(recipe => recipe.ResolvedItemStack is not null)
                    .Select(recipe => recipe.ResolvedItemStack!.Collectible)!
            )
            {
                collectibles.TryGetValue(ingredientCollectible, out HashSet<RecipeOutputHashValue>? outputs);
                if (outputs is null)
                {
                    outputs = [];
                    collectibles.Add(ingredientCollectible, outputs);
                }
                outputs.Add(new(recipe.GroupId, recipe.Output.ResolvedItemStack!));
            }
        }

        foreach (var collectible in collectibles)
        {
            BuildingItemHandbookInfoBehavior behavior = new(collectible.Key)
            {
                IngredientForBlockGroups = [.. collectible.Value.GroupBy(r => r.GroupId).Select(g => g.Select(r => r.ItemStack).ToArray())],
            };
            collectible.Key.CollectibleBehaviors = collectible.Key.CollectibleBehaviors.Append(behavior);
        }
    }
}

file sealed record RecipeOutputHashValue(int GroupId, ItemStack ItemStack)
{
    public bool Equals(RecipeOutputHashValue? other) => other is not null && ItemStack.Collectible.Code == other.ItemStack.Collectible.Code;

    public override int GetHashCode() => ItemStack.Collectible.Code.GetHashCode();
}
