using Vintagestory.API.Common;

namespace ImmersiveBuilding.Features.BuildingModes;

internal class BuildingModeHandler(ICoreAPI api, string blockCode) : IModeHandler
{
    private readonly Block? block = api.World.GetBlock(new AssetLocation(blockCode));

    public void HandleStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
    {
        IPlayer? byPlayer = (byEntity as EntityPlayer)?.Player;
        if (byPlayer == null || block is null)
        {
            return;
        }

        BlockSelection newBlockSelection = blockSel;
        if (!api.World.BlockAccessor.GetBlock(blockSel.Position).IsReplacableBy(block))
        {
            newBlockSelection = blockSel.AddPosCopy(blockSel.Face.Normali);
        }

        string failureCode = "no-failure";
        block.TryPlaceBlock(api.World, byPlayer, slot.Itemstack, newBlockSelection, ref failureCode);

        if (failureCode == "no-failure")
        {
            api.World.PlaySoundAt(block.Sounds.Place, blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, byPlayer);
        }
    }
}
