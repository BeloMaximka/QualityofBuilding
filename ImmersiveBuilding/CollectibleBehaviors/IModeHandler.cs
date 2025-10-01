using Vintagestory.API.Common;

namespace ImmersiveBuilding.CollectibleBehaviors;

public interface IModeHandler
{
    void HandleStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel);
}
