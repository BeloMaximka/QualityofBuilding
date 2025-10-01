using ImmersiveBuilding.Source.Render;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace ImmersiveBuilding.Source.Systems;

public class ImmersiveBuildingRenderingSystem : ModSystem
{
    public SkillModeBuildingHud SkillModeHud { get; private set; } = null!;

    public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Client;

    public override void StartClientSide(ICoreClientAPI api)
    {
        SkillModeHud = new SkillModeBuildingHud(api);
        api.Event.BeforeActiveSlotChanged += (args) =>
        {
            SkillModeHud?.TryClose();
            return EnumHandling.PassThrough;
        };
    }
}
