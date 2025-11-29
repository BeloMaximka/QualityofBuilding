using System;
using System.Linq;
using Vintagestory.API.Common;

namespace QualityOfBuilding.Source.CollectibleBehaviors.ShovelModes;

public class ReplaceModeHandler(int[] replacableBlockIds, Block output) : ShovelModeHandlerBase(output)
{
    private readonly Block output = output;

    public override void HandleStop(
        float secondsUsed,
        ItemSlot slot,
        EntityAgent byEntity,
        BlockSelection blockSel,
        EntitySelection entitySel
    )
    {
        if (
            byEntity.Api.Side == EnumAppSide.Client
            || byEntity is not EntityPlayer { Player: IPlayer byPlayer }
            || byPlayer.CurrentBlockSelection.Block is null
        )
        {
            return;
        }

        UpdateSelection(byPlayer, blockSel, tmpSel);

        if (!CanHandle(tmpSel.Block, byEntity) || HasNotMinedEnough(tmpSel.Block, secondsUsed, slot))
        {
            return;
        }

        ReplaceBlock(tmpSel.Position, byPlayer, output);
        DamageShovel(byPlayer, slot);
    }

    internal override bool CanHandle(Block block, EntityAgent entity)
    {
        return block.Id != output.Id && replacableBlockIds.Contains(block.Id);
    }
}
