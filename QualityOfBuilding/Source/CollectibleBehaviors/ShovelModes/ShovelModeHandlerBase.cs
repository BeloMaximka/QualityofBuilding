using Vintagestory.API.Common;

namespace QualityOfBuilding.Source.CollectibleBehaviors.ShovelModes;

internal class ShovelModeHandlerBase : ModeHandlerBase
{
    public override bool HandleStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
    {
        return base.HandleStep(secondsUsed, slot, byEntity, blockSel, entitySel);
    }
}
