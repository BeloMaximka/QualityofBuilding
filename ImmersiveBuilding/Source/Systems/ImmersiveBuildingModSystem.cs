using HarmonyLib;
using ImmersiveBuilding.Source.BlockBehaviors;
using ImmersiveBuilding.Source.CollectibleBehaviors.BuildingModes;
using ImmersiveBuilding.Source.CollectibleBehaviors.ShovelModes;
using ImmersiveBuilding.Source.Recipes;
using ImmersiveBuilding.Source.Utils;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace ImmersiveBuilding.Source.Systems;

public class ImmersiveBuildingModSystem : ModSystem
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
        api.RegisterCollectibleBehaviorClass(nameof(BuildingItemBehavior), typeof(BuildingItemBehavior));
        BuildingRecipes = api.RegisterRecipeRegistry<RecipeRegistryGeneric<SkillModeBuildingRecipe>>("skillmodebuildingrecipes").Recipes;
    }

    public override void AssetsFinalize(ICoreAPI api)
    {
        if (api.Side == EnumAppSide.Client)
        {
            foreach (Block stonePathBlock in api.World.SearchBlocks("stonepath-*"))
            {
                stonePathBlock.BlockBehaviors = stonePathBlock.BlockBehaviors.Append(new StonePathBehavior(stonePathBlock));
            }
            foreach (Block pathStairsStairsBlock in api.World.SearchBlocks("stonepathstairs-*"))
            {
                pathStairsStairsBlock.BlockBehaviors = pathStairsStairsBlock.BlockBehaviors.Append(
                    new StonePathBehavior(pathStairsStairsBlock)
                );
            }

            return;
        }

        // Add shovel behaviors
        foreach (Item item in api.World.SearchItems(AssetLocation.Create("shovel-*")))
        {
            Mod.Logger.VerboseDebug("Adding {0} to {1}", nameof(ShovelBehavior), item.Code.ToString());
            item.CollectibleBehaviors = [new ShovelBehavior(item), .. item.CollectibleBehaviors];
        }

        // Add other building behaviors
        BuildingRecipes.AddRange([.. api.Assets.GetMany<SkillModeBuildingRecipe>(api.Logger, "recipes/skillmodebuilding/").Values]);
        foreach (var recipesGroupedByTools in BuildingRecipes.GroupBy((recipe) => recipe.Tool.Code))
        {
            CollectibleObject[] collectibles = api.World.SearchItems(recipesGroupedByTools.Key);
            if (collectibles.Length == 0)
            {
                collectibles = api.World.SearchBlocks(recipesGroupedByTools.Key);
            }
            if (collectibles.Length == 0)
            {
                api.Logger.Warning("No items or blocks found by code {0}", recipesGroupedByTools.Key);
                continue;
            }

            foreach (CollectibleObject collectible in collectibles)
            {
                string variant = WildcardUtil.GetWildcardValue(recipesGroupedByTools.Key, collectible.Code);
                if (!recipesGroupedByTools.Any(recipe => recipe.IsValidVariant(variant)))
                {
                    continue; // No recipes for this variant
                }

                if (collectible.GetBehavior<BuildingItemBehavior>() is null) // Prevent duplicate behavior from recipes like firebrick
                {
                    Mod.Logger.VerboseDebug("Adding {0} to {1}", nameof(BuildingItemBehavior), collectible.Code.ToString());
                    collectible.CollectibleBehaviors = [new BuildingItemBehavior(collectible), .. collectible.CollectibleBehaviors];
                }

                // Adjust drops for blocks
                foreach (
                    SkillModeBuildingRecipe recipe in recipesGroupedByTools.Where(recipe =>
                        recipe.ReplaceDrops && recipe.IsValidVariant(variant)
                    )
                )
                {
                    ChangeOutputBlockDropsToRawMaterials(api, recipe, variant);
                }
            }
        }
    }

    private void ChangeOutputBlockDropsToRawMaterials(ICoreAPI api, SkillModeBuildingRecipe recipe, string variant)
    {
        AssetLocation blockCode = recipe.ResolveSubstitute(recipe.Output.Code, variant).WithStatePartsAsWildcards();
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
                    {
                        string itemCode = recipe.ResolveSubstitute(ingredient.Code, variant);
                        CollectibleObject? collectible = api.World.GetItem(itemCode);
                        collectible ??= api.World.GetBlock(itemCode);

                        if (collectible is null)
                        {
                            Mod.Logger.Warning(
                                "Unable to add recipe ingredient {0} for {1}, no blocks or items found!",
                                itemCode,
                                recipe.ResolveSubstitute(recipe.Output.Code, variant)
                            );
                            return null;
                        }
                        return new BlockDropItemStack(new ItemStack(collectible, ingredient.Quantity))
                        {
                            Quantity = new NatFloat(ingredient.Quantity, 0f, EnumDistribution.UNIFORM), // BlockDropItemStack hardcodes NatFloat.One
                        };
                    })
                    .Where(ingredient => ingredient is not null),
            ];
            Mod.Logger.VerboseDebug("Changed recipe for block {0}", block.Code);
        }
    }

    public override void Dispose()
    {
        HarmonyInstance.UnpatchAll(HarmonyInstance.Id);
    }
}
