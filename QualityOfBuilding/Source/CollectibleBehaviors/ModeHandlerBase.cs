using Vintagestory.API.Common;

namespace QualityOfBuilding.Source.CollectibleBehaviors;

public class ModeHandlerBase
{
    public virtual void HandleStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel) { }

    public virtual bool HandleStep(
        float secondsUsed,
        ItemSlot slot,
        EntityAgent byEntity,
        BlockSelection blockSel,
        EntitySelection entitySel
    )
    {
        return false;
    }

    public virtual void HandleStop(
        float secondsUsed,
        ItemSlot slot,
        EntityAgent byEntity,
        BlockSelection blockSel,
        EntitySelection entitySel
    )
    { }
}
