using ImmersiveBuilding.Features.BuildingModes;
using ImmersiveBuilding.Features.Recipes;
using ImmersiveBuilding.Features.ShovelModes;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;

namespace ImmersiveBuilding;

public class ImmersiveBuildingModSystem : ModSystem
{
    public List<SkillModeBuildingRecipe> BuildingRecipes { get; private set; } = [];

    // Set >1 so we can load recipes and patch shovel behaviors
    public override double ExecuteOrder() => 1.1;

    public override void Start(ICoreAPI api)
    {
        api.RegisterCollectibleBehaviorClass(nameof(ShovelBehavior), typeof(ShovelBehavior));
        api.RegisterCollectibleBehaviorClass(nameof(BuildingItemBehavior), typeof(BuildingItemBehavior));
        BuildingRecipes = api.RegisterRecipeRegistry<RecipeRegistryGeneric<SkillModeBuildingRecipe>>("skillmodebuildingrecipes").Recipes;
    }

    public override void AssetsLoaded(ICoreAPI api)
    {
        if (api.Side == EnumAppSide.Client)
        {
            return; // It seems server syncs behaviors and recipes with the client
        }

        // Add shovel behaviors
        foreach (Item item in api.World.SearchItems(AssetLocation.Create("shovel-*")))
        {
            Mod.Logger.Notification("Adding {0} to {1}", nameof(ShovelBehavior), item.Code.ToString());
            CollectibleBehavior[] collectibleBehaviorList = new CollectibleBehavior[item.CollectibleBehaviors.Length + 1];
            item.CollectibleBehaviors.CopyTo(collectibleBehaviorList, 0);
            collectibleBehaviorList[^1] = new ShovelBehavior(item);
            item.CollectibleBehaviors = collectibleBehaviorList;
        }

        // Add other building behaviors
        BuildingRecipes.AddRange([.. api.Assets.GetMany<SkillModeBuildingRecipe>(api.Logger, "recipes/skillmodebuilding/").Values]);
        foreach (var recipesGroupedByTools in BuildingRecipes.GroupBy((recipe) => recipe.Tool.Code))
        {
            foreach (Item item in api.World.SearchItems(recipesGroupedByTools.Key))
            {
                Mod.Logger.Notification("Adding {0} to {1}", nameof(BuildingItemBehavior), item.Code.ToString());
                CollectibleBehavior[] collectibleBehaviorList = new CollectibleBehavior[item.CollectibleBehaviors.Length + 1];
                item.CollectibleBehaviors.CopyTo(collectibleBehaviorList, 0);
                collectibleBehaviorList[^1] = new BuildingItemBehavior(item);
                item.CollectibleBehaviors = collectibleBehaviorList;
            }
        }
    }
}
