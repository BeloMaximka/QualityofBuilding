using Vintagestory.API.Common;

namespace QualityOfBuilding.Source.CollectibleBehaviors;

public interface IModeHandler
{
    void HandleStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel);
}
