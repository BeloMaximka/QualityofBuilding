using Vintagestory.API.Common;

namespace ImmersiveBuilding.Features.StoneItemModes;

internal class StoneItemCobbleBlockModeHandler(ICoreAPI api, string stoneName) : IModeHandler
{
    private readonly Block? cobbleBlock = api.World.GetBlock(new AssetLocation($"cobblestone-{stoneName}"));

    public void HandleStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
    {
        IPlayer? byPlayer = (byEntity as EntityPlayer)?.Player;
        if (byPlayer == null || cobbleBlock is null)
        {
            return;
        }

        BlockSelection newBlockSelection = blockSel;
        if (!api.World.BlockAccessor.GetBlock(blockSel.Position).IsReplacableBy(cobbleBlock))
        {
            newBlockSelection = blockSel.AddPosCopy(blockSel.Face.Normali);
        }

        string failureCode = "no-failure";
        cobbleBlock.TryPlaceBlock(api.World, byPlayer, slot.Itemstack, newBlockSelection, ref failureCode);

        if (failureCode == "no-failure")
        {
            api.World.PlaySoundAt(
                cobbleBlock.Sounds.Place,
                blockSel.Position.X,
                blockSel.Position.Y,
                blockSel.Position.Z,
                byPlayer
            );
        }
    }
}
