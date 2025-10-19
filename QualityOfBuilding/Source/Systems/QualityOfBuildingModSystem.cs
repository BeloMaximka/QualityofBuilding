using HarmonyLib;
using QualityOfBuilding.Source.CollectibleBehaviors.BuildingModes;
using QualityOfBuilding.Source.CollectibleBehaviors.ShovelModes;
using QualityOfBuilding.Source.Commands;
using QualityOfBuilding.Source.Recipes;
using QualityOfBuilding.Source.Utils;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace QualityOfBuilding.Source.Systems;

public class QualityOfBuildingModSystem : ModSystem
{
    private Harmony HarmonyInstance => new(Mod.Info.ModID);

    public List<SkillModeBuildingRecipe> BuildingRecipes { get; private set; } = [];

    // Set >1 so we can load recipes and patch shovel behaviors
    public override double ExecuteOrder() => 1.1;

    public override void StartPre(ICoreAPI api)
    {
        if (!HarmonyInstance.GetPatchedMethods().Any())
        {
            HarmonyInstance.PatchAll();
        }
    }

    public override void Start(ICoreAPI api)
    {
        api.RegisterCollectibleBehaviorClass(nameof(ShovelBehavior), typeof(ShovelBehavior));
        BuildingRecipes = api.RegisterRecipeRegistry<RecipeRegistryGeneric<SkillModeBuildingRecipe>>("skillmodebuildingrecipes").Recipes;

        if (api is ICoreServerAPI sapi)
        {
            GenerateBuildingRecipeJsonFromGridRecipeCommand.Register(sapi);
            sapi.RegisterCollectibleBehaviorClass(nameof(BuildingItemBehavior), typeof(BuildingItemBehavior));
        }
    }

    public override void AssetsFinalize(ICoreAPI api)
    {
        if (api.Side == EnumAppSide.Server)
        {
            // Add shovel behaviors
            foreach (Item item in api.World.SearchItems(AssetLocation.Create("shovel-*")))
            {
                Mod.Logger.VerboseDebug("Adding {0} to {1}", nameof(ShovelBehavior), item.Code.ToString());
                item.CollectibleBehaviors = [new ShovelBehavior(item), .. item.CollectibleBehaviors];
            }
        }

        // Add other building behaviors manually
        InitBuildingRecipes(api);
        foreach (var recipesGroupedByTools in BuildingRecipes.GroupBy((recipe) => recipe.Tool.Code))
        {
            CollectibleObject? tool = recipesGroupedByTools.First().Tool.ResolvedItemStack?.Collectible;
            if (tool is null)
            {
                continue;
            }

            if (tool.GetBehavior<BuildingItemBehavior>() is null) // Prevent duplicate behavior from recipes like firebrick
            {
                Mod.Logger.VerboseDebug("Adding {0} to {1}", nameof(BuildingItemBehavior), tool.Code.ToString());
                tool.CollectibleBehaviors = [new BuildingItemBehavior(tool, api, recipesGroupedByTools), .. tool.CollectibleBehaviors];
            }

            if (api is not ICoreServerAPI sapi)
            {
                continue;
            }
            // Adjust drops for blocks
            foreach (SkillModeBuildingRecipe recipe in recipesGroupedByTools.Where(recipe => recipe.ReplaceDrops))
            {
                ChangeOutputBlockDropsToRawMaterials(sapi, recipe);
            }
        }
    }

    public override void Dispose()
    {
        HarmonyInstance.UnpatchAll(HarmonyInstance.Id);
    }

    private void InitBuildingRecipes(ICoreAPI api)
    {
        if (api.Side == EnumAppSide.Server)
        {
            int groupId = 0;
            foreach (var recipeJson in api.Assets.GetMany<SkillModeBuildingRecipeJson>(api.Logger, "recipes/buildingmenu/").Values)
            {
                BuildingRecipes.AddRange(recipeJson.Unpack(api.World, groupId++));
            }
        }

        foreach (SkillModeBuildingRecipe recipe in BuildingRecipes)
        {
            recipe.ResolveItemStacks(api.World);
        }

        Mod.Logger.Notification("Initialized {0} building recipes", BuildingRecipes.Count);
    }

    private void ChangeOutputBlockDropsToRawMaterials(ICoreServerAPI api, SkillModeBuildingRecipe recipe)
    {
        AssetLocation blockCode = recipe.Output.Code.WithStatePartsAsWildcards();
        Block[] blocksWithVariants = api.World.SearchBlocks(blockCode);

        if (blocksWithVariants.Length == 0)
        {
            Mod.Logger.Warning("Unable to change recipe for {0}, no blocks found!", blockCode);
            return;
        }

        foreach (Block block in blocksWithVariants)
        {
            block.Drops =
            [
                .. recipe
                    .Ingredients.Select(ingredient =>
                        ingredient.ResolvedItemStack is not null
                            ? new BlockDropItemStack(ingredient.ResolvedItemStack)
                            {
                                Quantity = new NatFloat(ingredient.Quantity, 0f, EnumDistribution.UNIFORM), // BlockDropItemStack hardcodes NatFloat.One
                            }
                            : null
                    )
                    .Where(ingredient => ingredient is not null),
            ];
            Mod.Logger.VerboseDebug("Changed recipe for block {0}", block.Code);
        }
    }
}
