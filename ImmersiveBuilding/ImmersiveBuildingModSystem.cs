using ImmersiveBuilding.Features.ShovelModes;
using ImmersiveBuilding.Features.StoneItemModes;
using Vintagestory.API.Common;

namespace ImmersiveBuilding;

public class ImmersiveBuildingModSystem : ModSystem
{
    // Set >0.2 so we can patch shovel behaviours
    public override double ExecuteOrder() => 0.3;

    public override void Start(ICoreAPI api)
    {
        api.RegisterCollectibleBehaviorClass(nameof(ShovelBehavior), typeof(ShovelBehavior));
        api.RegisterCollectibleBehaviorClass(nameof(StoneItemBehavior), typeof(StoneItemBehavior));
    }

    public override void AssetsLoaded(ICoreAPI api)
    {
        if (api.Side == EnumAppSide.Client)
        {
            return; // It seems server syncs behaviors with the client
        }

        foreach (Item item in api.World.SearchItems(AssetLocation.Create("shovel-*")))
        {
            Mod.Logger.Notification("Adding {0} to {1}", nameof(ShovelBehavior), item.Code.Path);
            CollectibleBehavior[] collectibleBehaviorList = new CollectibleBehavior[item.CollectibleBehaviors.Length + 1];
            item.CollectibleBehaviors.CopyTo(collectibleBehaviorList, 0);
            collectibleBehaviorList[^1] = new ShovelBehavior(item);
            item.CollectibleBehaviors = collectibleBehaviorList;
        }

        foreach (Item item in api.World.SearchItems(AssetLocation.Create("stone-*")))
        {
            Mod.Logger.Notification("Adding {0} to {1}", nameof(StoneItemBehavior), item.Code.Path);
            CollectibleBehavior[] collectibleBehaviorList = new CollectibleBehavior[item.CollectibleBehaviors.Length + 1];
            item.CollectibleBehaviors.CopyTo(collectibleBehaviorList, 0);
            collectibleBehaviorList[^1] = new StoneItemBehavior(item);
            item.CollectibleBehaviors = collectibleBehaviorList;
        }
    }
}
